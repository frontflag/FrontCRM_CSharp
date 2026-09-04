# RBAC 权限体系 — 权限测试对照说明

> **关联文档**
> - 需求：[PRD/RBAC权限系统PRD.md](../../PRD/RBAC权限系统PRD.md)
> - 需求：[PRD/管理角色分级与权限体系PRD.md](../../PRD/管理角色分级与权限体系PRD.md)
> - 实现：[System/权限/权限-部门.md](../../System/权限/权限-部门.md)
> - 实现：[System/权限/数据权限-业务员客户与采购员供应商.md](../../System/权限/数据权限-业务员客户与采购员供应商.md)
> - 实现：[System/管理角色三级-设计与实现.md](../../System/管理角色三级-设计与实现.md)
> - 实现：[System/部门组织角色编码.md](../../System/部门组织角色编码.md)
> - 节点清单：[实现方案/RBAC权限节点清单.md](../../实现方案/RBAC权限节点清单.md)

---

## 一、三维权限模型基础

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| RBAC-001 | 用户权限汇总流程 | 任意用户登录 | 1. 登录后调用 `GET /api/v1/auth/permission-summary` | 返回 `permissionCodes`、`identityType`、`saleDataScope`、`purchaseDataScope`、`isSysAdmin`、`isSysManager`、`isBizManager`、`hasBizDataBypass`、`roleCodes` 等字段 | P0 |
| RBAC-002 | 主部门决定业务身份 | 用户有多个部门 | 1. 查看 permission-summary 中的 `identityType` | 与主部门（`IsPrimary=true`）的 `IdentityType` 一致；兼任部门不影响 | P0 |
| RBAC-003 | 角色继承权限 | 用户只挂一个角色 | 1. 查看 permission-summary | 权限码为该角色绑定权限的并集 | P0 |
| RBAC-004 | 多角色权限合并 | 用户挂多个角色 | 1. 查看 permission-summary | 权限码为所有角色绑定权限的并集 | P0 |
| RBAC-005 | 权限码禁用后失效 | 某权限码状态置 0 | 1. 用户重新登录 | permission-summary 中不再包含该禁用权限码 | P1 |
| RBAC-006 | 部门禁用后用户身份 | 用户主部门被禁用 | 1. 用户重新登录 | 按产品设计处理：通常无法登录或身份为空；需确认具体行为 | P1 |

---

## 二、部门数据范围（Scope）

### 2.1 范围枚举通用性

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| SCOPE-001 | 销售数据范围 0=全部 | 用户主部门 `SaleDataScope=0` | 1. 打开销售订单/客户列表 | 可见全部销售订单/客户 | P0 |
| SCOPE-002 | 销售数据范围 1=自己 | 用户主部门 `SaleDataScope=1` | 1. 打开销售订单/客户列表 | 仅可见 `SalesUserId` / `CreateByUserId` 为当前用户的数据 | P0 |
| SCOPE-003 | 销售数据范围 2=本部门 | 用户主部门 `SaleDataScope=2` | 1. 打开销售订单/客户列表 | 仅可见同部门用户创建/归属的数据 | P0 |
| SCOPE-004 | 销售数据范围 3=本部门及下级 | 用户主部门 `SaleDataScope=3` | 1. 打开销售订单/客户列表 | 可见本部门及子部门路径下用户的数据 | P0 |
| SCOPE-005 | 销售数据范围 4=禁止 | 用户主部门 `SaleDataScope=4` | 1. 打开销售订单/客户列表 | 无数据（`WHERE 1=0`） | P0 |
| SCOPE-006 | 采购数据范围 0=全部 | 用户主部门 `PurchaseDataScope=0` | 1. 打开采购订单列表 | 可见全部采购订单 | P0 |
| SCOPE-007 | 采购数据范围 1=自己 | 用户主部门 `PurchaseDataScope=1` | 1. 打开采购订单列表 | 仅可见 `PurchaseUserId` / `Assistor` / `CreateByUserId` 为当前用户的数据 | P0 |
| SCOPE-008 | 采购数据范围 2=本部门 | 用户主部门 `PurchaseDataScope=2` | 1. 打开采购订单列表 | 仅可见同部门用户的数据 | P0 |
| SCOPE-009 | 采购数据范围 3=本部门及下级 | 用户主部门 `PurchaseDataScope=3` | 1. 打开采购订单列表 | 可见本部门及子部门路径下用户的数据 | P0 |
| SCOPE-010 | 采购数据范围 4=禁止 | 用户主部门 `PurchaseDataScope=4` | 1. 打开采购订单列表 | 无数据 | P0 |
| SCOPE-011 | 物流数据范围 4=禁止 | 用户主部门 `LogisticsDataScope=4` | 1. 查看侧栏 | 入库/出库/库存/报关整组菜单隐藏 | P0 |
| SCOPE-012 | 财务数据范围 4=禁止 | 用户主部门 `FinanceDataScope=4` | 1. 查看侧栏 | 付款管理、收款管理、进项/销项发票菜单隐藏 | P0 |
| SCOPE-013 | 范围修改后需重新登录 | 修改部门 `SaleDataScope` | 1. 不重新登录，刷新列表<br>2. 重新登录后刷新列表 | 步骤1仍按旧范围；步骤2按新范围 | P0 |

### 2.2 访问方式（Access）

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| ACCESS-001 | 销售数据读写 0 | 用户主部门 `SaleDataAccess=0` | 1. 查看 permission-summary<br>2. 尝试编辑销售订单 | 保留 `sales-order.write`；可编辑 | P0 |
| ACCESS-002 | 销售数据只读 1 | 用户主部门 `SaleDataAccess=1` | 1. 查看 permission-summary<br>2. 尝试编辑销售订单 | permission-summary 中无 `sales-order.write`；保存 403 或按钮隐藏 | P0 |
| ACCESS-003 | 采购数据只读 1 | 用户主部门 `PurchaseDataAccess=1` | 1. 查看 permission-summary<br>2. 尝试编辑采购订单 | 无 `purchase-order.write`；保存 403 或按钮隐藏 | P0 |
| ACCESS-004 | 财务数据只读 1 | 用户主部门 `FinanceDataAccess=1` | 1. 查看 permission-summary<br>2. 尝试保存付款执行 | 无 `finance-payment.write`；保存 403 或按钮隐藏 | P0 |
| ACCESS-005 | 禁止范围时 Access 下拉禁用 | 部门 `SaleDataScope=4` | 1. 打开部门编辑页 | `SaleDataAccess` 下拉禁用；保存时强制为 0 | P1 |

---

