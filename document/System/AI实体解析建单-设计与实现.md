# AI 实体解析建单（entity.parse.*）设计与实现

**文档版本：** v1.0  
**更新日期：** 2026-06-03  
**项目名称：** FrontCRM_CSharp  
**关联文档：** [AI模块架构与实现](./AI模块架构与实现.md) · [AI模块PRD](../PRD/AI模块PRD.md)

---

## 1. 文档目的与范围

本文档描述 **粘贴非结构化文本 → AI 解析 → 用户确认 → 表单预填 → 保存建单** 的完整设计与实现，覆盖以下 **7 个业务节点**：

| # | 业务 | 场景码 | 实体类型 |
|---|------|--------|----------|
| 1 | 新建客户 | `entity.parse.customer` | `CUSTOMER` |
| 2 | 新建客户联系人 | `entity.parse.customer_contact` | `CUSTOMER_CONTACT` |
| 3 | 新建客户地址 | `entity.parse.customer_address` | `CUSTOMER_ADDRESS` |
| 4 | 新建供应商 | `entity.parse.vendor` | `VENDOR` |
| 5 | 新建供应商联系人 | `entity.parse.vendor_contact` | `VENDOR_CONTACT` |
| 6 | 新建供应商地址 | `entity.parse.vendor_address` | `VENDOR_ADDRESS` |
| 7 | 新建 RFQ | `entity.parse.rfq` | `RFQ` |

**读者：** 产品、前端、后端、运维。

**不在本文范围：** AI 平台通用能力（厂商、限流、管理页框架）详见 [AI模块架构与实现](./AI模块架构与实现.md)；物料情报 `material.intel.lookup` 见 [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)。

---

## 2. 产品概述

### 2.1 用户场景

业务员/采购从邮件、名片、聊天、送货单等复制一段文字，希望 **少填表、少出错** 地创建 CRM 主数据或 RFQ，而不是手工逐字段录入。

**统一交互：**

1. 在列表/首页/详情页点击 **「AI 创建」**（Split Button 下拉）
2. 在弹窗中 **粘贴文本**，点击「生成」
3. 在 **确认弹窗** 中核对/修改解析字段
4. 确认后跳转 **独立创建页**，表单已预填（`?aiPrefill=token`）
5. 用户补全必填项后 **保存**，完成正式建单

### 2.2 设计原则

| 原则 | 说明 |
|------|------|
| **场景驱动** | 每个业务节点对应独立 `entity.parse.*` 场景与 RBAC 权限 |
| **后端 normalize** | LLM 输出 snake_case JSON → 后端 `EntityParseNormalizer` 转为 camelCase KV，与前端契约一致；invoke 响应 `data` 即规范化结果 |
| **用户可改** | 确认弹窗允许编辑；`confirmed_fields_json` 记录用户最终确认值 |
| **质量可追溯** | `ai_entity_parse_log` 记录 raw / parsed / confirmed / saved 全链路 |
| **不替代保存** | AI 只负责预填；正式入库仍走既有 API（客户/供应商/RFQ/联系人/地址） |
| **子实体带父级** | 联系人/地址从详情页发起时，`bizId` 写入日志 `parent_biz_id` |

### 2.3 入口与权限

| 业务 | UI 入口 | 目标路由 | AI 权限 |
|------|---------|----------|---------|
| 客户 | `CustomerHome` / `CustomerList` → AI 创建 | `/customers/create` | `biz.ai.entity.parse.customer` |
| 客户联系人 | `CustomerDetail` 联系人 Tab → AI 创建 | `/customers/:id/contacts/create` | `biz.ai.entity.parse.customer_contact` |
| 客户地址 | `CustomerDetail` 地址 Tab → AI 创建 | `/customers/:id/addresses/create` | `biz.ai.entity.parse.customer_address` |
| 供应商 | `VendorHome` / `VendorList` → AI 创建 | `/vendors/create` | `biz.ai.entity.parse.vendor` |
| 供应商联系人 | `VendorDetail` 联系人 Tab → AI 创建 | `/vendors/:id/contacts/create` | `biz.ai.entity.parse.vendor_contact` |
| 供应商地址 | `VendorDetail` 地址 Tab → AI 创建 | `/vendors/:id/addresses/create` | `biz.ai.entity.parse.vendor_address` |
| RFQ | `RFQHome` / `RFQList` → AI 创建 | `/rfqs/create` | `biz.ai.entity.parse.rfq` |

