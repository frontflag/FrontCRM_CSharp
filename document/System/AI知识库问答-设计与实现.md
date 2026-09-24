# AI 知识库问答（knowledge.handbook.qa）设计与实现

**文档版本：** v0.1  
**更新日期：** 2026-09-23  
**状态：** 草案（未编码）  
**项目名称：** FrontCRM_CSharp  
**适用对象：** 后端 / 前端 / 运维  

**关联文档：**

- [AI模块架构与实现](./AI模块架构与实现.md)
- [PostgreSQL 增量脚本编写规范](../PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md)

**权限与脚本（落地时新增）：** `scripts/kb_handbook_postgresql.sql`  
**EBS 对照：** 无  
**QA 对照：** 功能落地后另写 `document/QA/`，编写前须先读 [测试规范总纲](../QA/测试规范总纲.md)。本文不替代测试对照说明。

---

## 1. 目的与范围

用户在系统里用自然语言提问。系统把知识库文档切成块，写入 PostgreSQL 向量列，检索最相近的若干块，再调用现有 LLM 场景，只根据这些块生成答复，并给出章节出处。

**首份知识库：** `电子元器件分销行业新人培养教材（行业通用版）.docx`（约 4.8 万字，21 章 + 附录，正文含【标准】【话术】【案例】标记）。文件现存于仓库外，由管理端导入，不提交进 Git。

**本期做：**

- docx 按章、节切块，长节再切，块间重叠
- 同一 FrontCRM 库启用 `pgvector`，块向量与正文同库
- 提问：问题向量 → 余弦距离取 Top-K → 距离合格才调 LLM
- 单轮问答页、文档导入与版本管理页
- 答案标明来自培训教材，并附检索到的章节摘录

**本期不做：**

- 多轮追问、流式输出
- 把教材回答当成公司制度或 FrontCRM 操作说明
- 用对话模型生成向量（现有 Provider 只有 `/chat/completions`）
- 在通用 `POST /api/v1/ai/invoke` 里由前端上传检索片段

通用厂商、限流框架、调用日志仍走 [AI模块架构与实现](./AI模块架构与实现.md)。本文只定义知识库检索和教材问答场景。

---

## 2. 产品行为

### 2.1 问答

| 项 | 约定 |
|----|------|
| 页面 | 培训问答，路由 `/knowledge/handbook` |
| 权限 | `biz.ai.kb.qa` |
| 输入 | 一个问题，1～500 字 |
| 输出 | 答案、是否覆盖、教材名与版本号、检索到的章节摘录 |
| 无权限 | 菜单不显示 |
| 超时 | 前端 180 秒，与现有 `aiApi.invoke` 一致 |

答案区固定一句：**以下内容来自新人培训教材，不是公司制度。**

检索块落在第 15 章（风控红线）或第 16 章（出口管制与贸易合规）时，答案再固定加一句：**涉及合规、假货或诈骗的筛查，以公司合规要求为准。**

教材片段不足以回答时，不调用对话模型，直接回复：**教材未覆盖该问题。** 页面仍可展示距离最近的一条标题，便于管理员判断是切块问题还是确实没有。

### 2.2 导入

| 项 | 约定 |
|----|------|
| 页面 | 知识库文档，路由 `/system/kb-documents`，挂在参数管理中「AI 配置」附近 |
| 权限 | `biz.ai.kb.admin` |
| 动作 | 上传 docx、查看切块、看嵌入进度、启用某个已就绪版本 |
| 版本 | 每次上传生成新版本。仅 `ready` 版本可启用。同一文档同时只有一个启用版本 |

启用新版本后，提问只检索该版本。旧版本的问答缓存不再命中。

---

## 3. 调用链

