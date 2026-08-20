# 财务 Phase 1 状态流转矩阵

## 1. 付款单（FinancePayment）

- 主状态：`1新建 -> 2待审核 -> 10审核通过 -> 100付款完成`
- 负向状态：`-1审核失败`，`-2取消`
- **详细操作权限、编辑请款字段边界、API/前端索引**见 [付款单状态与操作权限](../../System/财务/付款单状态与操作权限.md)

允许流转：

- `1 -> 2`（提交审核）
- `-1 -> 1`（驳回后编辑保存，或 `UpdateRequestAsync` 自动转换）
- `2 -> 10`（审核通过）
- `2 -> -1`（审核驳回）
- `10 -> 100`（付款完成）
- `10 -> 1`（**撤回**：清空付款执行字段与水单附件，非 `UpdateStatus` 通用流转）
- `1/2 -> -2`（取消）

### 1.1 状态 × 用户操作（Phase 1+）

| 状态 | 编辑请款 | 提交审核 | 撤回 | 付款执行/完成 | 取消 |
|------|:--------:|:--------:|:----:|:-------------:|:----:|
| 1 | ✅ | ✅ | — | — | ✅ |
| -1 | ✅ | —* | — | — | ✅ |
| 2 | ❌ | — | — | — | ✅ |
| 10 | ❌ | — | ✅ | ✅ | ❌ |
| 100 / -2 | ❌ | — | — | — | ❌ |

\* 驳回单须先编辑保存（`-1→1`）再提交。

核销规则：

- 明细核销金额必须 `> 0`
- 单次核销不得超过 `VerificationToBe`
- 核销后自动回写：
  - `purchaseorderitem.finance_payment_status`（0/1/2）
  - `purchaseorder.finance_status`（0/1/2）

---

## 2. 收款单（FinanceReceipt）

- 主状态：`0新建 -> 3确认`（取消为 `4`）
- 历史兼容：`1` 视为新建，`2` 视为确认；迁库 `1→0`、`2→3`
- **详细口径**见 [收款单状态-设计与实现](../../System/财务/收款单状态-设计与实现.md)

允许流转：

- `0 -> 3`（确认；权限 `finance-receipt.write`）
- `0 -> 4`（新建取消）
- `3 -> 4`（确认后整单未核销可取消；已核销须先反核销；预收冲不回或货代已付款则禁止）

核销规则：

- 仅 **确认** 状态可核销 / 入预收池
- 明细核销金额必须 `> 0`
- 单次核销不得超过 `ReceiptConvertAmount`
- 核销后自动回写：
  - `financesellinvoice.receive_status`（0/1/2）
  - `sellinvoiceitem.receive_status`（随主单）
  - `sellorder.finance_receipt_status`（0/1/2）

### 2.1 状态 × 用户操作

| 状态 | 编辑 | 确认 | 取消 | 核销 |
|------|:----:|:----:|:----:|:----:|
| 0 新建 | ✅ | ✅ | ✅ | — |
| 3 确认 | ❌ | — | ✅（整单未核销；预收可冲回；无货代付款） | ✅ |
| 4 取消 | ❌ | — | — | — |

确认/取消权限：仅 `finance-receipt.write`。待审批桌面不再收录收款。

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

- 付款：submit/approve/reject/complete/cancel；**编辑请款** `PUT .../request`、**付款执行** `PUT .../execution`、**撤回** `POST .../withdraw`（见 [付款单状态与操作权限](../../System/财务/付款单状态与操作权限.md) §7）
- 收款：submit/approve/confirm-received/cancel
- 进项：confirm/unconfirm/red-invoice
- 销项：submit-application/mark-issued/mark-issue-failed/void

