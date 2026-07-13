# AI 供应商情报调查（vendor.intel.lookup）设计与实现

**文档版本：** v1.0  
**更新日期：** 2026-07-14  
**项目名称：** FrontCRM_CSharp  
**状态：** Phase 1 已实现  
**关联文档：** [AI客户情报调查-设计与实现](./AI客户情报调查-设计与实现.md) · [AI模块架构与实现](./AI模块架构与实现.md) · [AI物料情报查询-设计与实现](./AI物料情报查询-设计与实现.md)

---

## 1. 文档目的与范围

本文档描述 **供应商方向** AI 情报调查的产品约定与技术实现。功能在架构上 **镜像客户情报**（`customer.intel.lookup`），在 Prompt 视角与权限授予上按 **采购与供应链** 业务做差异化。

**业务目标：**

- 帮助采购员在准入、询价、下单前了解供应商公开背景与风险
- 为采购风控、供应商评估提供可引用的章节化情报（第一期只读，不写回 CRM）

**读者：** 产品、前端、后端、运维。

**不在本文范围：** AI 平台通用编排（见 [AI模块架构与实现](./AI模块架构与实现.md)）；独立路由 `/vendors/intel` **不建**，能力嵌入供应商首页 `/vendor` 与列表/详情右栏。

---

## 2. 产品决策汇总（已拍板）

| 主题 | 结论 |
|------|------|
| 架构路线 | **独立场景** `vendor.intel.lookup` + **独立表** `vendor_intel_report`（镜像客户，非共用 `customer_intel_report`） |
| JSON 契约 | v1 **与客户共用 13 章**（`schema_version: 1.1`），章节 `id` 不变 |
| Prompt | **仅改视角**（采购/供应链、`ai_assessment` 偏合作建议），不删减销售向章节 |
| 权限码 | `biz.ai.vendor_intel.lookup` |
| 权限授予 | **不与** `vendor.read` 绑定；采购角色默认拥有 |
| 权限种子角色 | `SYS_ADMIN`、`biz_all`、`purchase_buyer`、`pur_manager`、`pur_staff`、`PURCHASER` |
| 职能开放 | **高管并集**：销售侧仅 `customer_intel`；采购侧 `vendor_intel`；`biz_all` 两侧都有 |
| 路由门禁 | `/vendor`、`VendorList`、`VendorDetail` 仍要求 `vendor.read`（仅有 AI 权限无列表权限者进不了模块，本期不改） |
| 跨主体缓存 | **双向复用**：同 `company_name + credit_code` 命中对侧报告时 **静默** 使用，**不弹**「已有客户情报」；`source=cache` 写入本侧表 |
| 跨主体只读 | `GetLatest*` 本表无记录时可 **只读** 对侧最新报告（不写表，直至用户发起调查） |
| 历史列表 | **隔离**：右栏历史仅列 `vendor_intel_report`，不合并客户侧记录 |
| 强制刷新 | 跳过跨表复用，走本场景 LLM；首页 **不提供** 强制刷新 |
| 首页缓存 | `/vendor` 默认 `forceRefresh: false` |
| 首页状态 | 页面内局部 state，**不写入** `vendorIntelLookup` store |
| 右栏切换行 | 进行中调查 **不取消**；右栏仅展示 **当前选中供应商** 状态（对齐需求明细「物料」页签） |
| 写回 CRM | **第一期不做** |
| 报告语言 | 简体中文 |
| AI 调用缓存 TTL | **90 天**（与客户一致） |
| Mock | 需要；system prompt 含「供应商情报」关键字即可命中 Mock JSON |

---

## 3. 产品概述

### 3.1 用户场景

采购人员在以下入口发起 **AI 供应商调查**：

- **供应商首页**（`/vendor`）：输入企业名称，点击「AI 调查」（未建档亦可）
- **供应商列表/详情右栏**：单击选中已建档供应商，在「调查」页签查看或刷新报告

**与 `entity.parse.vendor` 的区别：** 实体解析是「粘贴文本 → 预填新建供应商」；供应商情报是「联网调查 → 结构化只读报告」。

### 3.2 入口与权限

| 入口 | 路由 | 说明 |
|------|------|------|
| **供应商首页 AI 调查** | `/vendor`（`VendorHome`） | 药丸搜索条内「AI 调查」；对标 `CustomerHome` |
| 供应商列表右栏 | `/vendorlist`（`VendorList`） | 单击选中供应商 → 右栏「调查」页签 |
| 供应商详情右栏 | `/vendors/:id`（`VendorDetail`） | 自动绑定当前供应商 |

| 项 | 值 |
|----|-----|
| 场景码 | `vendor.intel.lookup` |
| 权限码 | `biz.ai.vendor_intel.lookup` |