```
管理端上传 docx
  → 保存文件，写入 kb_document_version（status = pending）
  → KbIngestHostedService 取到任务
       解析 docx → 切块 → 批量 /embeddings → 写入 kb_chunk.embedding
       → status = ready（首个成功版本自动启用）

用户提问
  → POST /api/v1/kb/ask
  → 权限 biz.ai.kb.qa，用户每分钟提问次数上限 10（含未覆盖）
  → 查 kb_ask_cache（同一启用版本 + 规范化问题）
  → 未命中：同一 embedding 模型把问题变成向量
  → SQL：当前启用版本上按余弦距离取候选
  → Top1 距离 > 0.45：写未覆盖缓存，返回「教材未覆盖」
  → 否则取距离 ≤ 0.55 的最多 5 块
  → AiOrchestrator 场景 knowledge.handbook.qa
       只把这几块填进提示词
       权限、LLM 限流、LLM 结果缓存、调用日志沿用现有编排
  → 返回答案 + 服务端检索到的摘录（不单信模型自称的引用）
```

对话模型继续用现有 Moonshot 场景配置。向量模型单独配置，见第 6 节。

---

## 4. 切块

切块是纯函数，不访问数据库，单独单测。

### 4.1 从 docx 取段落

使用 `DocumentFormat.OpenXml` 读取正文段落与表格。

- 段落：保留样式为标题的文本，以及普通段落
- 表格：按行拼成文本行，单元格之间用 ` | `，避免表格被丢掉
- 图片忽略
- 空白段落丢弃

### 4.2 去掉目录

本教材前部有目录，目录条目与正文标题重复。规则：

1. 找到文本规范化后等于「目录」或「目 录」的段落。
2. 其后若再次出现已经在目录区见过的同一标题（本教材是第二次「前言：使用说明」），正文从该标题开始。
3. 没有目录段时，正文从第一个章节标题开始。

规范化：去掉首尾空白，连续空白压成一个空格。

### 4.3 标题

| 匹配 | 级别 |
|------|------|
| `第{数字}章` 开头 | 章 |
| `{数字}.{数字}` 开头，且后面不是第三个点号段 | 节 |
| `附录` 开头 | 附录，视同章 |
| `【案例` 开头 | 案例标题，视同节 |

节继承当前章的章号与章标题。

### 4.4 块大小

| 参数 | 初值 | 说明 |
|------|------|------|
| 目标长度 | 900 字 | 按 C# 字符串长度，汉字计 1 |
| 硬上限 | 1400 字 | 超过则在句号、分号、换行处切开 |
| 重叠 | 150 字 | 只加在同一节被切开的后续块开头 |
| 过短合并 | 少于 80 字 | 并入下一块；文档末尾的短块保留 |

一块记录：

- `chapter_no`、`chapter_title`（如 `10`、`报价策略与 GP 管理`）
- `section_no`、`section_title`（如 `10.1`、`GP 公式与底线`）
- `heading`：展示用，`第10章 10.1 GP 公式与底线`
- `content`：标题行 + 正文。正文里的【标准】【话术】【案例】原样保留
- `chunk_index`：该版本内从 0 递增
- `content_sha256`：正文哈希，便于重跑时跳过未变化的块

同一版本的块文本不可原地修改。改教材必须上传新版本并重新嵌入。

---

## 5. 数据模型

库：现有 FrontCRM PostgreSQL。部署脚本先执行 `CREATE EXTENSION IF NOT EXISTS vector`。本机与生产的 PostgreSQL 都必须已安装 pgvector，否则扩展语句失败。

向量列固定 **`vector(1024)`**。HNSW 索引要求建索引时维度确定。换维度必须新建列或新表，并整库重嵌。

### 5.1 `kb_document`

| 列 | 说明 |
|----|------|
| `id` | UUID 主键 |
| `code` | 稳定编码。首份为 `handbook.distributor.newcomer` |
| `title` | 展示名 |
| `active_version_id` | 当前启用版本，可空 |
| `is_deleted` / `create_time` / `modify_time` | 与现有软删字段一致 |

### 5.2 `kb_document_version`

