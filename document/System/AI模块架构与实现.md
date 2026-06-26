# AI 模块架构与实现

**文档版本：** v1.0  
**更新日期：** 2026-06-24  
**项目名称：** FrontCRM_CSharp  
**适用对象：** 后端 / 前端开发、运维、产品

---

## 1. 文档目的与范围

本文档描述 CRM 系统中 **通用 AI 调用架构** 的设计原则、分层结构、数据库模型、调用链路、配置方式及首个业务场景的实现，便于后续扩展更多 AI 场景（如报价辅助、文档解析等）。

**当前已实现：**

- 多厂商（Provider）抽象，支持 Mock 与 OpenAI 兼容 API（如 Kimi / Moonshot）
- 场景（Scenario）驱动：提示词模板、缓存、限流、权限均可配置
- PostgreSQL 缓存与调用日志
- 管理端配置页 + Debug 调试页
- 业务场景：`material.spec.lookup`（Debug：PN + 品牌查规格）
- 业务场景：**`material.intel.lookup`（RFQ 首页 AI 物料情报）** — 详见 [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)
- 业务场景：**`entity.parse.*`（7 类实体 AI 解析建单）** — 详见 [AI实体解析建单-设计与实现](./AI实体解析建单-设计与实现.md)

**不在本文范围：** 流式输出（SSE）、多模态、向量检索/RAG、全局配置的管理 UI（`ai_global_config` 目前仅数据库/种子维护）。**RFQ 物料情报 UI 渲染细节** 见专用文档；**实体解析建单交互与日志** 见专用文档，不在此重复。

---

## 2. 设计原则

| 原则 | 说明 |
|------|------|
| **场景驱动** | 业务只传 `scenarioCode` + 结构化 `input`；Prompt、模型、厂商、缓存策略由配置决定 |
| **密钥不入库** | API Key 通过环境变量名（`api_key_env`）引用，运行时 `Environment.GetEnvironmentVariable` 读取 |
| **缓存与日志在 PG** | 响应缓存在 `ai_invocation_cache`；审计与用量在 `ai_invocation_log` |
| **权限按场景** | 每个场景绑定 `permission_code`；系统管理员可 bypass |
| **Mock 优先开发** | 种子数据默认 `mock` 厂商，本地/Debug 无 Key 也可联调 |
| **OpenAI 兼容扩展** | 除 `mock` 外，统一走 `OpenAiCompatibleAiLlmProvider`（`/chat/completions`） |

---

## 3. 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│  CRM.Web（Vue 3）                                            │
│  AiConfigPage.vue  /  DebugAi.vue  /  api/ai.ts             │
└──────────────────────────┬──────────────────────────────────┘
                           │ REST / JWT