各场景另需对应 **业务写权限**（如 `customer.write`、`rfq.create`）。无 AI 权限时 Split Button 的 AI 项不可用。

---

## 3. 总体架构

```
┌──────────────────────────────────────────────────────────────────────────┐
│  业务页（Home/List/Detail）                                                │
│  AiEntityCreateHost（entityType + targetRoute + parentBizId?）             │
└────────────────────────────┬─────────────────────────────────────────────┘
                             │ ① 粘贴 raw_text
┌────────────────────────────▼─────────────────────────────────────────────┐
│  AiTextParseDialog → POST /api/v1/ai/invoke (scenarioCode, input, biz*)   │
│  AiOrchestrator → LLM → EntityParseNormalizer → ai_entity_parse_log       │
│  响应：data=normalize KV, entityParseLogId                                  │
└────────────────────────────┬─────────────────────────────────────────────┘
                             │ ② 确认弹窗（可编辑）
┌────────────────────────────▼─────────────────────────────────────────────┐
│  AiEntityParseConfirmDialog → POST .../entity-parse-logs/{id}/confirm       │
│  setAiPrefill(entityType, formPayload, parseLogId) → sessionStorage       │
│  router.push(targetRoute, { query: { aiPrefill: token } })                  │
└────────────────────────────┬─────────────────────────────────────────────┘
                             │ ③ 创建页 consume 一次
┌────────────────────────────▼─────────────────────────────────────────────┐
│  CustomerEdit / VendorEdit / RFQCreate / *ContactEdit / *AddressEdit       │
│  用户保存 → 业务 API create → markEntityParseSaved(logId, savedBizId)       │
└────────────────────────────────────────────────────────────────────────────┘
```

**与通用 AI 模块关系：** 仍走 `AiOrchestrator.InvokeAsync`；`entity.parse.*` 成功后额外写 `ai_entity_parse_log` 并替换返回 `Data` 为规范化对象。

---

## 4. 前端设计

### 4.1 共享组件（`CRM.Web/src/components/AiCreate/`）

| 组件 | 职责 |
|------|------|
| `AiEntityCreateHost.vue` | 编排入口：选场景、`invoke`、打开确认弹窗、写 prefill、跳转 |
| `AiTextParseDialog.vue` | 粘贴区 +「生成」 |
| `AiEntityParseConfirmDialog.vue` | 按 `entityType` 渲染不同字段表单；客户/RFQ 含模糊匹配提示 |

**Host 关键 props：**

- `entityType`：`CUSTOMER` | `RFQ` | `VENDOR` | `CUSTOMER_CONTACT` | `VENDOR_CONTACT` | `CUSTOMER_ADDRESS` | `VENDOR_ADDRESS`
- `targetRoute`：确认后跳转的路由（含详情页 `:id`）
- `parentBizId`（可选）：详情页子实体时传客户/供应商 id，作为 invoke 的 `bizId`

### 4.2 预填机制（`utils/aiPrefill.ts`）

- `setAiPrefill(entityType, payload, parseLogId?)` → 生成 token，写入 `sessionStorage`（TTL 30 分钟）
- `consumeAiPrefill(entityType, token)` → 返回 `{ payload, parseLogId }` 并 **删除** 存储（一次性）
- 创建页通过 `?aiPrefill=token` 消费；消费后从 URL 移除 query，避免刷新重复应用

### 4.3 字段契约与映射（`utils/entityParseSchema.ts`）

| 函数 | 说明 |
|------|------|
| `normalize*ParseResult` | 前端 fallback normalize（无 `entityParseLogId` 时） |
| `*PrefillToFormPayload` | 解析模型 → 各创建页 `formData` 片段 |

**地址类特殊规则：**

- 港澳台与国内均走「中国 + 省市区级联」（`usesChinaRegionCascader`）
- 省市区补全依赖 `constants/region.ts` + `data/regions.ts`
- 客户地址类型：`Office` / `Billing` / `Shipping` / `Registered`
- 供应商地址类型：`1` 收货 / `2` 账单