| 列 | 说明 |
|----|------|
| `id` | UUID |
| `document_id` | 所属文档 |
| `version_no` | 从 1 递增 |
| `source_file_name` | 原始文件名 |
| `source_sha256` | 文件哈希。同一文档、同一哈希、已有 `ready` 版本时拒绝重复导入 |
| `storage_path` | 上传目录中的 docx 相对路径 |
| `embedding_provider_code` | 写入时的向量厂商 |
| `embedding_model` | 写入时的向量模型 |
| `embedding_dimension` | 必须为 1024 |
| `chunk_count` | 块数 |
| `status` | `0 pending` / `1 embedding` / `2 ready` / `3 failed` / `4 retired` |
| `is_active` | 仅 `ready` 可为 true |
| `error_message` | 失败原因 |
| `create_time` / `modify_time` / `is_deleted` | 审计与软删 |

部分唯一索引：同一 `document_id` 在 `is_active = true AND is_deleted = false` 时只能有一行。

### 5.3 `kb_chunk`

| 列 | 说明 |
|----|------|
| `id` | UUID |
| `document_version_id` | 版本 |
| `chunk_index` | 版本内序号 |
| `chapter_no` / `chapter_title` | 章 |
| `section_no` / `section_title` | 节 |
| `heading` | 展示标题 |
| `content` | 送入模型的文本 |
| `content_chars` | 长度 |
| `content_sha256` | 内容哈希 |
| `embedding` | `vector(1024)`，可空直到嵌入完成 |

唯一：`(document_version_id, chunk_index)`。  
索引：`USING hnsw (embedding vector_cosine_ops)`，仅在列已是 1024 维且扩展可用时创建。

EF 映射文档与版本。`embedding` 不进 LINQ，插入与检索用参数化 SQL：把浮点数组格式化成 `[...]` 再 `::vector`。应用代码里的 SQL 可以使用数据库参数；**仓库 `scripts/*.sql` 仍须遵守增量脚本规范，文件中不得出现双花括号占位符。**

### 5.4 `kb_ask_cache`

| 列 | 说明 |
|----|------|
| `id` | UUID |
| `document_version_id` | 启用版本 |
| `question_sha256` | 规范化问题的 SHA256 |
| `question_norm` | trim、连续空白压缩后的问题 |
| `covered` | 是否调用了模型并判定覆盖 |
| `answer` | 展示文案 |
| `chunk_ids` | jsonb，检索到的块 id，顺序即距离序 |
| `top_distance` | Top1 余弦距离 |
| `expire_time` | 默认 7 天 |

唯一：`(document_version_id, question_sha256)`。  
命中缓存时不再请求 embedding，也不再请求对话模型。

LLM 成功结果仍由 `AiOrchestrator` 写入 `ai_invocation_cache`。两层缓存都带版本：`kb_ask_cache` 按版本外键；场景缓存的 key 字段包含 `corpus_version_id` 与 `chunk_ids`。

### 5.5 `kb_embedding_profile`

一行当前配置（种子可先插入禁用行，由运维填实）：

| 列 | 说明 |
|----|------|
| `provider_code` | 与 `ai_provider.code` 对齐，便于复用 base_url 与 `api_key_env` |
| `model` | embedding 模型名 |
| `dimension` | 固定 1024 |
| `is_enabled` | 未启用时导入与提问都失败并提示未配置向量模型 |

密钥仍只放环境变量，不入库。这与现有 AI 厂商约定相同。

---

## 6. 向量

### 6.1 接口

新增 `OpenAiCompatibleEmbeddingClient`，请求：

`POST {base_url}/embeddings`

```json
{ "model": "<profile.model>", "input": ["文本1", "文本2"] }
```

按返回的 `index` 写回对应块。每批 16 条。单批失败时该版本 `status = failed`，已写入的块保留，管理端可对该版本执行「继续嵌入」（只补 `embedding IS NULL` 的块）。

### 6.2 与对话模型分开

`OpenAiCompatibleAiLlmProvider` 只调用 `/chat/completions`，不能用来生成向量。  
对话场景 `knowledge.handbook.qa` 的厂商仍在 AI 配置里选择（开发默认 `mock`，联调改为 `moonshot`）。  
向量厂商走 `kb_embedding_profile`。对话继续用 Moonshot。维度必须是 1024。

已选定的向量配置（密钥只放环境变量，不写入文档和库）：

