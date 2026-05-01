# 强制删除 API 契约与错误语义

本文档与 `/swagger` 首页说明一致，描述当前所有 `POST …/force-delete` 路由的统一约定，便于前后端与运维对齐。

## 通用约定

| 项 | 说明 |
| --- | --- |
| 鉴权 | 均需有效 JWT（`Authorization: Bearer …`），具体权限以各控制器 `RequirePermission` 为准。 |
| 系统管理员 | 凡标注「仅 SYS_ADMIN」的接口，服务端以 RBAC `IsSysAdmin == true` 为准；否则返回 **403**。 |
| 请求体 | JSON；确认字段统一为 **`confirmBillCode`**（camelCase，与 ASP.NET Core 默认序列化一致），须与下表「确认比对字段」**完全一致**（Ordinal 字符串比较，一般区分大小写）。 |
| 幂等与副作用 | 成功则执行硬删或业务约定的删除路径；失败不删。无独立业务错误码枚举，以 HTTP 状态 + `message` 为准。 |

## 响应体两种形态

### A. `ApiResponse<T>`（库存、物流、出库、入库、报关、库存中心等）

HTTP 响应 JSON 形如：

```json
{
  "success": true,
  "message": "…",
  "data": null,
  "errorCode": 0
}
```

失败时常见：`success: false`，`message` 为人类可读原因，`errorCode` 与控制器传入一致（多为 400 / 403 / 404 / 500）。

前端统一使用 `getApiErrorMessage`（`CRM.Web/src/utils/apiError.ts`）即可解析 **`message`**。

### B. 财务付款/收款/进销项发票、采购申请

HTTP 响应 JSON 形如：

```json
{ "success": true, "message": "…" }
```

或失败：`{ "success": false, "message": "…" }`。同样可被 `getApiErrorMessage` 解析。

### HTTP 状态与前端提示

| HTTP | 典型含义 |
| --- | --- |
| **200** | 成功；读 `message` 作成功提示。 |
| **400** | 参数缺失、确认号不匹配、业务守卫拒绝（下游存在等）。 |
| **403** | 未登录/非系统管理员等。 |
| **404** | 目标主键不存在。 |
| **409** | 资源冲突（当前仅采购申请强制删除等服务层 `InvalidOperationException` 映射）。 |
| **500** | 未预期异常；`message` 多为异常文本。 |

---

## 路由一览

路径中 `{id}` 均为服务端主键（UUID 等），与 `confirmBillCode` 的业务单号不同。

| 模块 | 方法 | 路径 | 响应形态 | 仅 SYS_ADMIN | `confirmBillCode` 须等于 |
| --- | --- | --- | --- | --- | --- |
| 出库通知 | POST | `api/v1/stock-out/request/{id}/force-delete` | A | 是 | `RequestCode` |
| 出库单 | POST | `api/v1/stock-out/{id}/force-delete` | A | 是 | `StockOutCode` |
| 入库单 | POST | `api/v1/stock-in/{id}/force-delete` | A | 是 | `StockInCode` |
| 报关单 | POST | `api/v1/customs-declarations/{id}/force-delete` | A | 是 | `DeclarationCode` |
| 到货通知 | POST | `api/v1/logistics/arrival-notices/{id}/force-delete` | A | 是 | `NoticeCode` |
| 质检单 | POST | `api/v1/logistics/qcs/{id}/force-delete` | A | 是 | `QcCode` |
| 库存明细 | POST | `api/v1/inventory-center/stock-items/{id}/force-delete` | A | 是 | `StockItemCode`，若空则等于明细主键 `id` |
| 拣货单 | POST | `api/v1/inventory-center/picking-list/{id}/force-delete` | A | 是 | `TaskCode`；**任意** `Status`；若存在关联**出库单**则 400（见下表「拣货单」） |
| 采购申请 | POST | `api/v1/purchase-requisitions/{id}/force-delete` | B | 是 | 采购申请单号（服务层校验，与列表展示单号一致） |
| 付款单 | POST | `api/v1/finance/payments/{id}/force-delete` | B | 是 | `FinancePaymentCode` |
| 收款单 | POST | `api/v1/finance/receipts/{id}/force-delete` | B | 是 | `FinanceReceiptCode` |
| 进项发票 | POST | `api/v1/finance/purchase-invoices/{id}/force-delete` | B | 是 | **`InvoiceNo`**（发票号码，不做 Id 兜底） |
| 销项发票 | POST | `api/v1/finance/sell-invoices/{id}/force-delete` | B | 是 | **`InvoiceCode`**（发票代码，不做 Id 兜底） |

权限码（摘录）：财务类为 `finance-*-invoice.write` / `finance-payment.write` / `finance-receipt.write`；采购申请为 `purchase-requisition.write`；其余以各控制器特性为准。

---

## 业务守卫（400 时 `message` 示例方向）

以下与 `IForceDeleteGuardService` 或控制器内联校验一致，实际文案以服务端返回为准。