### 4.4 各创建页集成

| 页面 | 预填消费 | 保存后回写 saved |
|------|----------|------------------|
| `CustomerEdit.vue`（create） | `applyAiPrefillFromRoute` | `markEntityParseSaved` + 新客户 id |
| `VendorEdit.vue`（create） | 同上 | 同上 |
| `RFQCreate.vue` | watch `aiPrefill` query | create RFQ 返回 id |
| `CustomerContactEdit.vue` | 同上 | 新建联系人 id |
| `VendorContactEdit.vue` | 同上 | 同上 |
| `CustomerAddressEdit.vue` | 同上 | 新建地址 id |
| `VendorAddressEdit.vue` | 同上 | 同上 |

工具函数：`utils/entityParseLogTrack.ts` → `markEntityParseSaved(parseLogId, savedBizId)`（失败不阻断业务）。

### 4.5 前端 API（`api/ai.ts`）

| 方法 | 路径 |
|------|------|
| `aiApi.invoke` | `POST /api/v1/ai/invoke` |
| `aiApi.confirmEntityParseLog` | `POST /api/v1/ai/entity-parse-logs/{id}/confirm` |
| `aiApi.markEntityParseSaved` | `POST /api/v1/ai/entity-parse-logs/{id}/saved` |
| 管理端 list/detail/export/purge | `GET/POST /api/v1/ai/mgmt/entity-parse-logs/*` |

常量：`AI_SCENARIO_ENTITY_PARSE_*`、`AI_PERMISSION_ENTITY_PARSE_*`。

---

## 5. 后端设计

### 5.1 场景配置共性

所有 `entity.parse.*` 场景在种子 SQL 中配置：

| 字段 | 典型值 | 说明 |
|------|--------|------|
| `allowed_input_fields` | `["raw_text"]` | 仅允许粘贴全文 |
| `cache_ttl_seconds` | `0` | **不缓存**（每次粘贴内容不同，且需完整日志） |
| `output_format` | `json` | LLM 输出 JSON 对象 |
| `enable_web_search` | `false` | 不联网 |
| `permission_code` | `biz.ai.entity.parse.*` | 见 §2.3 |

Prompt 模板：`scripts/ai_entity_parse_postgresql.sql`（DBeaver-safe：`raw_text` 占位符用 hex/CHR 拼接，避免 SQL 中写 `{{`）。

Mock 分支：`MockAiLlmProvider.cs` 按场景返回固定 JSON，便于本地无 Key 联调。

### 5.2 规范化（`EntityParseNormalizer.cs`）

- 输入：LLM JSON（snake_case）
- 输出：camelCase 对象，形状与 `entityParseSchema.ts` 中 `Parsed*Fields` 一致
- 省市区：`EntityParseRegionHelper.cs` + 嵌入资源 `china_regions.json`（与前端 `regionData` 同源导出）

Orchestrator 在 invoke 成功路径调用 `IAiEntityParseLogService.TryCreateParsedLogAsync`，并设置：

- `AiInvokeResultDto.EntityParseLogId`
- `AiInvokeResultDto.Data` = 规范化 KV

### 5.3 解析质量日志

**表：** `ai_entity_parse_log`（见 `scripts/ai_entity_parse_log_postgresql.sql`）

| 字段 | 说明 |
|------|------|
| `invocation_id` | 关联 `ai_invocation_log.id` |
| `raw_text` | 用户粘贴原文 |
| `parse_result_raw` | LLM 原始文本 |
| `parse_result_json` | 后端 normalize 后 KV（jsonb） |
| `confirmed_fields_json` | 用户确认弹窗最终 KV |
| `outcome` | `parsed` → `confirmed` → `saved`（或 `failed` 预留） |
| `parent_biz_type` / `parent_biz_id` | 子实体场景（`CUSTOMER`/`VENDOR` + 父 id） |
| `saved_biz_id` / `saved_at` | P1：保存成功后回写（见 `ai_entity_parse_log_p1_postgresql.sql`） |

**服务：** `AiEntityParseLogService.cs`