| 项 | 值 |
|----|----|
| base_url | `https://api.siliconflow.cn/v1` |
| 请求路径 | `POST /embeddings` |
| model | `BAAI/bge-m3`（原生 1024 维） |
| 环境变量名 | `AI_EMBEDDING_API_KEY` |
| 维度 | 1024 |

### 6.3 Mock

`provider_code = mock` 时不访问网络。用文本 SHA256 生成确定性的 1024 维单位向量，供切块之后的检索单测使用。Mock 向量没有语义，不能用来验收教材问答质量。

### 6.4 距离

使用 pgvector 余弦距离运算符 `<=>`（越近越小，相同为 0）。

| 参数 | 初值 | 行为 |
|------|------|------|
| Top-K | 5 | 最多送入模型的块数 |
| 覆盖阈值 | Top1 距离 ≤ 0.45 | 超过则不调对话模型 |
| 块入选阈值 | 距离 ≤ 0.55 | 丢掉过远的块，避免无关章节进提示词 |

两个阈值放 `ai_global_config`：`kb_handbook_max_top_distance`、`kb_handbook_max_chunk_distance`，缺省用上表。改阈值后删除该版本的 `kb_ask_cache`，否则旧的「未覆盖」会继续命中。

首份教材导入后，用第 8 节的样例问题校准阈值，再改配置。不要在未看距离分布前把阈值写死进业务判断以外的地方。

---

## 7. 问答场景

| 项 | 值 |
|----|------|
| 场景码 | `knowledge.handbook.qa` |
| 权限 | `biz.ai.kb.qa` |
| 输出格式 | `json` |
| 温度 | 0.2（Moonshot kimi-k2 系由现有 Provider 强制为 1.0 的逻辑保持不变） |
| 缓存 TTL | 604800 秒 |
| 缓存字段 | `question`、`corpus_version_id`、`chunk_ids` |
| 允许输入 | `question`、`corpus_version_id`、`chunk_ids`、`context`、`compliance_hint` |
| 用户每分钟 | 场景限流 10。提问入口另有每分钟 10 次，限制含未覆盖，避免只打 embedding |

模型只看到 `context`（若干块，每块前有 `heading`）。服务端返回给前端的摘录来自检索结果，模型多写或少写的引用不作为页面出处。

期望 JSON：

```json
{
  "covered": true,
  "answer": "……"
}
```

`covered = false` 或 JSON 无法解析时，对用户显示「教材未覆盖该问题。」，并把本次调用记为失败以外的业务未覆盖（编排日志仍记 success，响应体保留模型原文供管理员查看）。不要把模型编造的数字展示为【标准】。

提示词约束（运行时模板，入库脚本用 CHR 拼接，见增量脚本规范）：

- 只根据给定片段回答
- 数字、公式、阈值、话术与片段一致，不补充教材外的公司流程
- 片段不够则 `covered` 为 false，`answer` 为空
- 用户问题、片段、合规附加句由服务端填入

`compliance_hint` 在任一入选块的 `chapter_no` 为 `15` 或 `16` 时为合规固定句，否则为空字符串。

---

## 8. API

控制器 `KnowledgeBaseController`，路由前缀 `/api/v1/kb`。权限在服务内校验，与 `AiOrchestrator` 的场景权限方式一致。

| 方法 | 路径 | 权限 | 作用 |
|------|------|------|------|
| GET | `/documents` | `biz.ai.kb.admin` | 文档与版本列表 |
| POST | `/documents/{code}/versions` | `biz.ai.kb.admin` | `multipart` 上传 docx，返回版本 id 与 `pending` |
| POST | `/documents/{code}/versions/{versionId}/resume` | `biz.ai.kb.admin` | 失败或中断后继续嵌入 |
| POST | `/documents/{code}/versions/{versionId}/activate` | `biz.ai.kb.admin` | 仅 `ready` 可启用 |
| GET | `/documents/{code}/versions/{versionId}/chunks` | `biz.ai.kb.admin` | 分页查看切块，不含向量 |
| POST | `/ask` | `biz.ai.kb.qa` |  body：`{ "question": "..." }` |

`/ask` 响应：