## 三、业务身份（IdentityType）与权限码剥离

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| ID-001 | 销售身份剥离采购财务码 | 用户主部门 `IdentityType=1`（销售） | 1. 查看 permission-summary | 剥离 `finance-payment.*`、`finance-purchase-invoice.*` | P0 |
| ID-002 | 采购身份剥离销售财务码 | 用户主部门 `IdentityType=2/3`（采购/采购助理） | 1. 查看 permission-summary | 剥离 `finance-receipt.*`、`finance-sell-invoice.*` | P0 |
| ID-003 | 财务身份补全财务码 | 用户主部门 `IdentityType=5`（财务） | 1. 查看 permission-summary | 补全 8 个 `finance-*` 的 read/write（即使角色种子只有 read） | P0 |
| ID-004 | 客服身份剥离部分财务码 | 用户主部门 `IdentityType=4`（客服） | 1. 查看 permission-summary | 按设计剥离 `finance-purchase-invoice.read` 等 | P1 |
| ID-005 | 隐藏客户管理 | 部门 `HideCustomerManagement=true` | 1. 查看 permission-summary<br>2. 查看侧栏 | 剥离 `customer.read`/`customer.write`；隐藏「客户管理」菜单；拦截客户路由（财务身份例外保留 `customer.read`） | P0 |
| ID-006 | 隐藏供应商管理 | 部门 `HideVendorManagement=true` | 1. 查看 permission-summary<br>2. 查看侧栏 | 剥离 `vendor.read`/`vendor.write`；隐藏「供应商管理」菜单；拦截供应商路由（财务身份例外保留 `vendor.read`） | P0 |
| ID-007 | 销售身份不可见采购订单菜单 | 用户主部门 `IdentityType=1` | 1. 查看侧栏 | 仅可见采购申请；不可见采购订单、采购订单明细（管理角色例外） | P0 |
| ID-008 | 采购身份不可见销售订单菜单 | 用户主部门 `IdentityType=2/3` | 1. 查看侧栏 | 不可见销售订单等销售菜单（管理角色例外） | P0 |

---

## 四、管理角色与系统管理门禁

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| MGT-001 | SYS_ADMIN 全放行 | 用户挂 `SYS_ADMIN` | 1. 查看 permission-summary | `isSysAdmin=true`、`hasBizDataBypass=true`；业务数据不受范围限制；系统菜单需对应 `system.*` | P0 |
| MGT-002 | SYS_MANAGER 业务数据 bypass | 用户挂 `SYS_MANAGER` | 1. 查看 permission-summary<br>2. 打开需求列表 `/rfq`<br>3. 打开需求明细 `/rfq-items`（空筛选） | `isSysManager=true`、`hasBizDataBypass=true`；业务菜单/API 放行；**需求列表有记录时明细作业页不得为空**（与主表同 bypass，不得只认 `IsSysAdmin`）；系统菜单需 `system.*`；不可见 SuperAdmin 账号 | P0 |
| MGT-003 | SYS_BIZ_MANAGER 业务数据 bypass | 用户挂 `SYS_BIZ_MANAGER` | 1. 查看 permission-summary | `isBizManager=true`、`hasBizDataBypass=true`；业务菜单/API 放行；不可建 Manager/Admin | P1 |
| MGT-004 | SYS_MGR_* 不 bypass 业务数据 | 用户挂 `SYS_MGR_SALES` | 1. 查看 permission-summary | `hasBizDataBypass=false`；业务单据仍受主部门范围限制；系统管理需 `hasManagementAccess + system.*` | P0 |
| MGT-005 | system.* 需管理身份 | 普通员工被误配 `system.org.users.read` | 1. 打开系统管理菜单 | 无菜单；直接打开 `/system/users` 返回 403 | P0 |
| MGT-006 | system.* 需管理身份 + 权限码 | SYS_MANAGER 无 `system.org.users.read` | 1. 打开员工管理 | 无菜单；直接打开返回 403 | P0 |
| MGT-007 | SuperAdmin 专属页面 | 非 SYS_ADMIN | 1. 直接打开 `/debug/super` | 404（不跳登录、不跳 dashboard） | P0 |
| MGT-008 | 管理角色不受 IdentityType 藏菜单影响 | SYS_MANAGER 主部门为销售 | 1. 查看侧栏 | 仍可见采购订单、付款等对称菜单 | P0 |
| MGT-009 | 参数模块权限约定 | 用户挂 SYS_MGR_SALES | 1. 查看侧栏 | 可见 `system.params.sales.read` 对应子菜单；页内子项需 `system.params.{area}.{feature}.read|write` | P1 |

---

## 五、数据权限服务（DataPermissionService）

### 5.1 业务员 — 客户

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| DPS-C-001 | FilterCustomersAsync 按 SaleDataScope | 用户 `SaleDataScope=1` | 1. 打开客户列表 | 仅可见 `SalesUserId` 为当前用户的客户 | P0 |
| DPS-C-002 | FilterSalesOrdersAsync 按 SaleDataScope | 用户 `SaleDataScope=2` | 1. 打开销售订单列表 | 仅可见 `SalesUserId` 落在允许用户集合的销售订单 | P0 |
| DPS-C-003 | 财务部收款不按客户业务员 | 用户 `IdentityType=5`、`SaleDataScope=1` | 1. 打开收款单列表 | 财务身份下不按 `SalesUserId` 缩小 | P0 |

### 5.2 采购员 — 供应商

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| DPS-V-001 | FilterVendorsAsync 非禁止时全量 | 用户 `PurchaseDataScope=0/1/2/3` | 1. 打开供应商列表 | 返回全部供应商，不按 `PurchaseUserId` 缩小 | P0 |
| DPS-V-002 | FilterVendorsAsync 禁止时为空 | 用户 `PurchaseDataScope=4` | 1. 打开供应商列表 | 返回空列表 | P0 |
| DPS-V-003 | FilterPurchaseOrdersAsync 按 PurchaseDataScope | 用户 `PurchaseDataScope=2` | 1. 打开采购订单列表 | 按 `PurchaseUserId` / `Assistor` + 允许用户集合过滤 | P0 |
| DPS-V-004 | 到货通知/质检/入库批次按采购单范围 | 用户 `PurchaseDataScope=1` | 1. 打开到货通知列表 | 仅可见关联到可见采购单的行 | P0 |
| DPS-V-005 | 财务部付款不按供应商采购员 | 用户 `IdentityType=5`、`PurchaseDataScope=1` | 1. 打开付款单列表 | 财务身份下不按 `Vendor.PurchaseUserId` 缩小 | P0 |

### 5.3 采购执行链路

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| DPS-L-001 | 无法关联到可见采购单的行不展示 | 用户 `PurchaseDataScope=1` | 1. 打开入库单/库存明细列表 | 无法关联到可见采购单的行不展示 | P0 |
| DPS-L-002 | 范围 0 可见全量 | 用户 `PurchaseDataScope=0` | 1. 打开采购执行链路列表 | 可见全量 | P0 |

### 5.4 需求（RFQ）主表与明细

