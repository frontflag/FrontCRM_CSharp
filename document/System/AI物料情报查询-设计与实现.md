# AI 物料情报查询（material.intel.lookup）设计与实现

**文档版本：** v1.1  
**更新日期：** 2026-08-18  
**项目名称：** FrontCRM_CSharp  
**关联文档：** [AI模块架构与实现](./AI模块架构与实现.md) · [AI模块PRD](../PRD/AI模块PRD.md)

---

## 1. 文档目的与范围

本文档描述 RFQ 首页 **「AI 查询」物料情报** 功能的产品设计、技术架构与实现细节。该功能对应 AI 场景码 **`material.intel.lookup`**，与 Debug 页的 **`material.spec.lookup`**（PN+品牌查规格）相互独立。

**读者：** 产品、前端、后端、运维。

**不在本文范围：** AI 平台通用能力（厂商抽象、管理页、限流框架）的完整说明，见 [AI模块架构与实现](./AI模块架构与实现.md)。

---

## 2. 产品概述

### 2.1 用户场景

采购/销售在 RFQ 首页输入 **型号（PN）**，点击 **「AI 查询」**，获取该型号的结构化情报，包括：

- 品牌与规格参数（含型号解析、电气参数、DataSheet / 产品图链接）
- 应用领域、可替代料、价格与渠道、行业新闻

**与「本地 PN 库」的区别：** 本地 PN 库走 CRM 已有物料主数据；AI 查询走大模型 + 可选联网搜索，结果带固定免责声明，仅供参考。

### 2.2 入口与权限

| 项 | 说明 |
|----|------|
| 页面 | RFQ 首页 `RFQHome.vue`，路由 `/rfq` |
| 按钮 | 搜索框内「AI 查询」 |
| 权限 | `biz.ai.material_intel.lookup` |
| 无权限 | 按钮禁用，Tooltip 提示无权限 |

拥有 `rfq.read` 的角色在种子 SQL 中会自动关联该 AI 权限（与 `SYS_ADMIN` / `biz_all` 一并授予）。

### 2.3 交互要点

- 加载中显示秒级计时：`正在查询物料情报，请稍候…（N 秒）`
- 结果区顶部固定提示：**本信息由AI获取，仅供参考**（不展示 AI 返回的长 `disclaimer` 原文）
- 支持「缓存 / 实时」标签、「关闭」与「复制 JSON」
- 调用超时：前端 `aiApi.invoke` 超时 180s；生产 Nginx 需 `proxy_read_timeout >= 300s`（见 `scripts/nginx-ai-invoke-timeout.snippet.conf`）

---

## 3. 总体架构