┌──────────────────────────▼──────────────────────────────────┐
│  CRM.API                                                     │
│  AiController（invoke / scenarios）                          │
│  AiAdminController（providers / scenarios / templates / logs）│
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  CRM.Infrastructure / Ai                                     │
│  AiOrchestrator ──► AiLlmProviderFactory                     │
│       │                    ├── MockAiLlmProvider              │
│       │                    └── OpenAiCompatibleAiLlmProvider  │
│       ├── AiAdminService                                     │
│       └── AiJsonHelper                                       │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  CRM.Core                                                    │
│  Models/Ai/*  ·  Interfaces/IAiServices.cs  ·  AiCodes.cs    │
└──────────────────────────┬──────────────────────────────────┘
                           │
                    PostgreSQL（6 张 AI 表）
```

### 3.1 关键文件索引

| 层级 | 路径 | 职责 |
|------|------|------|
| Core 实体 | `CRM.Core/Models/Ai/` | `AiProvider`、`AiScenario`、`AiPromptTemplate`、`AiInvocationCache`、`AiInvocationLog`、`AiGlobalConfig` |
| Core 接口 | `CRM.Core/Interfaces/IAiServices.cs` | `IAiOrchestrator`、`IAiAdminService`、`IAiLlmProvider`、DTO |
| Core 常量 | `CRM.Core/Constants/AiCodes.cs` | 厂商/场景/权限/状态码/全局配置键 |
| 编排 | `CRM.Infrastructure/Ai/AiOrchestrator.cs` | 权限、限流、缓存、LLM 调用、写日志 |
| 厂商工厂 | `CRM.Infrastructure/Ai/AiLlmProviderFactory.cs` | `mock` → 单例 Mock；其余 → OpenAI 兼容 |
| Mock 厂商 | `CRM.Infrastructure/Ai/MockAiLlmProvider.cs` | 开发用固定 JSON 响应 |
| 真实厂商 | `CRM.Infrastructure/Ai/OpenAiCompatibleAiLlmProvider.cs` | HTTP 调用 + 密钥解析 + kimi-k2 温度修正 |
| 管理 | `CRM.Infrastructure/Ai/AiAdminService.cs` | 配置 CRUD、日志列表、用量统计 |
| 工具 | `CRM.Infrastructure/Ai/AiJsonHelper.cs` | 输入过滤、`{{var}}` 渲染、指纹 JSON、SHA256、JSON 解析 |
| DbContext | `CRM.Infrastructure/Data/ApplicationDbContext.cs` | AI 实体映射（snake_case 列名） |
| DI | `CRM.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | 注册 AI 服务 |
| 业务 API | `CRM.API/Controllers/AiController.cs` | `POST /invoke`、`GET /scenarios` |
| 管理 API | `CRM.API/Controllers/AiAdminController.cs` | `/admin` 与 `/mgmt` 双路由 |
| 前端 API | `CRM.Web/src/api/ai.ts` | 调用封装（invoke 超时 180s） |
| 管理页 | `CRM.Web/src/views/System/AiConfigPage.vue` | 厂商 / 场景 / 模板 / 日志 |
| Debug 页 | `CRM.Web/src/views/Debug/DebugAi.vue` | 物料规格查询调试 |
| RFQ AI 查询 | `CRM.Web/src/views/RFQ/RFQHome.vue` | 物料情报业务入口 |
| 实体解析建单 | `CRM.Web/src/components/AiCreate/AiEntityCreateHost.vue` 等 | 见 [AI实体解析建单-设计与实现](./AI实体解析建单-设计与实现.md) |
| 解析 Normalize | `CRM.Infrastructure/Ai/EntityParse/EntityParseNormalizer.cs` | 后端 KV 规范化 |
| 解析质量日志 | `CRM.Infrastructure/Ai/AiEntityParseLogService.cs` | `ai_entity_parse_log` |
| 物料情报 UI | `CRM.Web/src/components/RFQ/MaterialIntelResultPanel.vue` 等 | 见 [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md) |
| Debug 对照 | `CRM.Web/src/views/Debug/DebugMaterialIntel.vue` | 契约 v2 结构化 vs 原始 JSON |
| 模型预设 | `CRM.Web/src/constants/aiProviderModels.ts` | 管理端 Model 下拉选项 |
| 迁移 | `CRM.Infrastructure/Migrations/20260803180000_AiModuleSchema.cs` | 建表 + 种子 + 权限 |
| SQL 脚本 | `scripts/ai_module_postgresql.sql` | 独立部署；编写须符合 [PostgreSQL 增量脚本编写规范](../PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md) |
| 实体解析 SQL | `scripts/ai_entity_parse_postgresql.sql` 等 | 7 场景 + 质量日志表 |

---

## 4. 数据模型

### 4.1 表关系（逻辑）

```
ai_provider (code)
     ▲
     │ provider_code
ai_scenario (code) ──► ai_prompt_template (id)
     │
     ├── ai_invocation_cache (scenario_code)
     └── ai_invocation_log (scenario_code)

ai_global_config (key-value，全站配额等)
```

数据库层 **未建外键**；关联由应用层校验。

### 4.2 表说明

#### `ai_provider` — AI 厂商

| 字段 | 说明 |
|------|------|
| `code` | 唯一标识，如 `mock`、`moonshot` |
| `base_url` | API 根地址，如 `https://api.moonshot.cn/v1` |
| `api_key_env` | 环境变量名，如 `AI_MOONSHOT_API_KEY`（**不是 Key 本身**） |
| `default_model` | 默认模型 |
| `timeout_seconds` | HTTP 超时 |
| `extra_headers` | JSONB，预留（当前 Provider 未读取） |
| `is_enabled` | 是否启用 |

#### `ai_prompt_template` — 提示词模板

| 字段 | 说明 |
|------|------|
| `code` + `version` | 唯一对；场景通过 `prompt_template_id` 引用 |
| `system_prompt` | 系统提示词 |
| `user_prompt_template` | 用户模板，支持 `{{pn}}`、`{{brand}}` 等占位符 |
| `output_format` | `json` 或 `text` |
| `json_schema_hint` | JSON 结构说明（写入 Prompt，非 API 强制 schema） |
| `is_active` | 是否可用 |

#### `ai_scenario` — 业务场景

| 字段 | 说明 |
|------|------|
| `code` | 唯一，如 `material.spec.lookup` |
| `provider_code` / `model` | 使用的厂商与模型 |
| `prompt_template_id` | 关联模板 |
| `cache_ttl_seconds` | 缓存 TTL（秒），0 表示不缓存 |
| `cache_key_fields` | JSON 数组，参与缓存键的 input 字段 |
| `allowed_input_fields` | JSON 数组，允许的 input 字段白名单 |
| `max_tokens` / `temperature` | LLM 参数 |
| `permission_code` | 调用所需权限 |
| `rate_limit_per_user_per_min` | 每用户每分钟上限（不含缓存命中） |

#### `ai_invocation_cache` — 响应缓存

| 字段 | 说明 |
|------|------|
| `cache_key` | SHA256 十六进制，唯一 |
| `request_fingerprint` | 规范化后的请求指纹 JSON |
| `response_content` / `response_json` | 缓存响应 |
| `expires_at` | 过期时间 |
| `hit_count` | 命中次数 |

**缓存键计算：**

```
SHA256( scenarioCode | model | templateVersion | canonicalFingerprint )
```

`canonicalFingerprint` 由 `cache_key_fields` 指定的 input 字段按固定顺序序列化。

#### `ai_invocation_log` — 调用日志

记录每次调用（含缓存命中）：场景、厂商、模型、用户、状态（`success`/`failed`/`cached`）、是否缓存、耗时、Token、错误信息、可选 `prompt_preview`。

#### `ai_global_config` — 全站配置

| 键 | 默认值 | 说明 |
|----|--------|------|
| `daily_quota_limit` | 5000 | 全站日调用上限（不含失败、不含纯缓存日志的策略见 orchestrator） |
| `prompt_preview_enabled` | true | 是否在日志中存 Prompt 预览 |
| `prompt_preview_max_chars` | 200 | 预览最大字符数 |

---

## 5. 调用链路

### 5.1 时序概览

```
前端 POST /api/v1/ai/invoke { scenarioCode, input, bizType?, bizId? }
  │
  ▼
AiOrchestrator.InvokeAsync
  ├─ 1. 加载场景（启用、未删除）
  ├─ 2. RBAC：scenario.permission_code（SysAdmin 跳过）
  ├─ 3. 加载模板（active）与厂商
  ├─ 4. 按 allowed_input_fields 过滤 input
  ├─ 5. 限流：用户/分钟 + 全站日配额
  ├─ 6. 计算 cache_key，查 ai_invocation_cache
  │      └─ 命中 → 写 cached 日志 → 返回 FromCache=true
  ├─ 7. 渲染 user_prompt（{{var}} 替换）
  ├─ 8. AiLlmProviderFactory.Create → ChatAsync
  ├─ 9. 写 success/failed 日志
  ├─10. 成功且 TTL>0 → upsert 缓存
  └─11. 返回 AiInvokeResultDto（content + 解析后的 data）
```

### 5.2 限流规则

- **用户级**：同一用户、同一场景，1 分钟内非缓存调用次数 ≤ `rate_limit_per_user_per_min`
- **全站级**：当日非缓存且非 `failed` 的调用总数 ≤ `daily_quota_limit`
- **缓存命中**：不计入用户分钟限流中的「真实 LLM 调用」逻辑（仍写 `cached` 日志）

### 5.3 权限

| 权限码 | 用途 |
|--------|------|
| `biz.ai.admin` | AI 配置管理（厂商/场景/模板/日志） |
| `biz.ai.material_spec.lookup` | 调用 `material.spec.lookup` 场景 |
| `biz.ai.material_intel.lookup` | 调用 `material.intel.lookup` 场景（RFQ 首页 AI 查询） |

种子数据将上述权限赋给 `SYS_ADMIN`、`biz_all` 角色；`material.intel.lookup` 另按 `rfq.read` 批量授予（见 SQL 脚本）。

---

## 6. LLM 厂商实现

### 6.1 Mock（`mock`）

- 类：`MockAiLlmProvider`（DI 单例）
- 无 HTTP；从 user 消息中提取 PN/品牌，返回固定结构 JSON
- 用于本地开发、单元测试、无 API Key 环境

### 6.2 OpenAI 兼容（`moonshot` 等）

- 类：`OpenAiCompatibleAiLlmProvider`
- 请求：`POST {base_url}/chat/completions`
- 认证：`Authorization: Bearer {apiKey}`
- 密钥：`ConfigurationAiSecretResolver` → `Environment.GetEnvironmentVariable(api_key_env)`

**Moonshot / Kimi 端点：**

| 平台 | Base URL | 说明 |
|------|----------|------|
| 国际 | `https://api.moonshot.ai/v1` | 种子默认 |
| 国内 | `https://api.moonshot.cn/v1` | 在 **AI 配置 → 厂商** 修改 `base_url` |

Key 与端点必须匹配：国内平台申请的 Key 不能用于 `.ai` 端点，否则 **401 Invalid Authentication**。

**kimi-k2.x 温度限制：**

`kimi-k2.5` 等模型 API 仅接受 `temperature = 1`。`OpenAiCompatibleAiLlmProvider` 对 `kimi-k2` 前缀模型自动强制为 `1.0`，避免 400 错误。其他模型使用场景配置的 temperature（如 `0.3` 更利于稳定 JSON 输出）。

---

## 7. API 接口

### 7.1 业务调用（需登录 JWT）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/v1/ai/invoke` | 按场景调用 AI |
| GET | `/api/v1/ai/scenarios` | 当前用户可调用场景列表 |

**Invoke 请求体：**

```json
{
  "scenarioCode": "material.spec.lookup",
  "input": { "pn": "HMCG94AGBRA632N", "brand": "海力士" },
  "bizType": "optional",
  "bizId": "optional"
}
```

**Invoke 响应（成功）：**

```json
{
  "data": {
    "invocationId": "...",
    "fromCache": false,
    "content": "{ ... }",
    "data": { "package": "...", "voltage": "..." },
    "usage": { "promptTokens": 0, "completionTokens": 0, "totalTokens": 0 },
    "scenarioCode": "material.spec.lookup",
    "providerCode": "moonshot",
    "model": "kimi-k2.5"
  }
}
```

权限校验在 **Orchestrator 内部**完成，非 Controller 上的 `[RequirePermission]`。

### 7.2 管理接口（需 `biz.ai.admin`）

路由前缀：`/api/v1/ai/admin` 与 `/api/v1/ai/mgmt`（等价）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/providers` | 厂商列表 |
| PUT | `/providers/{id}` | 更新厂商 |
| GET | `/templates` | 模板列表 |
| PUT | `/templates/{id}` | 更新模板 |
| GET | `/scenarios` | 场景列表 |
| PUT | `/scenarios/{id}` | 更新场景 |
| GET | `/logs?take=&scenarioCode=` | 调用日志 |
| GET | `/usage` | 今日用量摘要 |

---

## 8. 前端实现

### 8.1 路由与菜单

| 页面 | 路由 | 权限 / 门禁 |
|------|------|-------------|
| AI 配置 | `/system/ai-config` | `biz.ai.admin` |
| AI Debug | `/debug/ai` | `sysAdminOnly` |
| AI 物料情报对照 | `/debug/material-intel` | `sysAdminOnly` |
| RFQ 首页 AI 查询 | `/rfq`（或需求管理首页） | `biz.ai.material_intel.lookup` |

菜单项在 `AppLayout.vue`；i18n 键 `aiConfig.*`、`layout.menu.aiConfig`。

### 8.2 管理页功能（`AiConfigPage.vue`）

- **用量卡片**：今日调用、Token、缓存命中 vs 日配额
- **厂商**：编辑 Base URL、API Key Env、默认模型、超时、启用
- **场景**：Provider / Model **下拉选择**（Model 选项来自 `aiProviderModels.ts`）；Cache TTL、Temperature、权限、限流
- **模板**：System / User Prompt、JSON Schema Hint
- **调用日志**：时间、场景、状态、缓存、耗时、Token、错误

### 8.3 Debug 页（`DebugAi.vue`）

- 场景：`material.spec.lookup`
- 输入：PN、品牌
- 展示 JSON 结果（只读）
- 常量：`AI_SCENARIO_MATERIAL_SPEC_LOOKUP`（`api/ai.ts`）

### 8.4 RFQ 物料情报（`material.intel.lookup`）

业务入口、JSON 契约 v2、前端渲染栈、部署与 Debug 对照见专用文档：

**[AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)**

---

## 9. 配置与部署

### 9.1 环境变量

```powershell
# Windows 用户级（需新开终端 / 重启 IDE 后 API 进程才能读到）
[System.Environment]::SetEnvironmentVariable("AI_MOONSHOT_API_KEY", "sk-...", "User")
```

开发环境也可在 `CRM.API/Properties/launchSettings.json` 的 profile 中配置（**勿提交真实 Key**）。

验证 Key 与端点：

```powershell
curl.exe -s -w "\nHTTP %{http_code}\n" `
  -H "Authorization: Bearer $env:AI_MOONSHOT_API_KEY" `
  https://api.moonshot.cn/v1/models
```

### 9.2 从 Mock 切换到 Moonshot

1. **AI 配置 → 场景 → `material.spec.lookup`**
   - Provider：`moonshot`
   - Model：`kimi-k2.5`
2. **AI 配置 → 厂商 → `moonshot`**
   - Base URL：`https://api.moonshot.cn/v1`（国内）或 `.ai/v1`（国际）
   - API Key Env：`AI_MOONSHOT_API_KEY`
3. 设置环境变量并 **重启 API 进程**
4. 注意：场景默认 **7 天缓存**，切换厂商后相同 PN+品牌可能仍返回旧 Mock 结果，可换参数测试或清缓存：

```sql
DELETE FROM ai_invocation_cache WHERE scenario_code = 'material.spec.lookup';
```

### 9.3 数据库初始化

| 方式 | 文件 |
|------|------|
| EF 迁移 | `dotnet ef database update`（迁移 `20260803180000_AiModuleSchema`） |
| 手工 SQL | `scripts/ai_module_postgresql.sql` |

---

## 10. 首个业务场景：物料规格查询

| 项 | 值 |
|----|-----|
| 场景码 | `material.spec.lookup` |
| 输入 | `pn`、`brand` |
| 输出 JSON 字段 | `package`、`voltage`、`temperature_range`、`description`、`confidence`、`disclaimer` |
| 默认缓存 | 604800 秒（7 天），键字段 `pn` + `brand` |
| 调用权限 | `biz.ai.material_spec.lookup` |

Prompt 要求模型只输出 JSON、不编造、无法确认填 `null`。

---

## 11. 扩展新场景指南

1. **数据库**
   - 新增 `ai_prompt_template`（`code` + `version`）
   - 新增 `ai_scenario`（指定 provider、model、模板 ID、缓存与权限）
   - 在 `sys_permission` 增加场景权限并赋给角色
2. **Core**
   - 在 `AiScenarioCodes` / `AiPermissionCodes` 增加常量（可选）
3. **前端**
   - 业务页调用 `aiApi.invoke({ scenarioCode, input })`
   - 管理端无需改代码即可编辑配置；可在 `aiProviderModels.ts` 补充模型预设
4. **厂商**
   - 新 OpenAI 兼容厂商：插入 `ai_provider` 行即可，Factory 自动走 `OpenAiCompatibleAiLlmProvider`
   - 非兼容协议：实现 `IAiLlmProvider` 并在 `AiLlmProviderFactory` 分支注册

---

## 12. 已知限制与待办

| 项 | 说明 |
|----|------|
| 非流式 | 当前仅同步等待完整响应 |
| `extra_headers` | 表字段存在，Provider 未使用 |
| 全局配置 UI | `ai_global_config` 无管理页，需 SQL 修改 |
| 生产业务页 | 物料情报、实体解析建单已嵌入；其余场景待扩展 |
| 缓存失效 | 无按场景一键清缓存 UI，需 SQL 或等 TTL 过期 |
| 端点选择 | 不自动探测 `.cn` / `.ai`，需管理员配置 |

---

## 13. 变更历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-06-24 | 初始版本：架构、数据模型、调用链、Moonshot 配置、首个场景 |
| v1.1 | 2026-06-03 | 补充 entity.parse.* 实体解析建单与质量日志索引 |

---

## 14. 相关文档

- [系统架构与底层运行机制文档](./系统架构与底层运行机制文档.md)
- [AI 模块 PRD（产品）](../PRD/AI模块PRD.md)
- [AI 实体解析建单-设计与实现](./AI实体解析建单-设计与实现.md)
- [AI 物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)
- [RBAC权限系统PRD](../PRD/RBAC权限系统PRD.md)
- SQL 脚本：`scripts/ai_module_postgresql.sql`
