# 供应商中英文名称只读展示规范 PRD

## 1. 文档目的

在**详情页、弹窗、描述项、列表单元格**等**只读**场景中，统一供应商名称的展示格式：在具备数据时同时呈现**中文全称**与**英文全称**，避免仅显示中文导致跨境、财务、物流协作时信息不完整。

本规范与以下文档分工明确、可交叉引用：

| 文档 | 分工 |
|------|------|
| [客户供应商名称状态展示规范 PRD](./客户供应商名称状态展示规范PRD.md) | 冻结 / 黑名单时的**灰色 + 图标** |
| [列表扩展列规范 PRD](./列表扩展列规范PRD.md) | 列表「供应商」**扩展列**（收起/展开：中文 \| 英文 \| 编号） |
| [供应商中英文名称只读展示-分期实施记录](../../实现方案/供应商中英文名称只读展示-分期实施记录.md) | 第一期 / 第二期落地范围与改动清单 |

---

## 2. 产品决策（方案 A+）

### 2.1 展示格式（语义层，统一）

只读场景下，供应商名称的**逻辑格式**统一为：

```text
中文全称 / 英文全称
```

| 数据情况 | 展示 |
|----------|------|
| 中英文均有 | `香港亿生国际有限公司 / HONG KONG YISHENG INTERNATIONAL CO., LTD` |
| 仅中文 | 只显示中文 |
| 仅英文 | 只显示英文 |
| 均无 / 脱敏 | `—` |

- 分隔符默认：` / `（空格 + 斜杠 + 空格）。
- **不**在只读区拆成两个表单项标签（如「供应商（中文）」「供应商（英文）」），保持**一个「供应商」字段**的产品语义。

### 2.2 展示组件（UI 层，按场景）

| 场景 | 组件 / 模式 | 行为 |
|------|-------------|------|
| **弹窗、窄表单** | `VendorNameReadonlyField` · `mode="compact"` | 字段**独占一行**（`span=24`）；`readonly textarea` **1～2 行**自动增高；hover **tooltip** 全文 |
| **详情 descriptions、列表单元格** | `VendorNameReadonlyText` 或 `formatVendorNameReadonly()` | 纯文本 + tooltip；允许 `word-break` 换行 |
| **详情宽表单（单行即可）** | `VendorNameReadonlyField` · `mode="inline"` | 单行 readonly input + tooltip |

**弹窗布局约定（收款/申请付款等）：**

```text
[ 供应商编号 span12 ] [ 货代单号 / 采购员 span12 ]
[ 供应商名称 span24 — 中文 / 英文，可换行 ]
```

避免供应商名称与短字段共用半宽导致长英文名截断。

### 2.3 与列表扩展列的关系

- **列表扩展列**（`useVendorExtendColumn`）：继续用于需要**分列查看**编号/中/英的业务列表；收起态默认中文，展开态三列并排。
- **只读详情 / 弹窗**：使用本规范的 **A+ 单行合并格式**，不要求与扩展列 UI 完全一致，但**数据来源**一致（`vendorName` + `vendorEnglishName`）。

---

## 3. 数据来源

### 3.1 字段对照

| 字段 | 存储 | 说明 |
|------|------|------|
| `vendorName` | 业务单据冗余或运行时填充 | 中文全称（`vendorinfo.OfficialName` 或创建时快照） |
| `vendorEnglishName` | **`[NotMapped]` 运行时 enrich** | 来自 `vendorinfo.EnglishOfficialName`，**不落库** |

英文随主数据更新；历史单据若需「创建时点英文快照」，属后续产品决策（需 schema 变更），**当前未做**。

### 3.2 后端 enrich 范围（已实现）

以下接口在返回前填充 `VendorEnglishName`（或 DTO 等价字段）：

| 模块 | 说明 |
|------|------|
| 付款单 | `FinancePaymentService.EnrichVendorCodesAsync` |
| 采购订单详情 | `PurchaseOrdersController.MaskPurchaseOrder` |
| 采购订单明细行列表 | `GET /api/v1/purchase-orders/items` |
| 进项发票 | `FinancePurchaseInvoiceService` |
| 到货通知列表 | `ArrivalNoticeListQuery` |
| 质检列表 | `LogisticsService.GetQcsPagedAsync` / `GetQcsAsync` |
| 入库 / 库存中心 | `StockInService`、库存列表查询 |
| 批次对账 | `BatchReconciliationListQuery`（join `vendorinfo`） |

公共工具：`CRM.Core/Utilities/VendorDisplayEnrichment.cs`。

### 3.3 采购脱敏（§5.2.1）

当 `maskPurchaseSensitiveFields === true` 时：

- 中英文统一显示 **`—`**；
- 后端 `PurchaseSensitiveFieldMask511` 清空 `VendorName`、`VendorEnglishName` 等供应商身份字段。

---

## 4. 前端实现约定

### 4.1 工具函数

路径：`CRM.Web/src/utils/vendorDisplayName.ts`

```ts
formatVendorNameReadonly(zh?, en?, { separator?, empty?, masked? })
formatVendorNameReadonlyFromRow({ vendorName, vendorEnglishName }, options?)
```

**所有只读供应商名称**应经上述函数或封装组件输出，禁止各页面自行拼接 `中文 + ' / ' + 英文`。

### 4.2 组件

| 组件 | 路径 | 用途 |
|------|------|------|
| `VendorNameReadonlyField` | `CRM.Web/src/components/Vendor/VendorNameReadonlyField.vue` | 表单 / 弹窗只读输入形态 |
| `VendorNameReadonlyText` | `CRM.Web/src/components/Vendor/VendorNameReadonlyText.vue` | 详情、列表 slot 纯文本 |

Props：`nameZh`、`nameEn`、`masked`；Field 另支持 `mode: 'compact' | 'inline'`。

### 4.3 可编辑场景（不在本规范范围）

- 供应商**下拉选择**、新建/编辑表单：仍可用中文 label 或 option 内 `中文 / 英文`，与只读展示分开处理。
- 客户侧英文展示：可参考收款单 `customerEnglishName` 与 `useCustomerExtendColumn`，后续可对齐同一 formatter 模式。

---

## 5. 验收要点

1. 付款单编辑弹窗：供应商独占一行，有中英文时显示 `中文 / 英文`。
2. 付款单详情、进项发票详情：供应商描述项为合并格式。
3. 采购单详情、申请付款弹窗：同上。
4. QC 新建/编辑、到货通知详情：供应商独占一行（QC 页）或描述项合并展示。
5. 列表单列 `vendorName` 模板：使用 `VendorNameReadonlyText` 或 formatter（扩展列接入页除外）。
6. 脱敏账号：供应商名称均为 `—`。
7. 供应商未维护英文：仅显示中文，不显示空分隔符。

---

## 6. 版本

| 版本 | 日期 | 说明 |
|------|------|------|
| V1 | 2026-06-03 | 方案 A+ 定稿；第一期（财务付款）+ 第二期（采购/物流/库存/审批等）落地，见分期实施记录 |