### 3.3 交互要点

**供应商首页（`/vendor`）**

```
药丸搜索条
  [🔍 输入] [AI 调查] [搜索供应商] [进入列表查询]
  Enter → 有 AI 权限则 AI 调查；否则跳转供应商列表搜索

药丸下方（有 AI 权限时）
  三行左对齐输入引导（vendorHome.aiSearchHint）

AI 调查中
  加载文案 + 秒级计时

结果区
  CustomerIntelResultPanel（i18nKeyPrefix=vendorIntel，layout=centered，show-close）
  ├─ 无 CRM 对照条
  └─ 13 章卡片

关闭 / 再次调查
  forceRefresh=false（90 天缓存 + 跨主体缓存由后端处理）
```

**供应商列表 / 详情右栏**

```
供应商列表
  单击行 → 行高亮 + 右栏展示该供应商槽位状态
  双击行 → 进入供应商详情（不变）

右栏「调查」
  ├─ VendorIntelCrmContextBar（公司名、信用代码、采购员、黑名单/冻结）
  ├─ [发起调查] / [重新调查] + [强制刷新]
  ├─ [历史报告 ▼]（仅 vendor_intel_report，多条时）
  ├─ 加载中 / 错误 / 空态（仅当前 boundVendorId）
  └─ CustomerIntelResultPanel（i18nKeyPrefix=vendorIntel，embedded）
```

### 3.4 右栏切换行行为（对齐「物料」页签）

对标 `materialIntelLookup` + `RfqItemMaterialPanel`：

| 行为 | 说明 |
|------|------|
| 供应商 A 调查中 → 切换到 B | 右栏 **立即** 展示 B 的缓存/空态/可点「发起调查」；**不** 沿用 A 的转圈 |
| A 的调查请求 | **后台继续**，完成后写入 A 的 `slotByVendorId[A]` |
| 再切回 A | 若已完成则直接看报告；若仍在调查则显示 A 的 loading |
| `clearBound()` | 仅解绑 `boundContext`，**不清** 各供应商槽位缓存 |

实现：`vendorIntelLookup` 使用 `slotByVendorId`、`investigatingVendorIds`、`inFlightInvestigate`（按 `vendorId` 分槽）。

---

## 4. 总体架构

```
┌──────────────────────────────────────────────────────────────────┐
│  VendorHome（/vendor）— 页面内局部 state                         │
│  vendorIntelApi.investigate(forceRefresh=false)                   │
│  CustomerIntelResultPanel（vendorIntel i18n）                     │
└────────────────────────────┬─────────────────────────────────────┘
                             │ POST /api/v1/vendor-intel-reports/investigate
┌──────────────────────────────────────────────────────────────────┐
│  VendorList / VendorDetail + AppLayout 右栏                       │
│  VendorIntelPanel → vendorIntelLookup store（按 vendorId 分槽）   │
└────────────────────────────┬─────────────────────────────────────┘
                             │
┌────────────────────────────▼─────────────────────────────────────┐
│  VendorIntelReportService.InvestigateAsync                        │
│  1. forceRefresh=false → 查 customer_intel_report（同 fingerprint）│
│  2. 命中 → 写 vendor_intel_report（source=cache），不调 LLM        │
│  3. 未命中 → AiOrchestrator（vendor.intel.lookup）                │
└────────────────────────────┬─────────────────────────────────────┘
                             │
┌────────────────────────────▼─────────────────────────────────────┐
│  vendor_intel_report（业务存档）  ↔  customer_intel_report（对侧） │
│  query_fingerprint = SHA256(company_name + credit_code)           │
└──────────────────────────────────────────────────────────────────┘
```

**双层缓存：**

| 层 | 表 / 机制 | 说明 |
|----|-----------|------|
| AI 调用缓存 | `ai_invocation_cache` | key 含 `scenario_code`，客户/供应商 **互相隔离** |
| 跨表业务复用 | `customer_intel_report` ↔ `vendor_intel_report` | 同指纹静默复用 JSON（见 §6.3） |
| 业务报告存档 | `vendor_intel_report` | 全公司可见、历史时间线、关联 `vendor_id` |

---

## 5. 输出契约

与客户情报 **完全相同** 的 13 章结构（`schema_version: 1.1`）。章节 ID、排序、`relations.section_order` 见 [AI客户情报调查-设计与实现](./AI客户情报调查-设计与实现.md) §5.2–§5.4。

前端契约解析：**复用** `customerIntelSchema.ts`；`vendorIntelSchema.ts` 仅 re-export。

Prompt 差异（`vendor.intel.lookup` 模板）：

- system：面向 **采购与供应链** 人员
- `ai_assessment`：强调供应商资质、交付、合规与合作建议
- `relations.for_procurement_followup` 替代客户侧 `for_sales_followup`（JSON schema hint 中）

