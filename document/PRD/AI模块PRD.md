# AI 模块 PRD

**文档版本：** v1.0  
**更新日期：** 2026-06-24  
**项目名称：** FrontCRM_CSharp（AI智销系统）  
**关联技术文档：** [AI模块架构与实现](../System/AI模块架构与实现.md)

---

## 1. 背景与目标

### 1.1 问题描述

电子元器件贸易业务中，业务员、采购员在报价、录单、对料时经常需要查询 **PN（型号）+ 品牌** 对应的规格参数（封装、电压、工作温度等）。当前主要依赖：

- 人工查阅官网、Datasheet、第三方网站
- 个人经验与历史订单记忆
- 外部工具零散查询，结果无法沉淀、无法审计

上述方式 **耗时长、口径不统一、无法与 CRM 权限体系联动**，也不便于控制外部 API 成本。

### 1.2 产品目标

建设一套 **可配置、可扩展、可管控** 的 AI 能力平台，使业务场景以「**场景码 + 结构化输入**」方式调用大模型，并满足：

| 目标 | 说明 |
|------|------|
| **业务可用** | 首个场景「物料规格查询」可在 Debug / 后续业务页使用 |
| **成本可控** | 响应缓存、用户/全站调用配额、调用日志可审计 |
| **安全合规** | API Key 不入库；按场景 RBAC；输出带置信度与免责声明 |
| **运维可配** | 管理员可切换厂商/模型/Prompt，无需发版改代码 |
| **平滑上线** | 默认 Mock 厂商，开发/测试无 Key 也可联调 |

### 1.3 目标用户

| 角色 | 诉求 |
|------|------|
| **业务员 / 采购员** | 快速查 PN 规格，辅助报价与录单 |
| **系统管理员** | 配置 AI 厂商、场景、模板，查看用量与日志 |
| **研发 / 运维** | 接入新场景、部署 Key 与端点（详见技术文档） |

---

## 2. 用户场景

### 场景一：业务员查询物料规格（首期）

销售在处理 RFQ 或报价时，拿到客户提供的 PN `HMCG94AGBRA632N` 与品牌「海力士」，希望在 CRM 内一键查询封装、电压等参数，用于填表或回复客户。

**期望：** 输入 PN + 品牌 → 数秒内得到结构化 JSON → 可复制；相同 PN+品牌 7 天内再次查询走缓存、响应更快。

### 场景二：管理员切换真实大模型

公司申请 Kimi（Moonshot）API Key 后，管理员在 **系统 → AI 配置** 中：

1. 确认厂商 Base URL（国内 `api.moonshot.cn` / 国际 `api.moonshot.ai`）
2. 将场景 `material.spec.lookup` 的 Provider 从 `mock` 改为 `moonshot`，Model 选 `kimi-k2.5`
3. 运维在服务器配置环境变量 `AI_MOONSHOT_API_KEY` 并重启 API

**期望：** 业务用户无感知切换，Debug 页查询返回真实 AI 结果；调用日志可看到 Token 与耗时。

### 场景三：管理员排查调用失败

用户反馈「查规格报错」。管理员打开 **AI 配置 → 调用日志**，按场景筛选，查看状态、错误信息、是否命中缓存、耗时与 Token。

**期望：** 可区分 401（Key/端点问题）、400（参数不兼容）、限流、权限不足等，无需查服务器日志即可完成一级排查。

### 场景四：后续扩展（规划，未实现）

- 在 **报价单 / 销售订单明细** 行内嵌入「AI 查规格」按钮
- BOM 快速报价时批量补全规格字段
- 新增场景：邮件摘要、合同条款提取等

---

## 3. 功能范围

### 3.1 本期包含（v1.0 已交付）

#### A. 通用 AI 调用能力

- 按 **场景码（scenarioCode）** 发起调用，传入结构化 **input**
- 支持 JSON 格式输出解析，返回 `content` + 结构化 `data`
- 命中缓存时明确标识 `fromCache`
- 每次调用写入 **调用日志**（成功 / 失败 / 缓存命中）

#### B. AI 配置管理（管理端）

路径：**系统 → AI 配置**（`/system/ai-config`）

| Tab | 能力 |
|-----|------|
| **厂商** | 查看/编辑 Base URL、API Key 环境变量名、默认模型、超时、启用状态 |
| **场景** | 查看/编辑名称、描述、Provider（下拉）、Model（下拉）、缓存 TTL、Max Tokens、Temperature、权限码、每分钟限流、启用 |
| **模板** | 编辑 System Prompt、User 模板（`{{pn}}` 等占位符）、JSON Schema 提示、是否激活 |
| **调用日志** | 时间、场景、状态、缓存、耗时(ms)、Token、错误摘要；支持按 scenarioCode 筛选 |
| **用量摘要** | 今日调用次数 / Token / 缓存命中 vs 全站日配额 |

