# RBAC 权限控制节点清单

与 `purchase.amount.read` 同类的权限码（`PermissionCode`）整理：以 `sys_permission` 种子 `seed_initial_rbac_admin.sql` 为主，并补充代码中使用、需在库中维护的节点。

---

## 一、`seed_initial_rbac_admin.sql` 中的权限（主数据）

| PermissionCode | 中文名（种子） | Resource | Action | 典型用途 |
|----------------|----------------|----------|--------|----------|
| `customer.read` | 客户-查看 | customer | read | 客户列表/详情 |
| `customer.write` | 客户-维护 | customer | write | 新建/编辑客户、审批 |
| `customer.info.read` | 客户敏感信息-查看 | customer | info.read | 客户敏感字段（与 `customer.read` 区分） |
| `vendor.read` | 供应商-查看 | vendor | read | 供应商列表/详情、银行子资源等读接口基类 |
| `vendor.write` | 供应商-维护 | vendor | write | 新建/改供应商、审批（供应商） |
| `vendor.info.read` | 供应商敏感信息-查看 | vendor | info.read | 供应商敏感字段、部分 PO 掩码里的联系人/地址 |
| `rfq.read` | 询价/需求-查看 | rfq | read | 需求/询价读 |
| `rfq.write` | 询价/需求-维护 | rfq | write | 需求/询价写 |
| `sales-order.read` | 销售订单-查看 | sales-order | read | 销售订单读 |
| `sales-order.write` | 销售订单-维护 | sales-order | write | 销售订单写、销售订单审批 |
| `sales.amount.read` | 销售金额-查看 | sales | amount.read | 销售侧金额/单价类掩码（与订单读区分） |
| `purchase-order.read` | 采购订单-查看 | purchase-order | read | 采购订单读、部分付款读接口（`RequireAny`） |
| `purchase-order.write` | 采购订单-维护 | purchase-order | write | 采购订单写、采购订单审批、部分付款写接口（`RequireAny`） |
| `purchase.amount.read` | 采购金额-查看 | purchase | amount.read | 采购订单/明细单价、合计、行金额等掩码 |
| `draft.read` | 草稿-查看 | draft | read | 草稿箱读 |
| `draft.write` | 草稿-维护 | draft | write | 草稿写 |
| `rbac.manage` | 用户/角色/权限管理 | rbac | manage | 员工/角色/权限、字典管理、公司参数、汇率维护等 |
| `finance-receipt.read` | 收款-查看 | finance-receipt | read | 收款单读 |
| `finance-receipt.write` | 收款-维护 | finance-receipt | write | 收款单写 |
| `finance-payment.read` | 付款-查看 | finance-payment | read | 付款单读 |
| `finance-payment.write` | 付款-维护 | finance-payment | write | 付款单写 |
| `finance-sell-invoice.read` | 销项发票-查看 | finance-sell-invoice | read | 销项发票读 |
| `finance-sell-invoice.write` | 销项发票-维护 | finance-sell-invoice | write | 销项发票写 |
| `finance-purchase-invoice.read` | 进项发票-查看 | finance-purchase-invoice | read | 进项发票读 |
| `finance-purchase-invoice.write` | 进项发票-维护 | finance-purchase-invoice | write | 进项发票写 |

---

## 二、代码中大量使用、须在 `sys_permission` 中有对应行

| PermissionCode | 说明 |
|----------------|------|
| `purchase-requisition.read` | 采购申请读（如 `PurchaseRequisitionsController`） |
| `purchase-requisition.write` | 采购申请写；与 `sales-order.write` 在个别接口上 `RequireAny` |

若库里没有这两条，需在 `sys_permission` 中补录并给角色授权（可参考 `scripts/ensure_purchase_buyer_vendor_write.sql` 等脚本）。

---

## 三、待办审批绑定的业务权限（`ApprovalsController`）

| 业务类型 | PermissionCode（审批操作依赖） |
|----------|--------------------------------|
| VENDOR | `vendor.write` |
| SALES_ORDER | `sales-order.write` |
| PURCHASE_ORDER | `purchase-order.write` |
| CUSTOMER | `customer.write` |
| FINANCE_RECEIPT | `finance-receipt.write` |
| FINANCE_PAYMENT | `finance-payment.write` |

---

## 四、`RbacService` 合并/剥离（影响 JWT 中是否出现上述码）

- **销售主部门（IdentityType = 1）**：会剥离 `purchase-order.*`、`purchase.amount.read`、付款/进项发票等；可合并 `customer.*`、`sales-order.*`、`rfq.*`；非采购部门且具备销售订单权限时可合并 `purchase-requisition.*`。
- **采购侧部门（`belongsToPurchaseDept`）**：可合并 `vendor.*`、`purchase-requisition.*`、`purchase-order.*`、`purchase.amount.read` 等（与仅 `DEPT_EMPLOYEE` 时的缺口对齐）。
- **采购/采购助理主部门（2 / 3）**：剥离销售订单与收款、销项发票等。

实现位置：`CRM.Core/Services/RbacService.cs`。

---

## 五、未单独挂资源权限码的模块

如：`QuotesController`、`InventoryCenterController`、`StockInController`、`StockOutController`、`LogisticsController` 等多为仅 `[Authorize]` 或无统一 `RequirePermission`，**不与 `purchase.amount.read` 同级列入「资源权限节点」**。若要对齐 RBAC，需另起 `quote.read` / `inventory.read` 等设计后再挂特性。

---

## 相关文件

| 说明 | 路径 |
|------|------|
| 权限种子 | `seed_initial_rbac_admin.sql` |
| 运行时合并/剥离 | `CRM.Core/Services/RbacService.cs` |
| 审批业务权限 | `CRM.API/Controllers/ApprovalsController.cs` |

---

*文档生成说明：与代码库当前实现一致；新增 API 或种子后请同步更新本清单。*