本功能不适用：金额核销/匹配足额超额（非金额提交）。

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| DPS-RFQ-001 | SYS_MANAGER 需求明细与主表一致 bypass | 用户挂 `SYS_MANAGER`（非 SYS_ADMIN），主部门销售、`SaleDataScope=1`，需求业务员为他人 | 1. 打开需求列表（空筛选）<br>2. 打开需求明细（空筛选）<br>3. 可选：需求明细看板 | 列表有记录时，明细「共 N 条」且 N 大于 0，并与列表「明细条目」合计可对账；看板同源 | P0 |
| DPS-RFQ-002 | 普通业务员需求明细仍按范围 | 无管理角色、`SaleDataScope=1`，需求归属他人 | 1. 打开需求明细空筛选 | 不可见他人业务员的明细 | P0 |

### 5.5 报价列表看板

本功能不适用：金额核销/匹配足额超额（看板以计数/比率/时长为主，无核销提交）。

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| DPS-QUOTE-001 | SYS_MANAGER 报价列表看板并联需求与列表一致 | 用户挂 `SYS_MANAGER`（非 SYS_ADMIN），主部门销售、`SaleDataScope=1`，报价/需求归属其他业务员 | 1. 打开报价列表（空筛选）确认有行<br>2. 切看板 | 询价报价率 / 查无报价 / 转化等并联需求 KPI 不得因只豁免 `IsSysAdmin` 而整板为空或明显小于列表可见报价所对应的需求 | P0 |

---

## 六、列级脱敏与金额字段

> 代码实现：`CRM.Core/Utilities/SaleSensitiveFieldMask521.cs` 和 `PurchaseSensitiveFieldMask511.cs`。当用户处于对侧数据范围=4（禁止）时，对应销售/采购敏感字段会被脱敏或隐藏。

### 6.1 列级脱敏 — 通用规则

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| MASK-001 | 销售方向 + PurchaseDataScope=4 采购列脱敏 | 用户 `IdentityType=1`、`PurchaseDataScope=4` | 1. 打开含采购列的页面 | 采购相关字段隐藏或占位 | P0 |
| MASK-002 | 采购方向 + SaleDataScope=4 销售列脱敏 | 用户 `IdentityType=2`、`SaleDataScope=4` | 1. 打开含销售列的页面 | 销售相关字段隐藏或占位 | P0 |
| MASK-003 | 系统管理员不脱敏 | 用户 `SYS_ADMIN` | 1. 打开上述页面 | 所有字段正常显示 | P0 |
| MASK-004 | 附件跨业务线禁止访问 | 用户 `PurchaseDataScope=4` | 1. 尝试访问采购相关附件上传/下载 | 禁止访问或隐藏入口 | P1 |

### 6.2 销售敏感字段清单（SaleSensitiveFieldMask521）

> 触发条件：采购侧用户查看销售单据且 `SaleDataScope=4`。字段由代码枚举，测试时应逐一检查以下字段是否被隐藏/置空。

| 字段类型 | 字段示例 |
|----------|----------|
| 销售价格 | `SalePrice`、`SaleAmount`、`SalesAmountUsd`、`SalesAmountCny`、`TargetSalePrice`、`QuotedSalePrice` |
| 客户信息 | `CustomerContactName`、`CustomerContactPhone`、`CustomerContactEmail`、`CustomerPaymentTerm`、`CustomerAddress` |
| 销售利润 | `EstimatedProfit`、`ProfitMargin`、`GrossProfit`、`SalesCommission`、`SalesBonus` |
| 销售备注 | `SalesInternalNote`、`SalesCustomerNote`、`SalesQuotationNote` |
| 销售附件 | `SalesAttachmentUrl`、`SalesQuotationAttachmentId` |
| 销售专属标识 | `SalesOrderNo`、`SalesContractNo`、`SalesQuoteNo`、`CustomerPoNo` |

### 6.3 采购敏感字段清单（PurchaseSensitiveFieldMask511）

> 触发条件：销售侧用户查看采购单据且 `PurchaseDataScope=4`。

| 字段类型 | 字段示例 |
|----------|----------|
| 采购价格 | `PurchasePrice`、`PurchaseAmount`、`PurchaseAmountUsd`、`PurchaseAmountCny`、`TargetPurchasePrice`、`QuotedPurchasePrice` |
| 供应商信息 | `VendorContactName`、`VendorContactPhone`、`VendorContactEmail`、`VendorPaymentTerm`、`VendorAddress`、`VendorBankInfo` |
| 采购成本 | `EstimatedCost`、`CostMargin`、`ActualCost`、`PurchaseCommission` |
| 采购备注 | `PurchaseInternalNote`、`VendorNote`、`PurchaseQuotationNote` |
| 采购附件 | `PurchaseAttachmentUrl`、`PurchaseQuotationAttachmentId` |
| 采购专属标识 | `PurchaseOrderNo`、`PurchaseContractNo`、`VendorQuoteNo`、`VendorInvoiceNo` |

### 6.4 附件跨业务线访问控制

> 代码实现：`CRM.Core/Utilities/CrossSideDocumentAttachmentPolicy.cs`、`DocumentsController.cs`。
> 跨业务线判定：当用户对侧数据范围=4 且附件业务类型属于对侧（销售侧访问采购附件 / 采购侧访问销售附件）时拒绝。

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| ATT-001 | 销售侧访问采购附件被拒绝 | `IdentityType=1`、`PurchaseDataScope=4` | 1. 调用 `/api/documents/{purchaseAttachmentId}` 或 preview | `CrossSideDocumentAttachmentPolicy` 返回 `true`；接口 403 或返回「禁止访问」 | P0 |
| ATT-002 | 采购侧访问销售附件被拒绝 | `IdentityType=2`、`SaleDataScope=4` | 1. 调用 `/api/documents/{salesAttachmentId}` | `CrossSideDocumentAttachmentPolicy` 返回 `true`；接口 403 | P0 |
| ATT-003 | 财务身份访问附件不被范围限制 | `IdentityType=5` | 1. 访问销售/采购附件 | 可正常访问 | P0 |
| ATT-004 | 管理角色访问附件不被范围限制 | `HasBizDataBypass=true` | 1. 访问对侧附件 | 可正常访问 | P0 |
| ATT-005 | 同业务线附件正常访问 | `IdentityType=1`、`SaleDataScope=1`、`PurchaseDataScope=4` | 1. 访问销售附件 | 正常 | P0 |

---

## 七、路由拦截

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| ROUTE-001 | 客户路由拦截 | 用户 `HideCustomerManagement=true` 且非 SYS_ADMIN | 1. 直接打开 `/customers` | 跳转 `/dashboard` | P0 |
| ROUTE-002 | 供应商路由拦截 | 用户 `HideVendorManagement=true` 且非 SYS_ADMIN | 1. 直接打开 `/vendors` | 跳转 `/dashboard` | P0 |
| ROUTE-003 | 无权限码直接打开业务路由 | 用户无 `customer.read` | 1. 直接打开 `/customers` | 403 或跳转 dashboard | P0 |
| ROUTE-004 | 无 system.* 打开系统路由 | 普通用户 | 1. 直接打开 `/system/users` | 403 或跳转 dashboard | P0 |