| 字段 | 说明 |
|------|------|
| `covered` | 是否依据教材作答 |
| `answer` | 已含教材声明；合规章另含合规句 |
| `documentTitle` / `versionNo` | 当前启用版本 |
| `fromCache` | 命中 `kb_ask_cache` |
| `citations` | `{ heading, excerpt, distance }[]`，excerpt 最多 240 字 |

没有启用版本、向量配置未启用、问题为空：返回 400 与明确文案，不调用模型。

---

## 9. 前端

| 文件 | 职责 |
|------|------|
| `CRM.Web/src/api/knowledgeBase.ts` | 上述 API |
| `CRM.Web/src/views/Knowledge/HandbookQaPage.vue` | 提问、答案、摘录 |
| `CRM.Web/src/views/System/KbDocumentPage.vue` | 上传、进度、切块预览、启用版本 |
| `routes.ts` | `/knowledge/handbook`、`/system/kb-documents` |
| `AppLayout.vue` | 问答菜单对 `biz.ai.kb.qa` 可见；管理菜单对 `biz.ai.kb.admin` 可见 |
| `zh-CN.ts` / `en-US.ts` | 文案 |

问答页在请求期间显示等待秒数。摘录默认折叠，展开后能看到章节标题和片段，便于核对 GP、货品状态、合规红线等【标准】。

---

## 10. 落地步骤

每步完成后再做下一步。向量扩展和 embedding 地址未就绪时，步骤 3 以后只能跑 Mock，不能把 Mock 结果当成教材验收。

### 步骤 0 — 环境

1. 在本地与目标 PostgreSQL 安装 pgvector，确认 `CREATE EXTENSION vector` 成功。
2. 确定 embeddings 的 base_url、模型名、环境变量名，并确认返回维度可以是 1024。
3. 对话模型沿用现有 Moonshot 配置，不把 API Key 写入库或文档。

**完成标准：** 数据库能建 `vector(1024)` 列；用一条样例文本能取回长度为 1024 的向量。

### 步骤 1 — 表、配置与权限

新增 `scripts/kb_handbook_postgresql.sql`（幂等，遵守增量脚本规范）：

- 扩展、四张表、HNSW 索引、启用版本的部分唯一索引
- `kb_embedding_profile` 种子（默认禁用，维度 1024）
- `ai_global_config` 两条距离阈值
- 场景 `knowledge.handbook.qa`、提示词模板、权限 `biz.ai.kb.qa` 与 `biz.ai.kb.admin`
- 权限赋给 `SYS_ADMIN`、`biz_all`
- `AiCodes` 增加场景码与权限码
- EF 实体与 `ApplicationDbContext` 映射文档、版本、缓存；块的 `embedding` 排除在 LINQ 之外

提示词入库不得在脚本文件中书写双花括号，用 CHR 拼接。

**完成标准：** 脚本重复执行不报错；AI 配置页能看到新场景；未配置向量模型时 profile 为禁用。

### 步骤 2 — 切块与单测

实现 `HandbookChunker`（纯函数）和 docx 段落读取。单测覆盖：

- 目录被跳过，正文从第二次「前言：使用说明」开始
- `第10章` / `10.1` 写入章号、节号
- 超长节被切开，且第二块开头含约 150 字重叠
- 短于 80 字的节并入下一块
- 【标准】字样保留在 `content` 中

测试用仓库内的短 docx 夹具，不要把完整教材提交进 Git。

**完成标准：** `HandbookChunker` 相关测试通过。

### 步骤 3 — Embedding 客户端

实现批量 embeddings 与 Mock 向量。Mock 对同一文本始终得到同一单位向量。

**完成标准：** Mock 单测不访问网络；真实客户端在步骤 0 的环境上对 2 条文本返回 2 条 1024 维向量。

### 步骤 4 — 导入

`KbIngestHostedService` 轮询 `pending` / 可继续的 `failed` 版本，一次处理一个版本：