#### C. Debug 调试页

路径：**Debug → AI 物料规格**（`/debug/ai`）

- 输入 PN、品牌，调用场景 `material.spec.lookup`
- 展示 JSON 结果（只读）、缓存/实时标签、厂商/模型/Token
- 支持复制 JSON

#### D. 权限与限流

- 场景级权限：`biz.ai.material_spec.lookup`
- 管理权限：`biz.ai.admin`
- 用户级：每场景 **N 次/分钟**（默认 10，不含纯缓存命中）
- 全站级：**日调用配额**（默认 5000，失败不计入配额统计逻辑见技术文档）

#### E. 首个业务场景：物料规格查询

| 项 | 约定 |
|----|------|
| 场景码 | `material.spec.lookup` |
| 输入 | `pn`（必填）、`brand`（必填） |
| 输出字段 | `package`、`voltage`、`temperature_range`、`description`、`confidence`（low/medium/high）、`disclaimer` |
| 业务规则 | 无法确认的字段填 `null`，禁止编造；输出纯 JSON |
| 默认缓存 | 7 天，键 = PN + 品牌 |

### 3.2 本期不包含

| 项 | 说明 |
|----|------|
| 流式输出（打字机效果） | 仅同步等待完整响应 |
| 业务页正式嵌入 | 除 Debug 外，报价/订单等页面尚未接入 |
| 多模态（图片/PDF） | 仅文本 |
| RAG / 向量库 / 知识库 | 依赖模型公开知识 + Prompt |
| API Key 在 UI 内录入 | 仅配置环境变量 **名称**，Key 由运维设置 |
| 全局配额 / Prompt 预览开关的 UI | 存于 `ai_global_config`，需 DBA/SQL 调整 |
| 自动切换 Moonshot 国内/国际端点 | 需管理员手动配置 Base URL |
| 场景的新增/删除 UI | 仅支持编辑已有种子场景；新增场景需研发 + SQL |

---

## 4. 核心概念（产品语言）

| 概念 | 产品定义 |
|------|----------|
| **厂商（Provider）** | 底层大模型服务方，如 Mock（测试）、Moonshot/Kimi（生产） |
| **场景（Scenario）** | 面向业务的一条 AI 能力，如「物料规格查询」，含模型、缓存、权限 |
| **模板（Prompt Template）** | 场景的提示词蓝图，分 System / User 两段，User 支持变量占位 |
| **调用** | 用户触发一次场景执行；可能走缓存或真实请求 LLM |
| **缓存** | 相同场景 + 模型 + 模板版本 + PN + 品牌 → 直接返回历史结果 |
| **Mock 厂商** | 不调用外部 API，返回固定结构数据，用于开发/demo |

---

## 5. 功能详细设计

### 5.1 AI 配置 — 厂商

**列表字段：** Code、名称、Base URL、API Key Env、默认模型、Timeout、是否启用

**编辑规则：**

- Base URL 必须为 OpenAI 兼容根地址，如 `https://api.moonshot.cn/v1`
- API Key Env 填环境变量名（如 `AI_MOONSHOT_API_KEY`），**禁止**填写 Key 明文
- 禁用厂商后，关联场景调用时将报错「厂商已禁用」

**Moonshot 端点选择（产品说明）：**

| 申请 Key 的平台 | 应填 Base URL |
|----------------|---------------|
| platform.moonshot.cn（国内） | `https://api.moonshot.cn/v1` |
| platform.moonshot.ai（国际） | `https://api.moonshot.ai/v1` |

Key 与端点不匹配时，用户侧表现为调用失败，日志显示 401。

### 5.2 AI 配置 — 场景

**列表字段：** Code、名称、Provider、Model、Permission、是否启用

**编辑交互：**

- **Provider**：下拉，仅展示已启用厂商（当前值若未启用仍可见）
- **Model**：下拉，随 Provider 变化；切换 Provider 时若当前 Model 无效则自动改为该厂商默认模型
- **Cache TTL (s)**：0 表示不缓存；物料规格默认 604800（7 天）
- **Temperature**：控制输出随机性；`kimi-k2.5` 由系统强制为 1（厂商限制），其他模型可用 0.3 等较低值以稳定 JSON
- **Rate/min**：单用户在该场景下每分钟最大 **非缓存** 调用次数

### 5.3 AI 配置 — 模板

**编辑内容：**

