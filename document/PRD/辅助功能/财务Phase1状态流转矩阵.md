# 财务 Phase 1 状态流转矩阵

## 1. 付款单（FinancePayment）

- 主状态：`1新建 -> 2待审核 -> 10审核通过 -> 100付款完成`
- 负向状态：`-1审核失败`，`-2取消`

允许流转：

- `1 -> 2`（提交审核）
- `-1 -> 1`（驳回后重提）
- `2 -> 10`（审核通过）
- `2 -> -1`（审核驳回）
- `10 -> 100`（付款完成）
- `1/2 -> -2`（取消）

核销规则：

- 明细核销金额必须 `> 0`
- 单次核销不得超过 `VerificationToBe`
- 核销后自动回写：
  - `purchaseorderitem.finance_payment_status`（0/1/2）
  - `purchaseorder.finance_status`（0/1/2）

---

## 2. 收款单（FinanceReceipt）

- 主状态：`0草稿 -> 1待审核 -> 2已审核 -> 3已收款`
- 负向状态：`4已取消`

允许流转：

- `0 -> 1`（提交审核）
- `0 -> 4`（草稿取消）
- `1 -> 2`（审核通过）
- `1 -> 4`（审核阶段取消）
- `2 -> 3`（确认收款）
- `2 -> 4`（取消）

核销规则：

- 明细核销金额必须 `> 0`
- 单次核销不得超过 `ReceiptConvertAmount`
- 核销后自动回写：
  - `financesellinvoice.receive_status`（0/1/2）
  - `sellinvoiceitem.receive_status`（随主单）
  - `sellorder.finance_receipt_status`（0/1/2）

---

## 3. 进项发票（FinancePurchaseInvoice）

- 认证状态：`ConfirmStatus 0未认证 / 1已认证`
- 冲红状态：`RedInvoiceStatus 0正常 / 1已冲红`

关键规则：

- 已冲红不可认证
- 已认证不可重复认证
- 发票金额必须大于 0 才能认证
- 已冲红不可取消认证
- 已认证不可直接冲红（需走财务冲销流程）

动作端点：

- `POST /finance/purchase-invoices/{id}/confirm`
- `POST /finance/purchase-invoices/{id}/unconfirm`
- `POST /finance/purchase-invoices/{id}/red-invoice`

---

## 4. 销项发票（FinanceSellInvoice）

- 发票状态：`1未申请 -> 2申请中 -> 100已开票`
- 异常状态：`101开票失败`，`-1已作废`

允许流转：

- `1 -> 2`（提交开票申请）
- `2 -> 100`（开票成功）
- `2 -> 101`（开票失败）
- `101 -> 2`（失败后重提）

作废规则：

- 已作废不可重复作废
- 已有收款核销（`ReceiveDone > 0`）禁止作废

动作端点：

- `POST /finance/sell-invoices/{id}/submit-application`
- `POST /finance/sell-invoices/{id}/mark-issued`
- `POST /finance/sell-invoices/{id}/mark-issue-failed`
- `POST /finance/sell-invoices/{id}/void`

---

## 5. 前端动作化调用（Phase 1）

已改为优先调用动作端点，不再直接随意写状态值：

- 付款：submit/approve/reject/complete/cancel
- 收款：submit/approve/confirm-received/cancel
- 进项：confirm/unconfirm/red-invoice
- 销项：submit-application/mark-issued/mark-issue-failed/void