---

## 八、审批自审规则

> 代码实现核对：`CRM.Core/Utilities/SalesDirectorSelfApprovalRules.cs`、`PurchaseDirectorSelfApprovalRules.cs`。
> - 销售总监可自审条件：`IsSysAdmin == true` OR (`IdentityType == 1` 且角色码含 `DEPT_DIRECTOR`)
> - 采购总监可自审条件：`IsSysAdmin == true` OR (`IdentityType == 2 或 3` 且角色码含 `DEPT_DIRECTOR`)
> - 非总监级经理/员工：不可自审（但系统管理员仍可通过 `IsSysAdmin` 自审）

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| SELF-001 | 系统管理员可自审全部 | 用户 `SYS_ADMIN` 提交自己的待审 | 1. 打开审批桌面 | 显示「审核」，可通过/拒绝 | P0 |
| SELF-002 | 销售总监可自审客户/销售订单 | `DEPT_DIRECTOR` + `IdentityType=1` | 1. 提交自己的客户/销售订单<br>2. 打开待审批 | 显示「审核」 | P0 |
| SELF-003 | 销售总监不可自审供应商/采购订单 | `DEPT_DIRECTOR` + `IdentityType=1` | 1. 提交自己的供应商/采购订单 | 显示「仅查看」；decide 403 | P0 |
| SELF-004 | 采购总监可自审供应商/采购订单 | `DEPT_DIRECTOR` + `IdentityType=2/3` | 1. 提交自己的供应商/采购订单 | 显示「审核」 | P0 |
| SELF-005 | 采购总监不可自审客户/销售订单 | `DEPT_DIRECTOR` + `IdentityType=2/3` | 1. 提交自己的客户/销售订单 | 显示「仅查看」；decide 403 | P0 |
| SELF-006 | 经理/员工不可自审 | `DEPT_MANAGER` / `DEPT_EMPLOYEE` | 1. 提交自己的单据 | 显示「仅查看」 | P0 |
| SELF-007 | 付款不可自审 | 非系统管理员 | 1. 提交自己的付款 | 显示「仅查看」；decide 403 | P0 |
| SELF-008 | 无业务 write 权限的总监只能查看 | `DEPT_DIRECTOR` + `IdentityType=1` 但无 `customer.write` | 1. 打开自己的客户待审 | 显示「仅查看」 | P0 |
| SELF-009 | 已提交单据的采购员换供应商后审批桌面按最新展示 | 采购员修改供应商后 | 1. 打开审批桌面 | 按最新供应商展示（见 [采购订单换供应商-权限测试对照说明](./采购订单换供应商-权限测试对照说明.md)） | P1 |

---

## 九、付款单权限

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| PAY-001 | 编辑请款：PO 写或财务写 | 用户有 `purchase-order.write` 无 `finance-payment.write` | 1. 在 PO 详情付款 Tab 点编辑请款 | 可进入编辑；财务只读时 PO 写可 bypass | P0 |
| PAY-002 | 保存付款执行须财务写 | 用户只有 `purchase-order.write` | 1. 在付款单详情点保存付款执行 | 403 或按钮隐藏 | P0 |
| PAY-003 | 撤回：创建人或财务写 | 用户是创建人但无财务写 | 1. 在审核通过的付款单点撤回 | 可撤回 | P0 |
| PAY-004 | 审核通过/驳回须财务写 | 用户只有 `purchase-order.write` | 1. 在审批桌面审付款 | decide 403 | P0 |
| PAY-005 | 提交审核当前实现差异 | 用户只有 `purchase-order.write` | 1. 点「提交审核」按钮 | 当前 `POST .../submit` 仅 `finance-payment.write`；可能 403；需使用 `PATCH .../status` | P1 |
| PAY-006 | 财务部只读时保存付款执行被拒 | 用户 `FinanceDataAccess=1` 但有 `finance-payment.write` | 1. 点保存付款执行 | 服务层 `RejectIfFinanceDataReadOnlyAsync` 拦截，返回只读错误 | P0 |
| PAY-007 | 取消/删除须财务写且非只读 | 用户无 `finance-payment.write` | 1. 点取消/删除 | 403 或按钮隐藏 | P0 |

---

## 十、强制删除权限

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| FORCE-001 | 强制删除需 IsSysAdmin 或 IsSysManager | 普通用户 | 1. 调用 `POST .../force-delete` | 403 | P0 |
| FORCE-002 | 财务类单据强制删除需对应 write | SYS_MANAGER 无 `finance-payment.write` | 1. 强制删除付款单 | 403 | P0 |
| FORCE-003 | 付款完成且已核销须先反核销 | SYS_MANAGER 有 `finance-payment.write` | 1. 直接强制删除已核销完成的付款单 | 被拦截，提示先反核销 | P0 |

---

## 十一、登录日志权限

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| LOG-001 | 登录日志菜单权限 | 用户有 `system.logs.login.read` | 1. 查看系统日志 | 可见「登录日志」 | P1 |
| LOG-002 | 无登录日志权限 | 用户无 `system.logs.login.read` | 1. 查看系统日志 | 不可见「登录日志」 | P1 |

---

## 十二、权限变更生效时间

| 用例编号 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| EFF-001 | 部门范围修改后重新登录生效 | 管理员修改用户主部门范围 | 1. 用户不重新登录操作<br>2. 用户重新登录后操作 | 步骤1按旧权限；步骤2按新权限 | P0 |
| EFF-002 | 角色权限变更后重新登录生效 | 管理员修改角色权限 | 1. 用户不重新登录操作<br>2. 用户重新登录后操作 | 步骤1按旧权限；步骤2按新权限 | P0 |
| EFF-003 | 用户角色分配后重新登录生效 | 管理员给用户增删角色 | 1. 用户不重新登录操作<br>2. 用户重新登录后操作 | 步骤1按旧权限；步骤2按新权限 | P0 |

---

## 十三、关联业务权限文档

- [采购订单分面刷新-权限测试对照说明.md](./采购订单分面刷新-权限测试对照说明.md)
- [销售订单分面刷新-权限测试对照说明.md](./销售订单分面刷新-权限测试对照说明.md)
- [用户等级-权限测试对照说明.md](./用户等级-权限测试对照说明.md)
- [采购订单换供应商-权限测试对照说明.md](./采购订单换供应商-权限测试对照说明.md)
- [销售订单编辑保存-客户名称-权限测试对照说明.md](./销售订单编辑保存-客户名称-权限测试对照说明.md)
- [采购订单取消确认-权限测试对照说明.md](./采购订单取消确认-权限测试对照说明.md)
- [财务核销-权限测试对照说明.md](./财务核销-权限测试对照说明.md)
- [审批桌面-权限测试对照说明.md](./审批桌面-权限测试对照说明.md)
- [系统功能-权限测试对照说明.md](./系统功能-权限测试对照说明.md)