1. 解析并切块，写入 `kb_chunk`（尚无向量）
2. `status = embedding`
3. 按批写入向量
4. `status = ready`；若该文档没有启用版本，则本版本 `is_active = true` 并回写 `kb_document.active_version_id`
5. 异常则 `failed` 与 `error_message`

上传 API 只负责存文件和插入 `pending` 行。同一 `source_sha256` 已有 `ready` 版本时返回冲突，不重复嵌入。

**完成标准：** 用短夹具 docx 走完 pending → ready；失败后 resume 只补空向量。

### 步骤 5 — 检索与作答

`KbHandbookQaService`：

1. 读启用版本与 embedding profile
2. 查 `kb_ask_cache`
3. 嵌问题、按 `<=>` 排序
4. 按第 6.4 节阈值决定未覆盖或调用 `IAiOrchestrator`
5. 拼教材声明与合规句
6. 写 `kb_ask_cache`

检索 SQL 只查 `is_active` 且 `status = ready` 的版本，并忽略 `embedding IS NULL` 的块。

单测用 Mock 向量：构造两块，其中一块与问题的 Mock 向量相同，断言选中该块且旧版本的块不会被选中。

**完成标准：** 版本隔离、阈值未覆盖、命中后编排入参只含选中块，这三项测试通过。

### 步骤 6 — API 与前端

按第 8、9 节接上页面与菜单。管理页能看到块标题和状态；问答页展示答案与摘录。

**完成标准：** 有 `biz.ai.kb.qa` 的用户能打开问答页；只有管理权限的用户能上传并启用版本。无权限用户看不到对应菜单。

### 步骤 7 — 导入正式教材并校准

1. 在 AI 配置中把 `knowledge.handbook.qa` 的厂商从 mock 改为实际对话模型。
2. 启用 `kb_embedding_profile` 的真实模型。
3. 上传《电子元器件分销行业新人培养教材（行业通用版）.docx》，等待 `ready` 并确认已启用。
4. 抽查切块：目录没有进入正文块；`10.1` 的 GP 内容在对应节的块里。
5. 用下面三类问题看 Top1 距离和答案，再决定是否调整两个阈值并清 `kb_ask_cache`。

| 类型 | 示例 | 期望 |
|------|------|------|
| 有标准答案 | GP 公式是什么 | 覆盖，摘录含第 10 章，数字与教材一致 |
| 有出处的流程 | 货品状态有哪几档 | 覆盖，摘录含第 2 章 2.5 |
| 教材没有的系统操作 | 装箱单在系统哪个菜单 | 未覆盖，不编造菜单路径 |
| 合规 | 出口管制销售要做什么 | 覆盖教材原文，并带合规固定句 |

**完成标准：** 上表四类结果符合期望；问答缓存在同一版本、同一问题上第二次不再请求模型。

### 步骤 8 — 帮助与测试对照

- `help/pages/` 用业务语言说明培训问答的用途和「不是公司制度」
- `document/QA/` 测试对照说明：先读测试规范总纲再写。本功能无金额核销；文档中写明金额边界不适用。须包含：覆盖成功、未覆盖、无权限、启用新版本后旧答案失效

---

## 11. 验收时看的失败

| 现象 | 先查 |
|------|------|
| 导入一直 pending | HostedService 是否运行；profile 是否启用；API Key 环境变量 |
| 扩展或类型不存在 | 该 PostgreSQL 实例是否安装 pgvector |
| 维度错误 | 模型是否返回 1024；禁止把 1536 维写入 `vector(1024)` |
| 答非所问 | 管理页打开命中块。块错了就调阈值或切块；块对了再改提示词 |
| 换了 Word 仍是旧答案 | 是否启用了新版本；`kb_ask_cache` 是否仍指向旧 `document_version_id` |
| 目录句子被当成正文 | 切块单测与该版本前几块的 `heading` |

---

## 12. 修订记录

| 日期 | 说明 |
|------|------|
| 2026-09-23 | 草案：切块、pgvector、LLM 问答、分步落地。首份文档为分销行业新人教材 |
| 2026-09-24 | 向量配置定为 SiliconFlow `BAAI/bge-m3`，环境变量名 `AI_EMBEDDING_API_KEY` |