```
┌─────────────────────────────────────────────────────────────────┐
│  RFQ 首页 (RFQHome.vue)                                          │
│  输入 pn → aiApi.invoke(material.intel.lookup)                   │
│  parseAiJsonObject → MaterialIntelResultPanel                    │
└────────────────────────────┬────────────────────────────────────┘
                             │ POST /api/v1/ai/invoke  (JWT)
┌────────────────────────────▼────────────────────────────────────┐
│  AiController → AiOrchestrator                                   │
│  · 权限 / 限流 / 缓存指纹 (pn)                                   │
│  · 渲染 Prompt + 注入 json_schema_hint                           │
│  · material.intel 专用中文 guard + datasheet/image 提醒          │
│  · Moonshot 联网搜索 ($web_search)                               │
│  · LLM → 解析 JSON → 写日志 / 写缓存 (jsonb 安全校验)            │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│  MaterialIntelResultPanel → JsonValueRenderer (通用 JSON 渲染)   │
│  · Enhancer 表格 / 行式布局 / 面板栈                             │
│  · i18n + FIELD_LABEL_ZH 中文标签                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. 后端设计

### 4.1 场景配置（`ai_scenario`）

| 字段 | 典型值 | 说明 |
|------|--------|------|
| `code` | `material.intel.lookup` | 场景码 |
| `name` | AI 物料情报查询 | |
| `provider_code` | `moonshot` / `mock` | 生产用 Moonshot |
| `model` | `kimi-k2.5` | 联网场景需 k2.5/k2.6，非 k2.7 |
| `prompt_template_id` | 关联 `material.intel.lookup` v1 模板 | |
| `cache_ttl_seconds` | `7776000`（90 天） | 按 PN 缓存 |
| `cache_key_fields` | `["pn"]` | 指纹仅含 PN |
| `allowed_input_fields` | `["pn"]` | 请求体 `input.pn` |
| `enable_web_search` | `true` | Moonshot `$web_search` |
| `permission_code` | `biz.ai.material_intel.lookup` | |
| `rate_limit_per_user_per_min` | `10` | | |

常量定义：`CRM.Core/Constants/AiCodes.cs`（`AiScenarioCodes.MaterialIntelLookup`、`AiPermissionCodes.MaterialIntelLookup`）。

### 4.2 提示词与 JSON 契约 v2

**模板表：** `ai_prompt_template`，`code = material.intel.lookup`，`version = 1`。

**运行时拼接（`AiOrchestrator.InvokeAsync`）：**

1. `user_prompt` = 模板 `{{pn}}` 替换 + 若存在则追加 `【JSON 结构要求】` + `json_schema_hint`
2. `material.intel.lookup` 额外追加 system/user  guard：简体中文、datasheet_url / image_url

**顶层 JSON 结构（契约 v2，2026-06）：**

| 键 | 类型 | UI 区块 |
|----|------|---------|
| `brand_info` | object | 品牌信息 |
| `spec_params` | object | 规格参数（含 breakdown、electrical_params、URL） |
| `application_areas` | string[] | 应用领域 |
| `industry_news` | `{title, url, summary}[]` | 市场新闻与行业动态 |
| `alternatives` | `{part_number, brand, note}[]` | 可替代料（表格） |
| `pricing` | object | 价格（含 market_price、market_conditions、price_tiers、distributors） |
| `disclaimer` | string | 仅存于 JSON，UI 不展示原文 |

完整 `json_schema_hint` 见：

- `scripts/ai_material_intel_schema_v2_postgresql.sql`
- 迁移 `20260805100000_MaterialIntelLookupSchemaV2.cs`
- 前端校验 `CRM.Web/src/utils/materialIntelSchema.ts`

**Prompt 变更后务必清缓存：**

```sql
DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
```

### 4.3 调用链路摘要

与通用 AI 模块一致，参见 [AI模块架构与实现 §5](./AI模块架构与实现.md#5-调用链路)。

**本场景特有点：**

| 环节 | 实现 |
|------|------|
| 输入 | 仅 `pn`，经 `AiJsonHelper.FilterInput` |
| 缓存键 | SHA256(`scenario|model|templateVersion|webSearch|fingerprintJson`) |
| JSON 写入 PG | `response_json` 仅写入 **可解析** 的 JSON 对象（`AiJsonHelper.ExtractJsonObjectText`）；失败则 `null`，完整文本在 `response_content` |
| 缓存失败 | 记录 Warning，**不阻断** 业务返回（避免 22P02 导致用户看不到结果） |
| 联网 | `enable_web_search=true` 时走 Moonshot；Orchestrator 校验 k2.7 不兼容 |

### 4.4 API 示例

**请求：**

```http
POST /api/v1/ai/invoke
Authorization: Bearer {jwt}

{
  "scenarioCode": "material.intel.lookup",
  "input": { "pn": "HMCG94AGBRA632N" }
}
```

**响应字段（`data`）：** `invocationId`、`fromCache`、`content`（原始字符串）、`data`（解析后的 object）、`usage`、`scenarioCode`、`providerCode`、`model`。

---

## 5. 前端设计

### 5.1 页面与组件层次

```
RFQHome.vue
├── 搜索框 + AI 查询 / 本地 PN 库
├── 加载态（秒级计时）
└── MaterialIntelResultPanel.vue
    ├── 固定免责声明 Alert
    ├── JsonValueRenderer.vue (root)
    │   ├── JsonBlockPanel.vue          # 独立面板外框
    │   ├── JsonEnhancerTable.vue       # 型号解析 / 阶梯价 / 可替代料表
    │   ├── IndustryNewsItemView.vue    # 新闻条目
    │   ├── LabeledRowsView.vue         # 市场行情等 Key：Value 行
    │   └── 通用 kv-grid / string-list / object-list …
    └── 复制 JSON