---

## 6. 后端实现

### 6.1 数据表 `vendor_intel_report`

| 列 | 说明 |
|----|------|
| `vendor_id` | 可空；首页未建档调查为 null |
| `company_name` / `credit_code` / `query_fingerprint` | 与客户相同归一化与 SHA256 指纹 |
| `scenario_code` | 固定 `vendor.intel.lookup` |
| `report_json` | jsonb，13 章对象 |
| `source` | `live` \| `cache` |
| `is_latest` | 同 fingerprint 仅一条为 true |

索引：`(vendor_id, created_at DESC)`、`(query_fingerprint, is_latest)`。

### 6.2 常量与场景

| 项 | 值 |
|----|-----|
| `AiScenarioCodes.VendorIntelLookup` | `vendor.intel.lookup` |
| `AiPermissionCodes.VendorIntelLookup` | `biz.ai.vendor_intel.lookup` |
| `cache_key_fields` | `company_name`, `credit_code` |
| `cache_ttl_seconds` | 7776000 |

### 6.3 跨主体缓存（双向）

**供应商 `InvestigateAsync`（`forceRefresh=false`）：**

1. 查 `customer_intel_report` 同 `query_fingerprint` 且 `is_latest`
2. 命中 → 写入 `vendor_intel_report`（`source=cache`），返回 `FromCache=true`
3. 未命中 → 调用 `AiOrchestrator`

**客户 `InvestigateAsync`（对称改造）：**

1. 未命中本表时查 `vendor_intel_report`
2. 同上写入 `customer_intel_report`

**`GetLatestByVendorId` / `GetLatestByQuery`：**

- 本表无记录时 **只读** 对侧报告填充 DTO（`FromCache=true`），**不** 自动 INSERT

**`forceRefresh=true`：** 跳过跨表复用。

共享工具：`IntelReportFingerprint`、`IntelReportPeerCache`（`CRM.Infrastructure/Services/`）。

### 6.4 API

| 方法 | 路径 | 权限 |
|------|------|------|
| POST | `/api/v1/vendor-intel-reports/investigate` | `biz.ai.vendor_intel.lookup` |
| GET | `/api/v1/vendor-intel-reports/latest?companyName=&creditCode=` | 同上 |
| GET | `/api/v1/vendor-intel-reports/{id}` | 同上 |
| GET | `/api/v1/vendors/{id}/intel-reports/latest` | 同上 |
| GET | `/api/v1/vendors/{id}/intel-reports` | 同上 |

### 6.5 核心代码路径

| 层级 | 路径 |
|------|------|
| 实体 | `CRM.Core/Models/Vendor/VendorIntelReport.cs` |
| 接口 | `CRM.Core/Interfaces/IVendorIntelReportService.cs` |
| 服务 | `CRM.Infrastructure/Services/VendorIntelReportService.cs` |
| 控制器 | `CRM.API/Controllers/VendorIntelReportsController.cs` |
| 实体端点 | `CRM.API/Controllers/VendorsController.cs`（intel-reports） |
| 编排守卫 | `CRM.Infrastructure/Ai/AiOrchestrator.cs`（`AppendVendorIntelLanguageGuard`） |
| Mock | `CRM.Infrastructure/Ai/MockAiLlmProvider.cs`（「供应商情报」/「客户情报」） |

---

## 7. 前端实现

### 7.1 文件清单

| 文件 | 职责 |
|------|------|
| `api/vendorIntel.ts` | investigate / latest / list / getById |
| `stores/vendorIntelLookup.ts` | 按 `vendorId` 分槽；`bound*` 计算属性供右栏 |
| `components/Vendor/VendorIntelPanel.vue` | 右栏页签根 |
| `components/Vendor/VendorIntelCrmContextBar.vue` | CRM 对照条（采购员） |
| `components/Customer/CustomerIntelResultPanel.vue` | 共用渲染；`i18nKeyPrefix="vendorIntel"` |
| `utils/vendorIntelSchema.ts` | re-export `customerIntelSchema` |
| `views/Vendor/VendorHome.vue` | 首页 AI 调查 |
| `views/Vendor/VendorList.vue` | 单击行 `bindContext` + 行高亮 |
| `views/Vendor/VendorDetail.vue` | 加载后 `bindContext` |
| `layouts/AppLayout.vue` | `VendorList`/`VendorDetail` 右栏页签 `r-vendor-intel` |

### 7.2 `vendorIntelLookup` store 设计

