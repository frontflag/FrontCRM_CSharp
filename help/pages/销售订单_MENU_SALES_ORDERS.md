[帮助文档目录](../帮助文档目录.md)

<h1 class="help-h1--offset-down">销售订单</h1>

## 页面功能

- 销售订单列表、筛选与统计（金额列受 `sales.amount.read` 控制）。

## 操作说明

<div class="help-op-block">

**提交审核**

**说明：** 主订单状态由「新建」流转为「待审核」。

**前置条件：** 状态为「新建」且账号具备 `sales-order.write` 权限。

</div>

<div class="help-op-block">

**订单明细 · 申请出库**

**说明：** 在「订单明细」页签中针对单行发起出库通知。

**前置条件：** **销售订单主表**须已为「审核通过」「进行中」或「完成」状态；另需具备 `sales-order.write`。

</div>
