# 客户首页 KPI — 设计与实现

**状态：** 已上线（应收台账 + 客单在库）  
**页面：** `/custome`（`CustomerHome`）  
**API：** `GET /api/v1/customers/statistics`  
**列表联动：** preset `has_receivable` / `pending_shipment`（码名保留，口径与下表一致）  
**左栏说明：** [客户列表-左栏检索-设计与实现](./客户列表-左栏检索-设计与实现.md) §2.6

---

## 1. 业务口径

| 卡片（展示名） | 金额 | 客户数 |
|----------------|------|--------|
| 应收货款 / 应收客户 | 权限范围内客户的应收台账 `VerifiedToBe`，经 `sell_order_item_id` 挂 SO 行按 **`convert_price/price`（`FromExtend`）** 折合 USD 后求和（与应收款看板一致；价比不可用时回落查询日财务汇率） | 有 `VerifiedToBe > 0` 台账的去重客户数 |
| 在库金额 / 在库客户 | 客单在库行 `QtyRepertory × SalesPriceUsd`（USD，空价按 0）求和 | 能挂到客户的去重客户数 |

### 1.1 应收

- 表：`finance_receivable`（EF 全局软删过滤）
- 条件：`VerifiedToBe > 0`
- 折算：与财务应收看板同一套 `FromExtend`（SO 行 `convert_price`）；价比不可用时回落 `ExchangeRateToUsdConverter` + 查询日财务参数汇率
- 数据范围：仅统计当前用户数据权限内的客户

### 1.2 在库（客单）

- 表：`stock_item`（EF 全局软删过滤）
- 条件：
  - `StockType = 1`（客单；**不含**备货=2、样品=3）
  - `QtyRepertory > 0`
  - 能挂到客户：行上 `CustomerId` 有值，**或**经 `SellOrderItemId` → 销售订单 `CustomerId`
  - **无销售明细但 `CustomerId` 有值：计入**
- 金额：`QtyRepertory × (SalesPriceUsd ?? 0)`
- 数据范围：同上，按解析出的客户 ID 过滤

### 1.3 与旧实现差异

| 项 | 旧 | 新 |
|----|----|----|
| 应收 | 销售订单头 `FinanceReceiptStatus < 2` 的 `ConvertTotal` | 应收台账 `VerifiedToBe`→USD |
| 「待出」 | 订单头 `StockOutStatus < 2` 的 `ConvertTotal`（且头字段常未维护） | 客单在库金额 |
| 文案 | 待出库 / 待出货款 / 待发货 | 在库 / 在库金额 / 有在库的客户 |

---

## 2. 实现要点

| 位置 | 说明 |
|------|------|
| `CustomerHomeKpiQuery` | 应收 / 在库金额与客户数 |
| `CustomersController.GetCustomerStatistics` | 调用 KPI；响应字段名仍为 `receivableGoodsAmount` / `pendingOutboundAmount` 等（兼容前端） |
| `CustomerListQueryQuickFilter` | `has_receivable` / `pending_shipment` 与首页同口径 |
| `CustomerHome.vue` | 应收 / 在库卡片点击 → 列表 `?preset=…&quickFilter=…` |

`mask521` 仍将金额类字段置 0（客户数保留）。

---

## 3. 测试对照

见 [客户首页KPI-测试对照说明](../../QA/客户/客户首页KPI-测试对照说明.md)。

---

## 4. 修订记录

| 日期 | 说明 |
|------|------|
| 2026-08-11 | 应收折 USD 改为挂 SO 行 `convert_price/price`（与应收款看板一致），不再以查询日财务汇率为主 |