```
boundContext                    // 当前选中供应商 CRM 上下文
slotByVendorId[vendorId]        // { currentReport, historyReports, loadError }
investigatingVendorIds[]        // 正在 AI 调查的 vendorId 列表
loadingLatestVendorIds[]        // 正在拉取 latest 的 vendorId 列表
inFlightInvestigate Map         // 进行中的 Promise，切换行不取消

bindContext(ctx)                // 切换选中；sessionStorage 记住 vendorId
loadLatest(vendorId?)           // 写入对应槽位
investigate({ force })          // 快照 context，按 vendorId 落槽
clearBound()                    // 仅 boundContext=null（路由离开列表/详情）
```

**面板绑定字段：** `boundCurrentReport`、`boundHistoryReports`、`boundInvestigating`、`boundLoadingLatest`、`boundLoadError`。

### 7.3 AppLayout 右栏页签

| 路由 | 页签 |
|------|------|
| `VendorList`、`VendorDetail` | `调查`（`layout.auxTabs.vendorIntel`）+ `帮助` |
| 其他路由 | 清理 `vendorIntelLookup.clearBound()` |

与客户右栏 `r-customer-intel` 互斥；切换模块时双方 store 均 `clearBound()`。

### 7.4 i18n

| 命名空间 | 用途 |
|----------|------|
| `vendorIntel.*` | 右栏/结果面板文案 |
| `vendorHome.aiSearch*` | 首页药丸、引导、加载 |
| `layout.auxTabs.vendorIntel` | 右栏页签「调查」 |

### 7.5 CRM 对照条字段

| 字段 | 来源 |
|------|------|
| 公司名 | `OfficialName`（琥珀色 14px） |
| 信用代码 | `CreditCode` |
| 采购员 | `purchaseUserName` / `purchaserName` |
| 黑名单 / 冻结 | `BlackList` / `IsDisenable` |

---

## 8. 部署与运维

### 8.1 SQL 脚本（已有库增量）

```text
scripts/ai_vendor_intel_lookup_postgresql.sql
```

执行顺序：在 AI 模块基础表已存在、客户情报脚本已执行的前提下 **单独执行** 即可。

### 8.2 EF Migration

`20260731200000_VendorIntelReportAndScenario`

### 8.3 种子数据 ID（勿与名片解析冲突）

| 资源 | ID |
|------|-----|
| `ai_prompt_template` | `a2000001-0000-4000-8000-00000000000d` |
| `ai_scenario` | `a3000001-0000-4000-8000-00000000000d` |
| `sys_permission` | `30000000-0000-4000-8000-0000000000cd` |

> **勿用** `...000b`（已被 `entity.parse.customer_business_card` 占用）、`...0cb`（已被 `biz.ai.entity.parse.customer_business_card` 占用）。

### 8.4 部署后检查

```sql
SELECT code, id FROM ai_prompt_template WHERE code = 'vendor.intel.lookup';
SELECT code, prompt_template_id FROM ai_scenario WHERE code = 'vendor.intel.lookup';
SELECT "PermissionCode" FROM sys_permission WHERE "PermissionCode" = 'biz.ai.vendor_intel.lookup';
```

采购员账号需 **重新登录** 以加载新权限。

### 8.5 帮助文档

`help/pages/供应商_MENU_VENDOR_HOME.md`（含首页 AI 调查、右栏调查、权限说明）。

---

## 9. 与客户情报对照

| 维度 | 客户 | 供应商 |
|------|------|--------|
| 场景 | `customer.intel.lookup` | `vendor.intel.lookup` |
| 权限种子 | 有 `customer.read` 的角色 | 采购角色（见 §2） |
| 首页路由 | `/custome` | `/vendor` |
| 对照条 | 业务员 | 采购员 |
| 冻结字段 | `disenableStatus` | `isDisenable` |
| 跨表缓存 | 可读 vendor 表 | 可读 customer 表 |
| 右栏 store | `customerIntelLookup`（按 customerId 分槽） | `vendorIntelLookup`（按 vendorId 分槽） |

---

## 10. 明确不做（v1）

- 写回 CRM 供应商主数据
- 供应商首页历史报告下拉
- 右栏历史合并客户侧记录
- 独立全屏搜索页 `/vendors/intel`
- 调查完成通知
- 仅有 `biz.ai.vendor_intel.lookup`、无 `vendor.read` 时开放 `/vendor` 路由

---

## 11. 后续可选（Phase 2+）

- Prompt 强化采购章节权重（不改 JSON `id`）
- 「同主体已在客户侧调查」轻提示（产品当前要求静默，可改为可配置）
- 未建档调查后「关联到供应商」
- Debug 页 `/debug/vendor-intel`（对标 `DebugCustomerIntel`）
- 章节 UI 增强（风险徽章、时间线组件等，与客户同期）

---

## 12. 变更记录

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-07-14 | Phase 1 全文：镜像客户、双向缓存、分槽右栏、部署 ID 修正 |