- System Prompt：角色与输出规范（如「只输出 JSON、不编造」）
- User Template：业务问句，如 `请查询物料规格：PN={{pn}}，品牌={{brand}}。`
- JSON Schema Hint：写入 Prompt 的字段说明，非 API 级 schema 校验
- 是否激活：停用后该模板不可被场景使用

**版本策略（v1）：** 模板以 `code + version` 区分；修改内容不改变 version 号时，缓存键中的 templateVersion 不变。重大 Prompt 变更应通过新增 version + 更新场景关联实现缓存隔离（需研发/DB 配合）。

### 5.4 AI 配置 — 调用日志

**列定义：**

| 列 | 含义 |
|----|------|
| 时间 | 调用发生时间（本地时区展示） |
| Scenario | 场景码 |
| Status | success / failed / cached |
| Cache | Y = 命中缓存，N = 实时 LLM |
| ms | 耗时毫秒（缓存命中通常接近 0） |
| Tokens | 总 Token（缓存命中为空） |
| Error | 失败原因摘要 |

**筛选：** 按 scenarioCode 模糊/精确过滤（输入框 + 刷新）

### 5.5 Debug — 物料规格查询

**页面结构：**

1. 标题与场景说明（含返回 Debug 入口）
2. 查询参数：PN、品牌（均必填）
3. 按钮「查询规格」— 加载中禁用重复提交
4. 结果区：缓存/实时标签、厂商、模型、Token、日志 ID、JSON 预览、复制按钮

**错误提示（用户可见）：**

- 未登录 / 无权限
- 调用过于频繁
- 已达全站日配额
- 厂商未配置 Key、认证失败、模型参数错误等（展示后端返回文案）

**产品免责声明（输出内）：**

模型返回 JSON 中应包含 `disclaimer` 字段，说明结果仅供参考、需人工核对 Datasheet。业务页正式接入时应保留该提示或等价 UI 文案。

### 5.6 物料规格输出规范

| 字段 | 类型 | 说明 |
|------|------|------|
| package | string \| null | 封装，如 FBGA、SOP-8 |
| voltage | string \| null | 电压规格 |
| temperature_range | string \| null | 工作温度范围 |
| description | string \| null | 简要描述 |
| confidence | string | low / medium / high |
| disclaimer | string | 免责说明 |

**产品原则：** 查不到填 `null`，不得虚构；`confidence` 低时应引导用户人工核实。

---

## 6. 权限设计

| 权限码 | 名称 | 适用角色（种子） | 能力 |
|--------|------|------------------|------|
| `biz.ai.admin` | AI 配置管理 | SYS_ADMIN、biz_all | 厂商/场景/模板/日志/用量 |
| `biz.ai.material_spec.lookup` | AI 物料规格查询 | SYS_ADMIN、biz_all | 调用 `material.spec.lookup` |

**规则：**

- 系统管理员（SysAdmin）可调用所有已启用场景，不受场景 permission 限制
- 普通用户须具备场景对应权限
- Debug 页路由为 `sysAdminOnly`，与业务权限独立（仅管理员可进 Debug 页，但 invoke 仍校验场景权限）

**后续业务页接入时：** 仅授予 `biz.ai.material_spec.lookup` 即可，无需开放 `biz.ai.admin`。

---

## 7. 业务规则

### 7.1 缓存

- 生效条件：场景 `cache_ttl_seconds > 0`
- 键维度：场景码 + 模型 + 模板版本 + 配置的 `cache_key_fields`（物料规格为 pn + brand）
- 命中行为：不写 LLM 请求，日志 status = `cached`，不计入用户分钟限流的「真实调用」
- **切换厂商/模型后：** 旧缓存可能仍返回 Mock 或旧模型结果 → 管理员可等待 TTL 过期，或由 DBA 清理 `ai_invocation_cache`

### 7.2 限流与配额

| 类型 | 默认 | 触发提示 |
|------|------|----------|
| 用户/场景/分钟 | 10 | 「调用过于频繁，请稍后再试」 |
| 全站/日 | 5000 | 「已达全站 AI 日调用配额」 |

配额与限流参数可在场景中调整（用户级）或通过 SQL 调整 `daily_quota_limit`（全站级）。

### 7.3 成本与用量

管理端顶部展示：

- 今日调用次数（相对日配额）
- 今日 Token 合计
- 今日缓存命中次数

**产品建议：** 缓存命中率高表示成本可控；Token 突增需结合日志排查是否 Prompt 过长或异常重试。

### 7.4 API Key 与部署