| 区域 | 拒绝删除的典型条件 |
| --- | --- |
| 出库通知（**普通删除** `DELETE …/request/{id}` 与**强制删除**） | 存在未取消的 **拣货单**（`PickingTask.StockOutRequestId` = 该通知；`Status != -1` 且未软删）。二者共用 `CanForceDeleteStockOutRequestAsync`。 |
| 出库单 | 已执行出库等状态；或销项明细等引用出库明细。强制删除走软删级联，**不**与库存自动冲销合并为一步。 |
| 入库单（`DELETE …/stock-in/{id}` 与**强制删除**） | 存在未软删 **库存明细** `StockItem` 且 `StockInId` = 本入库单。 |
| 财务付款/收款 | 存在核销类下游字段表明已核销。 |
| 进项发票 | 已认证 / 已冲红等。 |
| 销项发票 | 存在收款核销等下游。 |
| 到货通知 | 已有质检单，或状态/数量已进入质检、收货链路。 |
| 质检单 | 已绑定或进入入库链路、存在关联入库单。 |
| 库存明细 | 拣货或出库明细引用。 |
| 拣货单（普通删除 `DELETE …/picking-list/{id}`） | ① 非待拣货（`Status != 1`）不可删。② 存在关联**出库单**（见下表）。 |
| 拣货单（强制删除） | ① 存在关联**出库单**（`StockOut.PickingTaskId` = 本任务，或任一 `StockOutItem.PickingTaskItemId` 指向本任务拣货明细），未软删则不可删。② 仍**不按拣货状态**拦截（与「仅待拣货可普通删」不同）。 |

---

## 各强制删除节点与「有则拦截」的下游/条件

说明：**拦截**指接口在业务校验阶段返回 **400**（采购申请存在下游时多为 **409 Conflict** + `InvalidOperationException` 文案）且**不执行删除**；**不拦截**表示当前代码路径**未**对该类下游做业务 400 判断（仍可能因数据库约束等失败）。实体名为便于对照代码/库表的概念名。

| 被删节点 | 有则拦截（下游或条件） | 实现位置 |
| --- | --- | --- |
| **出库通知** | 同上；**普通删除**与**强制删除**均调用 `CanForceDeleteStockOutRequestAsync`（`Status == -1` 的已取消拣货单不拦截）。 | `IForceDeleteGuardService.CanForceDeleteStockOutRequestAsync`；`StockOutController.DeleteRequest` / `ForceDeleteRequest` |
| **出库单** | ① 本单 `Status` 为 **2 或 4**（已执行出库类，不可直接强删）。② 存在 **销项发票明细** `SellInvoiceItem`，其 `StockOutItemId` 指向本单任一 **出库明细** `StockOutItem`。 | `IForceDeleteGuardService.CanForceDeleteStockOutAsync` |
| **入库单** | 存在未软删 **库存明细** `StockItem` 且 `StockInId` = 本入库单（在库层仍占用），则 **普通删除**与**强制删除**均 **400**（`ArgumentException`）。无在库明细时仍走原 `DeleteAsync` 级联删入库明细等。 | `StockInService.DeleteAsync`；`StockInController.Delete` / `ForceDelete` |
| **报关单** | **无**业务级下游 400 拦截（校验确认号、`SYS_ADMIN` 后即删；关联移库单走软删，不作为拒绝条件）。 | `CustomsDeclarationsController` |
| **到货通知** | ① 存在 **质检单** `QCInfo`，`StockInNotifyId` = 本通知。② 或本通知 `Status >= 30`。③ 或 `ReceiveQty > 0` / `PassedQty > 0`（已进入收货/质检数量链路）。 | `LogisticsController.ForceDeleteArrivalNotice` |
| **质检单** | ① `StockInId` 已绑定 **或** `StockInStatus >= 100`（已进入入库链路）。② 或存在主键为 `StockInId` 的 **入库单** `StockIn` 记录。 | `LogisticsController.ForceDeleteQc` |
| **库存明细** | ① 存在 **出库明细** `StockOutItem`，`StockItemId` = 本库存明细。② 或存在 **拣货明细** `PickingTaskItem`，`StockItemId` = 本库存明细。 | `InventoryCenterController.ForceDeleteStockItem` |
| **拣货单** | 存在未软删 **出库单** `StockOut` 且 `PickingTaskId` = 本拣货任务；**或** 存在未软删 **出库明细** `StockOutItem` 且 `PickingTaskItemId` ∈ 本任务拣货明细 Id。满足则普通删除与强制删除均 **400**（强制删除另需 `SYS_ADMIN` + `TaskCode`）。 | `InventoryCenterController.HasStockOutLinkedToPickingTaskAsync` → `DeletePickingSlip` / `ForceDeletePickingSlip` |
| **采购申请** | 存在 **采购订单明细** `PurchaseOrderItem`，其 `SellOrderItemId` = 本申请关联的销售订单明细（以销定采已落单）。 | `PurchaseRequisitionService.ForceDeleteAsync`（`HasPurchaseOrderDownstreamAsync`） |
| **付款单** | 本单下任一 **付款明细** `FinancePaymentItem`：`VerificationStatus > 0` **或** `VerificationDone > 0`（已核销/部分核销）。 | `IForceDeleteGuardService.CanForceDeleteFinancePaymentAsync` |
| **收款单** | 本单下任一 **收款明细** `FinanceReceiptItem`：`VerificationStatus > 0` **或** `VerifiedAmount > 0`。 | `IForceDeleteGuardService.CanForceDeleteFinanceReceiptAsync` |
| **进项发票** | 票面状态：已认证（`ConfirmStatus == 1`）或已冲红（`RedInvoiceStatus == 1`）。 | `IForceDeleteGuardService.CanForceDeleteFinancePurchaseInvoiceAsync` |
| **销项发票** | 头：`ReceiveStatus > 0` 或 `ReceiveDone > 0`；**或** 任一 **销项发票明细** `SellInvoiceItem`（表内行）`ReceiveStatus > 0`。 | `IForceDeleteGuardService.CanForceDeleteFinanceSellInvoiceAsync` |

**已移除的节点**：库存聚合 `Stock` 的 `force-delete` 接口已删除，上表不再列出。

---

## 与 OpenAPI

Swagger UI（`/swagger`）全局 Description 中附有本文件短链接式说明；完整字段表与语义以本文档为准。