```

### 5.2 面板顺序与标题

定义于 `CRM.Web/src/utils/jsonLabels.ts`：

**顶层面板顺序（`ROOT_SECTION_ORDER`）：**

1. `brand_info` → 品牌信息  
2. `spec_params` → 规格参数  
3. `application_areas` → 应用领域  
4. `industry_news` → 市场新闻与行业动态  
5. `alternatives` → 可替代料  
6. `pricing` → 价格（始终最后）

中文标题：`materialIntel.sections.*` + `FIELD_LABEL_ZH` 兜底。

### 5.3 渲染策略（Phase 1 / 2 / 3）

| 阶段 | 思路 | 关键文件 |
|------|------|----------|
| Phase 1 | 递归通用 JSON 渲染，不丢字段 | `JsonValueRenderer.vue`、`jsonDisplay.ts` |
| Phase 2 | 独立面板栈 + Enhancer 优化常见结构 | `materialIntelJsonEnhancers.ts`、`JsonBlockPanel.vue` |
| Phase 3 | Prompt / schema / 渲染器契约对齐 + Debug 对照 | `materialIntelSchema.ts`、`DebugMaterialIntel.vue` |

**Enhancer 匹配（优先级高者优先）：**

| id | 字段 | 模式 | 展示 |
|----|------|------|------|
| `part_number_breakdown` | `part_number_breakdown` | breakdown-table | 段 / 含义 表 |
| `price_tiers` | `price_tiers` | price-tiers-table | 数量 / 单价 |
| `alternatives_list` | `alternatives` | alternatives-table | 型号 / 品牌 / 说明 |
| `industry_news_list` | `industry_news` | industry-news-list | 外层面板 + 条目标题/摘要 |
| `market_conditions_rows` | `market_conditions` | labeled-rows | 库存 / 走势 / 说明 各行 |
| `market_price_rows` | `market_price` | labeled-rows | 同上 |
| `pricing_distributors` | `distributors` | object-list | 渠道卡片（标题=渠道名，隐藏重复字段） |

未匹配 Enhancer 的字段仍走通用渲染，**保证数据可见**。

### 5.4 UI 规范（已实现）

- 顶部免责声明：固定文案 `materialIntel.disclaimerShort`，不用 AI 长文
- 嵌套对象（如 `electrical_params`）：父面板有标题时，内层不再重复标题行
- 可替代料：表格三列，无卡片标题行
- 市场新闻：外层面板含 section 标题；每条新闻仅标题（加粗）+ 摘要，无「标题：」「摘要：」前缀
- 渠道报价：列表项标题下无分隔横线（`JsonBlockPanel.headDivider=false`）；内层无重复线框

### 5.5 工具与常量

| 文件 | 职责 |
|------|------|
| `api/ai.ts` | `AI_SCENARIO_MATERIAL_INTEL_LOOKUP`、`AI_PERMISSION_MATERIAL_INTEL_LOOKUP` |
| `utils/aiJson.ts` | 从 `content` 去 markdown 围栏解析 JSON |
| `utils/jsonLabels.ts` | 面板顺序、i18n 标签、URL 链接文案 |
| `utils/materialIntelSchema.ts` | 契约 v2 校验（Debug 对照） |
| `utils/clipboard.ts` | 复制 JSON 降级 |

---

## 6. Debug 与验收

### 6.1 Debug 对照页

| 项 | 说明 |
|----|------|
| 路由 | `/debug/material-intel`（`sysAdminOnly`） |
| 页面 | `DebugMaterialIntel.vue` |
| 能力 | 在线 AI 查询 / 粘贴 JSON；左结构化预览、右原始 JSON、底部契约 v2 校验报告 |

### 6.2 契约校验

`validateMaterialIntelJson()` 输出 `error` / `warn` / `info`：

- 缺键、类型不符、非 snake_case
- v1 旧格式（`alternatives` 为 string[] 等）标 `deprecated`

---

## 7. 部署与运维

### 7.1 脚本与迁移（推荐顺序）

| 顺序 | 文件 | 说明 |
|------|------|------|
| 1 | `scripts/ai_module_postgresql.sql` | 全量 AI 表 + 两场景种子（新库） |
| 2 | `scripts/ai_material_intel_lookup_postgresql.sql` | 增量：场景 + 权限 + 联网 |
| 3 | `scripts/ai_material_intel_datasheet_image_prompt_postgresql.sql` | DataSheet / 图片 Prompt |
| 4 | `scripts/ai_material_intel_schema_v2_postgresql.sql` | **契约 v2** Prompt + schema + 清缓存 |
| 生产一站式 | `scripts/ai_production_deploy_postgresql.sql` | 含上述内容汇总 |

EF 迁移（按需）：`20260804100000_MaterialIntelLookupScenario` → `20260804140000_*` → `20260805100000_MaterialIntelLookupSchemaV2`。

### 7.2 环境要求

```powershell
# Moonshot API Key（用户级或 launchSettings，勿提交仓库）
[System.Environment]::SetEnvironmentVariable("AI_MOONSHOT_API_KEY", "sk-...", "User")
```

- 国内端点：`https://api.moonshot.cn/v1`
- 场景 Model：`kimi-k2.5`（联网）
- 部署 **CRM.API** + **CRM.Web** 后，在管理页确认场景 Provider/Model

