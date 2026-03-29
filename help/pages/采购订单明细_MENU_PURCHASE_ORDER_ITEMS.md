[帮助文档目录](../帮助文档目录.md)

# 采购订单明细

## 页面功能

- 采购单行：状态含新建/待审核/已确认/已付款/已入库等；通知到货与请款。

## 操作说明

<div class="help-op-block">

**通知到货**

**说明：** 打开新建到货通知弹窗。

**前置条件：** 行状态为**「已确认」**（与原 `itemStatus=已确认(30)` 一致）；账号具备 `purchase-order.read`。

</div>

<div class="help-op-block">

**申请付款**

**说明：** 打开请款弹窗。

**前置条件：** 行上 **`canApplyPayment=true`**（与列表/接口字段一致）。

</div>

### 列权限

| 列 | 权限 |
|----|------|
| 供应商 | `vendor.info.read` |
| 采购员 | `purchase.user.read` 或 `purchase-order.read` |
| 金额 | `purchase.amount.read` |