| 方法 | 触发时机 |
|------|----------|
| `TryCreateParsedLogAsync` | invoke 成功且场景为 `entity.parse.*` |
| `ConfirmAsync` | 前端确认弹窗提交 |
| `MarkSavedAsync` | 创建页保存成功 |
| `ListForAdminAsync` / `GetDetailForAdminAsync` / `ExportCsvAsync` / `PurgeOlderThanAsync` | 管理端 |

### 5.4 API 摘要

**业务用户（需对应 entity.parse 权限 + 登录）：**

```http
POST /api/v1/ai/invoke
{
  "scenarioCode": "entity.parse.customer",
  "input": { "raw_text": "..." },
  "bizType": "CUSTOMER",
  "bizId": null
}

POST /api/v1/ai/entity-parse-logs/{id}/confirm
{ "confirmedFields": { ... } }

POST /api/v1/ai/entity-parse-logs/{id}/saved
{ "savedBizId": "..." }
```

**管理员（`biz.ai.admin`）：**

- 列表：`GET /api/v1/ai/mgmt/entity-parse-logs?scenarioCode=&outcome=&entityType=`
- 详情：`GET /api/v1/ai/mgmt/entity-parse-logs/{id}`
- 导出：`GET /api/v1/ai/mgmt/entity-parse-logs/export`
- 清理：`POST /api/v1/ai/mgmt/entity-parse-logs/purge?keepDays=180`

**管理 UI：** [AI 配置](/system/ai-config) → Tab **「实体解析日志」**（非独立菜单）。

---

## 6. 七场景差异要点

### 6.1 客户（`entity.parse.customer`）

- **必填校验（前端）：** `customerName` 非空
- **输出：** 主档字段（名称、级别、地址、税号、账期等）；**不含** contacts 数组
- **创建页：** `CustomerEdit.vue` create 模式；预填后 `contacts: []`
- **增强：** 确认弹窗可展示 **相似客户** 提示（`useCustomerFuzzyMatch.ts`），不阻断新建

### 6.2 客户联系人（`entity.parse.customer_contact`）

- **必填：** `contactName`
- **父级：** `CustomerDetail` 传入 `parentBizId=customerId` → 日志 `parent_biz_id`
- **创建页：** `CustomerContactEdit.vue`；保存后可继续上传名片

### 6.3 客户地址（`entity.parse.customer_address`）

- **必填：** `streetAddress`（详细地址）
- **国内：** 国家归一「中国」+ 省市区级联；海外自由文本
- **创建页：** 独立全页 `CustomerAddressEdit.vue`（非 Dialog）

### 6.4 供应商（`entity.parse.vendor`）

- **必填：** `officialName`
- **字段映射：** `nickName`、`level`、`credit`、`paymentDays`、`taxNumber` 等
- **创建页：** `VendorEdit.vue` create；预填 `contacts: []`

### 6.5 供应商联系人（`entity.parse.vendor_contact`）

- **必填：** `cName` 或 `eName` 至少其一
- **字段：** `cName`/`eName`/`title`/`mobile`/`isMain` 等

### 6.6 供应商地址（`entity.parse.vendor_address`）

- **必填：** `address`（详细地址）
- **类型：** `addressType` 1=收货 / 2=账单
- **级联规则：** 与客户地址共用 `customerAddress` / `vendorAddress` 常量

### 6.7 RFQ（`entity.parse.rfq`）

- **明细：** `items[]` 数组（多行物料）；兼容 legacy 单对象 `item`
- **币别：** `price_currency` 1–4（RMB/USD/EUR/HKD）
- **创建页：** `RFQCreate.vue`；支持草稿 `draftId` 与 `aiPrefill` 互斥优先逻辑
- **客户匹配：** 确认弹窗可提示已有客户，创建页仍可手工选客户
- **Excel 导入（扩展）：** 另见 [RFQ Excel导入-设计与实现](./RFQ Excel导入-设计与实现.md) — 场景 `entity.parse.rfq_excel_column_map` / `entity.parse.rfq_excel_brand_map`，行数据前端解析，不写 `ai_entity_parse_log`
- **品牌学习映射：** 见 [智能学习品牌匹配-设计与实现](./智能学习品牌匹配-设计与实现.md) — `biz_brand_learned_mapping` 全公司共享，Excel/RFQCreate 路径读写