### 7.3 Navicat / DBeaver 执行 SQL 注意

含 `{{pn}}`、`{{raw_text}}` 等 AI 模板占位符时，**不可**在脚本中以明文或注释形式出现双花括号（会弹出「绑定参数」）。须用 **hex + CHR 拼接** 写入；完整规则见 **[PostgreSQL 增量脚本编写规范](../../PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md)**。

参考脚本：

- `scripts/ai_material_intel_schema_v2_postgresql.sql`（`{{pn}}`）
- `scripts/ai_entity_parse_postgresql.sql`（`{{raw_text}}`）

### 7.4 常见问题

| 现象 | 原因 | 处理 |
|------|------|------|
| 500 + 22P02 jsonb | 缓存列写入非法 JSON | 已修复：`ExtractJsonObjectText` 校验；升级 API |
| 504 超时 | Nginx / 浏览器断开 | 调大 `proxy_read_timeout`；LLM 耗时长属正常 |
| 仍返回英文/旧结构 | 命中旧缓存 | 执行清缓存 SQL；确认 v2 Prompt 已入库 |
| 401 Moonshot | Key 与 base_url 区域不匹配 | 国内 Key + `.cn` 端点 |
| 429 / 额度不足 | 厂商账号余额不足或套餐暂停 | 管理员在 AI 配置页核对密钥与账单后充值；界面只提示「额度不足」，不展示厂商 JSON / 组织号 / 密钥片段 |

---

## 8. 关键文件索引

### 8.1 后端

| 路径 | 说明 |
|------|------|
| `CRM.Infrastructure/Ai/AiOrchestrator.cs` | 编排、material.intel guard、缓存/日志 |
| `CRM.Infrastructure/Ai/AiJsonHelper.cs` | 模板渲染、JSON 提取与 jsonb 安全 |
| `CRM.API/Controllers/AiController.cs` | `/api/v1/ai/invoke` |
| `CRM.Core/Constants/AiCodes.cs` | 场景码与权限码 |
| `CRM.Core/Utilities/AiProviderUserError.cs` | 厂商 HTTP 错误 → 用户可见文案（不回传响应体） |
| `CRM.Infrastructure/Migrations/20260805100000_MaterialIntelLookupSchemaV2.cs` | 契约 v2 DB 更新 |

### 8.2 前端

| 路径 | 说明 |
|------|------|
| `CRM.Web/src/views/RFQ/RFQHome.vue` | 业务入口 |
| `CRM.Web/src/components/RFQ/MaterialIntelResultPanel.vue` | 结果容器 |
| `CRM.Web/src/components/RFQ/JsonValueRenderer.vue` | 递归渲染核心 |
| `CRM.Web/src/utils/materialIntelJsonEnhancers.ts` | Enhancer 注册 |
| `CRM.Web/src/views/Debug/DebugMaterialIntel.vue` | Debug 对照 |

### 8.3 与 material.spec.lookup 对比

| 维度 | material.intel.lookup | material.spec.lookup |
|------|----------------------|---------------------|
| 入口 | RFQ 首页 AI 查询 | Debug `/debug/ai` |
| 输入 | 仅 `pn` | `pn` + `brand` |
| 输出体量 | 多板块情报 JSON | 精简规格 JSON |
| 联网 | 默认开启 | 通常关闭 |
| 权限 | `biz.ai.material_intel.lookup` | `biz.ai.material_spec.lookup` |

---

## 9. 后续扩展（规划）

- 报价单 / 明细行内嵌「AI 查情报」
- 服务端返回 schema 校验摘要（可选）
- SSE 流式进度（讨论中，未实现）
- Prompt 版本化与 A/B（管理页选 template version）

---

**文档维护：** 变更 Prompt 契约、Enhancer 或 RFQ 交互时，请同步更新本文 §4.2、§5.3 与 `materialIntelSchema.ts`。