---

## 十四、PermissionCode 全表用例

> 数据来源：代码常量 `CRM.Core/Constants/SystemPermissionCodes.cs`、种子迁移 `CRM.Infrastructure/Migrations/*SeedPermissions*.cs`、`实现方案/RBAC权限节点清单.md`、控制器 `[RequirePermission]` 使用。

### 14.1 全量 PermissionCode 清单

> **说明**：清单以菜单/资源级粗粒度 PermissionCode 为主。代码实际控制器上还使用了更细粒度的 `system.params.*` 子权限（例如 `system.params.sales.refresh-customer.read`），详见 §14.1.2。

| 资源 | PermissionCode | 中文名 | 来源 |
|------|---------------|--------|------|
| 客户 | `customer.read` | 客户-查看 | 种子 |
| 客户 | `customer.write` | 客户-维护 | 种子 |
| 客户 | `customer.info.read` | 客户敏感信息-查看 | 种子 |
| 供应商 | `vendor.read` | 供应商-查看 | 种子 |
| 供应商 | `vendor.write` | 供应商-维护 | 种子 |
| 供应商 | `vendor.info.read` | 供应商敏感信息-查看 | 种子 |
| 询价/需求 | `rfq.read` | 询价/需求-查看 | 种子 |
| 询价/需求 | `rfq.write` | 询价/需求-维护 | 种子 |
| 询价/需求 | `rfq.create` | 询价/需求-新建 | SeedSalesRefreshCustomerPermissions |
| 销售订单 | `sales-order.read` | 销售订单-查看 | 种子 |
| 销售订单 | `sales-order.write` | 销售订单-维护 | 种子 |
| 销售 | `sales.amount.read` | 销售金额-查看 | 种子 |
| 采购申请 | `purchase-requisition.read` | 采购申请-查看 | 种子 |
| 采购申请 | `purchase-requisition.write` | 采购申请-维护 | 种子 |
| 采购订单 | `purchase-order.read` | 采购订单-查看 | 种子 |
| 采购订单 | `purchase-order.write` | 采购订单-维护 | 种子 |
| 采购 | `purchase.amount.read` | 采购金额-查看 | 种子 |
| 草稿 | `draft.read` | 草稿-查看 | 种子 |
| 草稿 | `draft.write` | 草稿-维护 | 种子 |
| 品牌 | `biz-brand.read` | 品牌-查看 | SeedBizBrandPermissions |
| 品牌 | `biz-brand.write` | 品牌-维护 | SeedBizBrandPermissions |
| AI | `biz.ai.customer_intel.lookup` | AI-客户情报调查 | SeedCustomerIntelReportAndScenario |
| AI | `biz.ai.material_intel.lookup` | AI-物料情报查询 | SeedMaterialIntelLookupScenario |
| 收款 | `finance-receipt.read` | 收款-查看 | 种子 |
| 收款 | `finance-receipt.write` | 收款-维护 | 种子 |
| 付款 | `finance-payment.read` | 付款-查看 | 种子 |
| 付款 | `finance-payment.write` | 付款-维护 | 种子 |
| 销项发票 | `finance-sell-invoice.read` | 销项发票-查看 | 种子 |
| 销项发票 | `finance-sell-invoice.write` | 销项发票-维护 | 种子 |
| 进项发票 | `finance-purchase-invoice.read` | 进项发票-查看 | 种子 |
| 进项发票 | `finance-purchase-invoice.write` | 进项发票-维护 | 种子 |
| 系统管理 | `rbac.manage` | 用户/角色/权限管理 | 种子 |
| 系统管理 | `system.org.company.update` | 公司信息-维护 | SystemPermissionCodes |
| 系统管理 | `system.org.departments.read` | 部门-查看 | SystemPermissionCodes |
| 系统管理 | `system.org.departments.write` | 部门-维护 | SystemPermissionCodes |
| 系统管理 | `system.org.departments.delete` | 部门-删除 | SystemPermissionCodes |
| 系统管理 | `system.org.roles.read` | 角色-查看 | SystemPermissionCodes |
| 系统管理 | `system.org.roles.write` | 角色-维护 | SystemPermissionCodes |
| 系统管理 | `system.org.roles.delete` | 角色-删除 | SystemPermissionCodes |
| 系统管理 | `system.org.users.read` | 员工-查看 | SystemPermissionCodes |
| 系统管理 | `system.org.users.write` | 员工-维护 | SystemPermissionCodes |
| 系统管理 | `system.org.users.delete` | 员工-删除 | SystemPermissionCodes |
| 系统管理 | `system.org.users.resetPassword` | 员工-重置密码 | SystemPermissionCodes |
| 系统管理 | `system.org.users.disable` | 员工-禁用 | SystemPermissionCodes |
| 系统管理 | `system.org.users.enable` | 员工-启用 | SystemPermissionCodes |
| 系统管理 | `system.org.permissions.read` | 权限-查看 | SystemPermissionCodes |
| 系统管理 | `system.org.permissions.write` | 权限-维护 | SystemPermissionCodes |
| 系统配置 | `system.config.company.read` | 公司参数-查看 | SystemPermissionCodes |
| 系统配置 | `system.config.company.write` | 公司参数-维护 | SystemPermissionCodes |
| 系统配置 | `system.config.tenant.read` | 租户参数-查看 | SystemPermissionCodes |
| 系统配置 | `system.config.tenant.write` | 租户参数-维护 | SystemPermissionCodes |
| 数据字典 | `system.data.dict.read` | 数据字典-查看 | SystemPermissionCodes |
| 数据字典 | `system.data.dict.write` | 数据字典-维护 | SystemPermissionCodes |
| 数据字典 | `system.data.dict.delete` | 数据字典-删除 | SystemPermissionCodes |
| 业务参数 | `system.params.sales.read` | 销售参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.sales.write` | 销售参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.purchase.read` | 采购参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.purchase.write` | 采购参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.finance.read` | 财务参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.finance.write` | 财务参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.logistics.read` | 物流参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.logistics.write` | 物流参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.customer.read` | 客户参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.customer.write` | 客户参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.product.read` | 产品参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.product.write` | 产品参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.compliance.read` | 合规参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.compliance.write` | 合规参数-维护 | SystemPermissionCodes |
| 业务参数 | `system.params.ai.read` | AI 参数-查看 | SystemPermissionCodes |
| 业务参数 | `system.params.ai.write` | AI 参数-维护 | SystemPermissionCodes |
| 日志导出 | `system.logs.export.read` | 导出日志-查看 | SystemPermissionCodes |
| 日志导出 | `system.logs.export.write` | 导出日志-维护 | SystemPermissionCodes |
| 登录日志 | `system.logs.login.read` | 登录日志-查看 | SystemPermissionCodes |
| 导入工具 | `system.tools.import.read` | 导入工具-查看 | SystemPermissionCodes |
| 导入工具 | `system.tools.import.write` | 导入工具-维护 | SystemPermissionCodes |
| 汇率工具 | `system.tools.exchange-rate.read` | 汇率-查看 | SystemPermissionCodes |
| 汇率工具 | `system.tools.exchange-rate.write` | 汇率-维护 | SystemPermissionCodes |
| 缓存工具 | `system.tools.clear-cache.write` | 清除缓存 | SystemPermissionCodes |
| 开发工具 | `system.dev.custom-sql.read` | 自定义 SQL-查看 | SystemPermissionCodes |
| 开发工具 | `system.dev.custom-sql.write` | 自定义 SQL-维护 | SystemPermissionCodes |
| 开发工具 | `system.dev.feature-flags.read` | 功能开关-查看 | SystemPermissionCodes |
| 开发工具 | `system.dev.feature-flags.write` | 功能开关-维护 | SystemPermissionCodes |
| 开发工具 | `system.dev.diagnostics.read` | 诊断-查看 | SystemPermissionCodes |
| 超级管理员 | `system.admin.super.read` | SuperAdmin-查看 | SystemPermissionCodes |
| 超级管理员 | `system.admin.super.write` | SuperAdmin-维护 | SystemPermissionCodes |

### 14.1.2 system.params 控制器级子权限清单

> 控制器实际校验的权限码（从各 *ParamsController、CompanyProfileController、DictionariesAdminController 提取）。子权限用于控制「页内具体 Tab / 按钮 / 字段」是否可读写；粗粒度 `system.params.{area}.read|write` 用于菜单显示。

| 业务域 | 子权限码（read） | 子权限码（write） | 对应控制器/操作 |
|--------|------------------|-------------------|------------------|
| 销售参数 | `system.params.sales.refresh-customer.read` | `system.params.sales.refresh-customer.write` | SalesParamsController.RefreshCustomer |
| 采购参数 | `system.params.purchase.assignee-count.read` | `system.params.purchase.assignee-count.write` | PurchaseParamsController 分配人数 |
| 采购参数 | `system.params.purchase.demand-protection.read` | `system.params.purchase.demand-protection.write` | PurchaseParamsController 需求保护 |
| 采购参数 | `system.params.purchase.default-assign-method.read` | `system.params.purchase.default-assign-method.write` | PurchaseParamsController 默认分配方式 |
| 采购成本参数 | `system.params.finance.purchase-cost-params.read` | `system.params.finance.purchase-cost-params.write` | PurchaseCostParamsController |
| 公司参数 | `system.params.company.read` | `system.params.company.write` | CompanyProfileController.Get/Update（read/write 任一即可查看） |
| 数据字典 | `system.params.dict.read` | `system.params.dict.write` | DictionariesAdminController |
| 报表参数 | `system.params.report.global.read` | `system.params.report.global.write` | ReportParamsController |

### 14.2 业务权限码通用验收用例

> 以下按「资源」分组，覆盖 `read` / `write` 两条基线。具体业务用例已在各业务权限文档中展开。

| 用例编号 | 资源 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|------|----------|----------|----------|----------|--------|
| PC-C-001 | 客户 | 有 customer.read | 用户含 `customer.read` | 1. 打开客户列表/详情<br>2. 直接调用 GET /api/customers | 200，数据正常 | P0 |
| PC-C-002 | 客户 | 无 customer.read | 用户不含 `customer.read` | 1. 查看侧栏<br>2. 直接调用 GET /api/customers | 菜单隐藏；返回 403 | P0 |
| PC-C-003 | 客户 | 有 read 无 write | 用户含 `customer.read` 不含 `customer.write` | 1. 打开客户详情<br>2. 点击保存/新建 | 编辑按钮隐藏；保存 403 | P0 |
| PC-C-004 | 客户 | 无 customer.info.read | 用户不含 `customer.info.read` | 1. 打开客户详情 | 敏感字段（如联系方式）脱敏或隐藏 | P1 |
| PC-V-001 | 供应商 | 有 vendor.read | 用户含 `vendor.read` | 打开供应商列表/详情 | 200 | P0 |
| PC-V-002 | 供应商 | 无 vendor.read | 用户不含 `vendor.read` | 打开供应商列表/详情 | 菜单隐藏；403 | P0 |
| PC-V-003 | 供应商 | 有 read 无 write | 用户含 `vendor.read` 不含 `vendor.write` | 编辑供应商 | 保存 403 | P0 |
| PC-SO-001 | 销售订单 | 有 sales-order.read | 用户含 `sales-order.read` | 打开销售订单列表 | 200 | P0 |
| PC-SO-002 | 销售订单 | 无 sales-order.read | 用户不含 `sales-order.read` | 打开销售订单列表 | 403 | P0 |
| PC-SO-003 | 销售订单 | 有 sales-order.write 无 read | 用户含 `sales-order.write` | 打开销售订单列表 | 403（write 不隐式包含 read） | P0 |
| PC-PO-001 | 采购订单 | 有 purchase-order.read | 用户含 `purchase-order.read` | 打开采购订单列表 | 200 | P0 |
| PC-PO-002 | 采购订单 | 无 purchase-order.read | 用户不含 `purchase-order.read` | 打开采购订单列表 | 403 | P0 |
| PC-PR-001 | 采购申请 | 有 purchase-requisition.read | 用户含 `purchase-requisition.read` | 打开采购申请列表 | 200 | P0 |
| PC-PR-002 | 采购申请 | 无 purchase-requisition.read | 用户不含 | 打开列表 | 403 | P0 |
| PC-RFQ-001 | 询价 | 有 rfq.read | 用户含 `rfq.read` | 打开 RFQ 列表 | 200 | P0 |
| PC-RFQ-002 | 询价 | 有 rfq.read 无 rfq.create | 采购部员工 | 点「新建需求」 | 按钮隐藏；POST 403 | P0 |
| PC-RFQ-003 | 询价 | 有 rfq.create | 销售员 | 点「新建需求」 | 可正常进入 | P0 |
| PC-DRAFT-001 | 草稿 | 有 draft.read | 用户含 `draft.read` | 打开草稿箱 | 200 | P1 |
| PC-DRAFT-002 | 草稿 | 无 draft.read | 商务部用户 | 打开草稿箱 | 403（商务部默认剥离 draft.read） | P1 |
| PC-BRAND-001 | 品牌 | 有 biz-brand.read | 用户含 `biz-brand.read` | 打开品牌列表 | 200 | P1 |
| PC-BRAND-002 | 品牌 | 无 biz-brand.read | 用户不含 | 打开品牌列表 | 403 | P1 |
| PC-AI-C-001 | AI 客户情报 | 有 biz.ai.customer_intel.lookup | 用户含该码 | 调用 AI 客户情报 | 200 | P1 |
| PC-AI-C-002 | AI 客户情报 | 仅有 customer.read | 用户含 `customer.read` | 调用 AI 客户情报 | 200（运行时合并：有 customer.read 自动补该码） | P1 |
| PC-AI-M-001 | AI 物料情报 | 有 biz.ai.material_intel.lookup | 用户含该码 | 调用 AI 物料情报 | 200 | P1 |
| PC-AI-M-002 | AI 物料情报 | 仅有 rfq.read | 用户含 `rfq.read` | 调用 AI 物料情报 | 200（运行时合并：有 rfq.read 自动补该码） | P1 |
| PC-FIN-001 | 财务付款 | 有 finance-payment.read | 用户含 | 打开付款单列表 | 200 | P0 |
| PC-FIN-002 | 财务付款 | 无 finance-payment.read | 用户不含 | 打开付款单列表 | 403 | P0 |
| PC-FIN-003 | 财务付款 | 有 read 无 write | 用户含 read 不含 write | 保存付款执行 | 403 | P0 |
| PC-FIN-004 | 财务收款 | 有 finance-receipt.read | 用户含 | 打开收款单列表 | 200 | P0 |
| PC-FIN-005 | 财务发票 | 有 finance-sell-invoice.read | 用户含 | 打开销项发票列表 | 200 | P0 |
| PC-FIN-006 | 财务发票 | 有 finance-purchase-invoice.read | 用户含 | 打开进项发票列表 | 200 | P0 |

### 14.3 系统权限码通用验收用例

| 用例编号 | 资源 | 用例标题 | 前置条件 | 测试步骤 | 预期结果 | 优先级 |
|----------|------|----------|----------|----------|----------|--------|
| PC-SYS-001 | system.* | 普通员工持 system.* | 用户有 `system.org.users.read`，无管理角色 | 1. 查看 permission-summary<br>2. 打开员工管理 | permission-summary 中无该码；菜单隐藏；直接访问 403 | P0 |
| PC-SYS-002 | system.* | SYS_ADMIN 持 system.* | SYS_ADMIN 有 `system.org.users.read` | 打开员工管理 | 可访问 | P0 |
| PC-SYS-003 | system.* | SYS_MANAGER 持 system.* | SYS_MANAGER 有 `system.org.users.read` | 打开员工管理 | 可访问 | P0 |
| PC-SYS-004 | system.* | SYS_BIZ_MANAGER 无 system.* | SYS_BIZ_MANAGER 无 `system.org.users.read` | 打开员工管理 | 403 | P0 |
| PC-SYS-005 | rbac.manage | 持有 rbac.manage 访问 system.* | 用户有 `rbac.manage` | 调用任一 system.* 接口 | 200（兼容迁移期） | P1 |
| PC-SYS-006 | system.org.users.resetPassword | 无该码重置密码 | 用户有 write 无 resetPassword | 调用重置密码 | 403 | P1 |
| PC-SYS-007 | system.params.sales | 子权限隔离 | 用户仅有 `system.params.sales.read` | 打开采购参数页 | 403 | P1 |
| PC-SYS-008 | system.dev.diagnostics | 无 SuperAdmin 不能访问 | 用户是 SYS_MANAGER 无 `system.admin.super.read` | 打开 SuperAdmin 页面 | 404（设计如此） | P0 |
| PC-COMPANY-001 | system.params.company | 读/写任一可查看 | 用户仅有 `system.params.company.read` 或仅有 `system.params.company.write` | GET /api/company-profile | 200 | P1 |
| PC-COMPANY-002 | system.params.company | 无任一权限 | 用户两个码都没有 | GET /api/company-profile | 403 | P1 |

---

## 十五、代码实现核对记录

> 核对代码位置：`CRM.Core/Services/RbacService.cs`、`CRM.Core/Services/DataPermissionService.cs`、`CRM.API/Authorization/RequirePermissionAttribute.cs`、`CRM.Core/Constants/SystemPermissionCodes.cs`、`CRM.Core/Constants/ManagementRoleCodes.cs`。

### 15.1 RbacService 核对

| 核对项 | 代码事实 | 用例覆盖 |
|--------|----------|----------|
| 主部门决定身份 | `primaryDepartmentId = IsPrimary=true` 的部门；默认 `SaleDataScope=1, PurchaseDataScope=1` | RBAC-002、SCOPE-* |
| IdentityType 兜底 | IdentityType=0 时按部门名称含「财务/Finance/Accounting」推断为 5 | ID-003 |
| 采购侧识别 | 主部门 2/3，或兼任部门 2/3（且主部门不是销售），或部门名称含「采购部/采购中心/Purchasing」 | ID-007、ID-008 |
| 采购侧剥离 | 剥离 `sales-order.*`、`sales.amount.read`、`finance-receipt.*`、`finance-sell-invoice.*`、`rfq.create` | ID-002、PC-RFQ-002 |
| 销售侧剥离 | 剥离 `finance-payment.*`、`finance-purchase-invoice.*`、`purchase-order.*`、`purchase.amount.read` | ID-001 |
| 商务部剥离 | 剥离 `vendor.*`、`purchase-order.read`、`purchase.amount.read`、`draft.read`、`finance-purchase-invoice.read` | ID-004 |
| 销售侧补全 | 补 `customer.*`、`sales-order.*`、`rfq.read/write/create` | ID-001 |
| 采购侧补全 | 补 `rfq.read/write`、`vendor.*`、`purchase-requisition.*`、`purchase-order.*`、`purchase.amount.read` | ID-002 |
| 商务部补全 | 补 `customer.*`、`sales-order.*`、`rfq.read/write/create` | ID-004 |
| 财务部补全 | 补 8 个 `finance-*` read/write | ID-003 |
| 只读剥离 | `SaleDataAccess=1` 时剥离销售侧 write；`PurchaseDataAccess=1` 时剥离采购侧 write；`FinanceDataAccess=1` 时剥离财务侧 write | ACCESS-* |
| 隐藏客户/供应商 | `HideCustomerManagement`/`HideVendorManagement` 时剥离对应读写；财务部保留 read | ID-005、ID-006 |
| AI 权限运行时合并 | 有 `rfq.read` → `biz.ai.material_intel.lookup`；有 `customer.read` → `biz.ai.customer_intel.lookup` | PC-AI-C-002、PC-AI-M-002 |
| 管理角色 bypass | `hasBizDataBypass = hasManagementAccess` | MGT-001~003 |
| 系统权限双重门槛 | 无管理角色时剥离全部 `system.*`；管理角色只保留其管理角色绑定的 system 菜单码 | MGT-005、MGT-006 |
| rbac.manage 兼容 | `RequirePermissionAttribute` 中对 system.* 做 legacy `rbac.manage` 放行 | PC-SYS-005 |

### 15.2 DataPermissionService 核对

| 核对项 | 代码事实 | 用例覆盖 |
|--------|----------|----------|
| FilterCustomersAsync | 管理身份/财务身份/SaleDataScope=0 时全量；SaleDataScope=1 时按 SalesUserId/CreateByUserId；2/3 时按部门用户集合 | DPS-C-001~003 |
| FilterVendorsAsync | 管理身份/财务身份/PurchaseDataScope=0/1/2/3 时全量；PurchaseDataScope=4 时返回空 | DPS-V-001~002 |
| FilterSalesOrdersAsync | 按 `SalesUserId`；支持 Assistor 身份；SaleDataScope=4 时返回空 | DPS-C-002 |
| FilterPurchaseOrdersAsync | 按 `PurchaseUserId` 或 `Assistor`；PurchaseDataScope=4 时返回空 | DPS-V-003 |
| 部门及下级 | `GetAllowedUserIdsAsync` 用 `department.Path` 前缀匹配子部门 | SCOPE-004、SCOPE-009 |
| 组织角色级别 | `ResolveOrgRoleLevel` 影响「本部门」范围是否可看全部门员工；总监级不扩大，经理/员工按实际 | SCOPE-003、SCOPE-008 |
| RFQ/Quote 范围 | 同时考虑 SaleDataScope 和 PurchaseDataScope；销售侧按 SalesUserId，采购侧按 AssignedPurchaserUserId1/2；**HasBizDataBypass 时主表、明细作业页、报价列表看板并联需求均不裁行** | DPS-RFQ-001、DPS-RFQ-002、DPS-QUOTE-001 |
| 采购执行链路 | 采购到货/质检/入库/库存按关联的 PO 范围过滤 | DPS-L-001 |
| 财务单据 | 收款/付款/发票在财务身份或 HasBizDataBypass 时不按业务员/采购员缩小 | DPS-C-003、DPS-V-005 |
| 销售敏感字段 | `SaleSensitiveFieldMask521` 对 11 类字段名做前缀/包含脱敏 | MASK-*、§6.2 |
| 采购敏感字段 | `PurchaseSensitiveFieldMask511` 对 11 类字段名做前缀/包含脱敏 | MASK-*、§6.3 |
| 跨业务线附件 | `CrossSideDocumentAttachmentPolicy.ShouldDeny` 在 `IdentityType` 与 `BizType` 互斥且对侧 Scope=4 时返回 true | ATT-001~005 |

### 15.3 RequirePermissionAttribute 核对

| 核对项 | 代码事实 | 用例覆盖 |
|--------|----------|----------|
| 基本校验 | 取 JWT 中 NameIdentifier → 调用 RbacService 获取 Summary；未登录 401 | ROUTE-* |
| SYS_ADMIN 全放行 | `summary.IsSysAdmin` 直接 return | MGT-001 |
| 业务码 bypass | 非 system.* 且 `HasBizDataBypass` 直接 return | MGT-002、MGT-003 |
| 系统码双重门槛 | system.* 需 `HasManagementAccess` + 具体码；否则 403 | MGT-005、MGT-006 |
| 多属性 AND | `AllowMultiple=true`；多个 `[RequirePermission]` 均需各自通过 | PC-SYS-007 |
| 大小写不敏感 | 比较使用 `OrdinalIgnoreCase` | — |
| rbac.manage 兼容 | system.* 无码时，若持有 `rbac.manage` 仍放行 | PC-SYS-005 |
| 异常处理 | 服务异常返回 500「权限校验失败」 | — |

### 15.3.2 RequireAnyPermissionAttribute 核对

| 核对项 | 代码事实 | 用例覆盖 |
|--------|----------|----------|
| 语义 | 多个权限码满足其一即可（OR） | PC-COMPANY-001 |
| SYS_ADMIN 放行 | 同 RequirePermission | MGT-001 |
| HasBizDataBypass 放行 | 同 RequirePermission，用于业务码组合 | MGT-002、MGT-003 |
| 无任一码 | 返回 403，提示所需权限 | PC-COMPANY-002 |
| 典型应用 | CompanyProfileController.Get 需要 `system.params.company.read` 或 `system.params.company.write` | PC-COMPANY-001 |

### 15.4 ManagementRoleCodes 核对

| 核对项 | 代码事实 | 用例覆盖 |
|--------|----------|----------|
| SuperAdmin | `SYS_ADMIN` | MGT-001 |
| Admin/Manager | `SYS_MANAGER`；`IsAdminRole()` 包含 SYS_ADMIN/SYS_MANAGER | MGT-002 |
| BizManager | `SYS_BIZ_MANAGER`；业务数据 bypass | MGT-003 |
| 域级经理 | `SYS_MGR_SALES`、`SYS_MGR_PURCHASE`、`SYS_MGR_FINANCE`、`SYS_MGR_LOGISTICS`、`SYS_MGR_CUSTOMER`、`SYS_MGR_PRODUCT`、`SYS_MGR_AI`、`SYS_MGR_COMPLIANCE` | MGT-004 |
| HasBizDataBypass | 仅前三种管理角色为 true | MGT-004 |

---

## 十六、待补充 / 待人工确认项

| 编号 | 待确认项 | 现状 | 建议处理方式 |
|------|----------|------|--------------|
| TODO-01 | `rfq.create` 在采购侧被剥离 | 代码：`RbacService.StaffPurchasePermissions()` 未包含 `rfq.create`；`StaffSalesPermissions()` 包含。 | **需产品确认**：采购部员工是否应能新建询价/需求？若需要，代码需补 `rfq.create`；若不需要，保持现状并在用例 PC-RFQ-002 中明确。 |
| TODO-02 | `sales.amount.read` / `purchase.amount.read` 具体字段清单 | 已核对：`SaleSensitiveFieldMask521` / `PurchaseSensitiveFieldMask511` 给出字段名模板，但 UI 实际字段名可能不完全一致。 | 前端走查：用实际接口返回值与敏感字段模板比对，补充/修正 §6.2、§6.3 字段示例。 |
| TODO-03 | `system.params.*` 各子菜单对应的路由/API | 已提取控制器级子权限（§14.1.2），但部分子模块（`system.params.logistics.*`、`system.params.customer.*`、`system.params.product.*`、`system.params.compliance.*`、`system.params.ai.*`）未找到对应控制器。 | 确认这些菜单是否仅前端展示，或后端接口尚未实现；若已分散到其他控制器，补充到 §14.1.2。 |
| TODO-04 | 审批桌面自审规则 | 已核对代码：`SalesDirectorSelfApprovalRules` / `PurchaseDirectorSelfApprovalRules` 规则明确，§8 已更新。 | 无需再确认，按 §8 执行测试。 |
| TODO-05 | 列级脱敏附件禁止访问 | 已核对：`CrossSideDocumentAttachmentPolicy` + `DocumentsController` 统一校验，§6.4 已补充用例。 | 无需再确认，按 §6.4 执行测试。 |