---

## 7. 数据库与部署

### 7.1 脚本执行顺序（增量环境）

1. `scripts/ai_module_postgresql.sql`（若未部署 AI 模块）
2. `scripts/ai_entity_parse_postgresql.sql` — 7 场景模板、场景、权限
3. `scripts/ai_entity_parse_log_postgresql.sql` — 质量日志表（P0）
4. `scripts/ai_entity_parse_log_p1_postgresql.sql` — `saved_*` 字段（P1）
5. （可选）`scripts/customer_bank_extend_postgresql.sql` — 客户详情银行字段（与 AI 无直接关系，但客户详情联调依赖）

或使用 EF 迁移：

- `20260806120000_AiEntityParseLog.cs`
- `20260806130000_AiEntityParseLogSavedFields.cs`

### 7.2 日志保留

- 配置键：`ai_global_config.entity_parse_log_retention_days`（默认 180）
- 脚本：`scripts/ai_entity_parse_log_retention_postgresql.sql`
- 或管理端「清理过期」按钮

### 7.3 清库脚本

`scripts/clear_all_business_data_keep_system_params_postgresql.sql` 已包含 `ai_entity_parse_log`（在 `ai_invocation_log` 之前 TRUNCATE）。

---

## 8. 关键文件索引

| 层级 | 路径 |
|------|------|
| SQL 场景 | `scripts/ai_entity_parse_postgresql.sql` |
| SQL 日志 | `scripts/ai_entity_parse_log_postgresql.sql`、`scripts/ai_entity_parse_log_p1_postgresql.sql` |
| Core 模型 | `CRM.Core/Models/Ai/AiEntityParseLog.cs` |
| Core 常量 | `CRM.Core/Constants/AiCodes.cs`（`AiEntityParseScenarioCodes`、`AiEntityParseOutcomeCode`） |
| 接口/DTO | `CRM.Core/Interfaces/IAiServices.cs` |
| 编排 | `CRM.Infrastructure/Ai/AiOrchestrator.cs`（`EnrichEntityParseResultAsync`） |
| 日志服务 | `CRM.Infrastructure/Ai/AiEntityParseLogService.cs` |
| Normalize | `CRM.Infrastructure/Ai/EntityParse/EntityParseNormalizer.cs` |
| 区划 | `CRM.Infrastructure/Ai/EntityParse/EntityParseRegionHelper.cs`、`china_regions.json` |
| Mock | `CRM.Infrastructure/Ai/MockAiLlmProvider.cs` |
| API | `CRM.API/Controllers/AiController.cs`、`AiAdminController.cs` |
| 共享 UI | `CRM.Web/src/components/AiCreate/*` |
| 契约 | `CRM.Web/src/utils/entityParseSchema.ts`、`aiPrefill.ts`、`entityParseLogTrack.ts` |
| 管理页 | `CRM.Web/src/views/System/AiConfigPage.vue`（Tab：实体解析日志） |
| i18n | `aiEntityCreate.*`（`zh-CN.ts` / `en-US.ts`） |

---

## 9. 已知限制与后续

| 项 | 说明 |
|----|------|
| 无流式 | 长文本需等待完整 LLM 响应 |
| 缓存关闭 | `cache_ttl_seconds=0`，相同文本重复调用会重复计费 |
| normalize 双实现 | 后端为主；前端 retain fallback normalize 供兼容 |
| RFQ 客户绑定 | AI 仅解析 `customerName` 文本，不自动写 `customerId` |
| 供应商地址 remark | 表单有 remark 字段，当前 create API 未持久化（与手工 Dialog 一致） |
| 质量分析 | 管理端已支持列表/导出；离线分析可基于 CSV + `parse_result_json` vs `confirmed_fields_json` diff |

---

## 10. 变更历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-06-03 | 7 场景建单、质量日志 P0/P1、管理端 Tab、后端 normalize |

---

## 11. 相关文档

- [AI模块架构与实现](./AI模块架构与实现.md)
- [AI模块PRD](../PRD/AI模块PRD.md)
- [PostgreSQL增量脚本编写规范](../PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md)
- [省市地区级联选择控件规范](../PRD/规范/UI规范/省市地区级联选择控件规范.md)