- Key ** never ** 出现在数据库或前端
- 运维在 API 进程环境中配置 `AI_MOONSHOT_API_KEY`
- 修改用户级环境变量后须 **重启 API 进程**（或 IDE）方生效

---

## 8. 交互与文案

### 8.1 菜单与路由

| 入口 | 路径 | 可见条件 |
|------|------|----------|
| 系统 → AI 配置 | `/system/ai-config` | `biz.ai.admin` |
| Debug → AI 物料规格 | `/debug/ai` | 系统管理员 |

### 8.2 关键状态文案

| 状态 | 用户侧文案建议 |
|------|----------------|
| 调用成功（实时） | 成功 + 结果 JSON |
| 调用成功（缓存） | 标签「缓存」+ 结果 JSON |
| 无权限 | 当前账号无权使用该 AI 场景 |
| 厂商 Key 缺失 | AI 厂商 xxx 未配置 API Key 环境变量 xxx |
| 认证失败 | AI 调用失败 (401): Invalid Authentication（需管理员检查 Key 与端点） |

---

## 9. 验收标准（v1.0）

### 9.1 管理端

- [ ] 具备 `biz.ai.admin` 的用户可打开 AI 配置页，四个 Tab 数据可加载
- [ ] 可编辑 Moonshot 厂商 Base URL 并保存
- [ ] 场景 Provider / Model 为下拉选择，保存后再次打开回显正确
- [ ] 调用日志列宽正常，Cache / ms / Tokens 标题不折行、数值不截断
- [ ] 用量卡片展示今日调用、Token、缓存命中

### 9.2 Debug 页

- [ ] Mock 模式下输入 PN+品牌可返回 JSON，无需 API Key
- [ ] 切换为 moonshot + 正确 Key/端点后返回真实 AI 结果
- [ ] 相同 PN+品牌 第二次查询显示「缓存」
- [ ] 无 `biz.ai.material_spec.lookup` 的用户调用 invoke 被拒绝（若开放非 Admin Debug 路由时）

### 9.3 权限与安全

- [ ] 无 `biz.ai.admin` 无法访问管理 API
- [ ] 数据库与 API 响应中不出现 API Key 明文
- [ ] 每次 invoke 在 `ai_invocation_log` 有记录

### 9.4 首个场景质量

- [ ] 返回 JSON 含约定字段；无法确认字段为 null
- [ ] 含 confidence 与 disclaimer

---

## 10. 上线与运营 checklist

1. 执行数据库迁移或 `scripts/ai_module_postgresql.sql`
2. 为业务角色分配 `biz.ai.material_spec.lookup`
3. 为管理员分配 `biz.ai.admin`
4. 配置 `AI_MOONSHOT_API_KEY` 并重启 API
5. AI 配置 → 厂商：确认 Base URL（国内/国际）
6. AI 配置 → 场景：`material.spec.lookup` → moonshot / kimi-k2.5
7. Debug 页验证成功后，再规划业务页嵌入
8. 监控调用日志与 Token 用量，按需调整缓存 TTL 与日配额

---

## 11. 路线图（建议）

| 阶段 | 内容 | 优先级 |
|------|------|--------|
| **v1.0** | 平台 + 配置 + Debug 物料规格 | ✅ 已完成 |
| **v1.1** | 报价单/销售明细行内「查规格」按钮 | 高 |
| **v1.2** | 管理端编辑全局日配额；一键清理场景缓存 | 中 |
| **v2.0** | 新场景：BOM 描述生成、邮件摘要 | 中 |
| **v2.x** | 流式输出、附件/PDF 解析 | 低 |

---

## 12. 风险与约束

| 风险 | 影响 | 缓解 |
|------|------|------|
| 模型幻觉 | 错误规格导致报价/采购风险 | Prompt 要求 null + disclaimer；UI 提示人工核对 |
| 外部 API 不可用 | 查询失败 | 日志告警；可临时切回 Mock（仅测试环境） |
| 成本超支 | Token 费用 | 缓存、日配额、用户限流 |
| Key 泄露 | 费用与安全 | Key 仅环境变量；轮换 Key 流程 |
| 缓存陈旧 | 切换模型后仍见旧结果 | 文档说明 + 后续清缓存工具 |

---

## 13. 变更历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-06-24 | 初版：平台能力、管理端、Debug 物料规格、权限与验收 |

---

## 14. 相关文档

| 文档 | 路径 |
|------|------|
| AI 模块架构与实现（研发） | `document/System/AI模块架构与实现.md` |
| RBAC 权限系统 | `document/PRD/RBAC权限系统PRD.md` |
| 数据库脚本 | `scripts/ai_module_postgresql.sql` |
