# RBAC权限系统产品需求文档（PRD）

**文档版本：** v1.8（平台管理员业务数据全量 bypass 与专篇 PRD 对齐）
**编写日期：** 2026-04-08
**项目名称：** AI智销系统（FrontCRM_CSharp）
**技术栈：** .NET 9.0 + Vue 3 + TypeScript + PostgreSQL
**适用对象：** 产品经理、系统架构师、前后端开发工程师、测试工程师

---

## 一、模块概述

### 1.1 背景与目标

**问题描述：**
当前企业业务中存在销售与采购两条独立业务线，需要实现：
1. 销售人员和采购人员数据隔离，防止业务信息泄露
2. 不同组织层级（总监、经理、员工）的数据可见性控制
3. 灵活的功能权限分配机制
4. 基于部门架构的业务身份管理

**功能目标：**
- 建立**部门驱动**的权限模型，部门决定用户的业务身份和数据范围
- 实现**角色分层**的组织权限控制，支持上下级数据可见性
- 提供**精细化**的功能权限控制，支持API、菜单、按钮级权限
- 确保**销售与采购数据双向隔离**，保障业务数据安全

### 1.2 核心设计理念

本系统采用 **"部门驱动 + 角色分层 + 数据隔离"** 的三维权限模型：

```
用户 (User)
  ├── 关联部门 (Department) ──决定──► 业务身份类型 (IdentityType)
  │       │                           │
  │       ├── 销售数据范围 (SaleDataScope)      │
  │       │                           │
  │       └── 采购数据范围 (PurchaseDataScope)  │
  │                                           │
  └── 关联角色 (Role) ──继承──► 权限 (Permission)
          │
          └── 组织层级 (总监/经理/员工)
```

### 1.3 模块范围

- **部门管理**：部门创建、编辑、层级关系维护
- **角色管理**：角色定义、权限分配
- **用户权限管理**：用户部门分配、角色分配
- **权限验证**：API权限验证、数据权限过滤、组织权限控制
- **权限查询**：用户权限汇总、数据范围查询

**管理角色分级（Admin / Manager / DepartManager）、`system.*` 与 `biz.*` 权限分层、系统管理双重门槛、品牌 options 与主数据拆分、角色编辑 UI 按域批量配置（方式 C）** 见专篇：[管理角色分级与权限体系PRD](./管理角色分级与权限体系PRD.md)（v1.0，2026-06-03 定稿）。

---

## 二、核心概念与关系模型

### 2.1 业务身份类型 (IdentityType)

| 身份类型 | 值 | 业务方向 | 数据权限特点 |
|----------|----|----------|--------------|
| **SalesMan** | 1 | 销售方向 | 只能访问销售相关数据（客户、销售订单、收款等） |
| **Purchaser** | 2 | 采购方向 | 只能访问采购相关数据（供应商、采购订单、付款等） |
| **PurchaserAssistant** | 3 | 采购助理 | 协助采购员处理采购事务 |
| **CustomerService** | 4 | 客服 | 客户服务支持 |
| **Finance** | 5 | 财务 | 可查看销售和采购两边财务数据 |
| **Logistics** | 6 | 物流 | 可查看销售和采购两边物流数据 |

### 2.2 数据范围 (DataScope)

| 数据范围值 | 含义 | SQL过滤逻辑 |
|------------|------|-------------|
| **0 (全部)** | 可查看全部数据 | 不过滤 |
| **1 (自己)** | 只能查看自己创建的数据 | `WHERE CreatorId = CurrentUserId` |
| **2 (本部门)** | 可查看本部门同事的数据 | `WHERE CreatorDepartmentId = CurrentUserDepartmentId` |
| **3 (本部门及下级)** | 可查看本部门及下级部门的数据 | `WHERE CreatorDepartmentPath LIKE CurrentUserDepartmentPath + '%'` |
| **4 (禁止)** | 无权限查看 | `WHERE 1=0` |

### 2.3 组织层级 (Organization Level)

| 角色编码 | 层级值 | 数据可见范围 |
|----------|--------|--------------|
| **DEPT_DIRECTOR** | 3 | 可查看下属经理和员工的数据 |
| **DEPT_MANAGER** | 2 | 可查看下属员工的数据 |
| **DEPT_EMPLOYEE** | 1 | 仅能查看自己的数据 |

**层级规则**：
- 总监(3) > 经理(2) > 员工(1)
- 上级可查看下级数据，同级和下级不能查看上级数据
- 待审批自审与数据范围正交：仅 **部门总监** 叠加主部门销售/采购身份后，可审本人提交的对应单据（客户/销售订单 或 供应商/采购订单）。经理、员工不能自审。见 [审批桌面-设计与实现](../System/审批/审批桌面-设计与实现.md) §4.1。

### 2.4 管理角色（与组织角色正交）

与 §2.3 **组织角色**（`DEPT_*`）独立，用于 **系统管理菜单与平台能力**。L1/L2（`SYS_ADMIN` / `SYS_MANAGER`）置 `HasBizDataBypass`，业务单据不受主部门 `DataScope` / `IdentityType` 藏菜单；L3 `SYS_MGR_*` **不**改变业务单据数据范围。完整矩阵见 [管理角色分级与权限体系PRD §三](./管理角色分级与权限体系PRD.md#三管理角色分级)。

| RoleCode | 中文名 | 摘要 |
|----------|--------|------|
| `SYS_ADMIN` | 系统管理员 | 最高权限；`IsSysAdmin=true`；`HasBizDataBypass=true` |
| `SYS_MANAGER` | 平台管理员 | 含日志/公司信息/模拟登录/强删、业务数据全量 bypass；**不含** 角色/权限/Debug/Admin 账号 |
| `SYS_MGR_HR` | 人事系统管理员 | 员工+部门；可创建 Manager |
| `SYS_MGR_SALES` / `PURCHASE` / `LOGISTICS` / `FINANCE` | 域系统管理员 | 本域 `system.params.*` + 可手工配本域主数据菜单；**不管** 业务单据数据范围 |

---

## 三、实体定义

### 3.1 部门实体 (RbacDepartment)

| 字段 | 类型 | 必填 | 说明 | 关键值 |
|------|------|------|------|--------|
| `Id` | `Guid` | 是 | 部门ID | 主键 |
| `DepartmentCode` | `string` | 是 | 部门编码 | 唯一 |
| `DepartmentName` | `string` | 是 | 部门名称 | |
| `ParentId` | `Guid?` | 否 | 父部门ID | |
| `Path` | `string` | 是 | 部门路径 | 如 `"Root/销售部/销售一部"` |
| `Level` | `int` | 是 | 部门层级 | 从1开始 |
| `IdentityType` | `short` | 是 | **业务身份类型** | `1=业务员`, `2=采购员`, `3=采购助理`, `4=客服`, `5=财务`, `6=物流` |
| `SaleDataScope` | `short` | 是 | **销售数据权限范围** | `0=全部`, `1=自己`, `2=本部门`, `3=本部门及下级`, `4=禁止` |
| `PurchaseDataScope` | `short` | 是 | **采购数据权限范围** | `0=全部`, `1=自己`, `2=本部门`, `3=本部门及下级`, `4=禁止` |
| `Status` | `short` | 是 | 状态 | `1=启用`, `0=禁用` |
| `CreatedAt` | `DateTime` | 是 | 创建时间 | |
| `UpdatedAt` | `DateTime` | 是 | 更新时间 | |

### 3.2 角色实体 (RbacRole)

| 字段 | 类型 | 必填 | 说明 | 标准编码 |
|------|------|------|------|----------|
| `Id` | `Guid` | 是 | 角色ID | 主键 |
| `RoleCode` | `string` | 是 | **角色编码** | **组织角色**：`DEPT_DIRECTOR`, `DEPT_MANAGER`, `DEPT_EMPLOYEE`；**管理角色**：`SYS_ADMIN`, `SYS_MANAGER`, `SYS_MGR_*`（见 [管理角色分级 PRD §3.1](./管理角色分级与权限体系PRD.md#31-角色定义)） |
| `RoleName` | `string` | 是 | 角色名称 | 如 "部门总监", "平台管理员", "采购系统管理员" |
| `Description` | `string` | 否 | 角色描述 | |
| `Status` | `short` | 是 | 状态 | `1=启用`, `0=禁用` |
| `CreatedAt` | `DateTime` | 是 | 创建时间 | |
| `UpdatedAt` | `DateTime` | 是 | 更新时间 | |

### 3.3 权限实体 (RbacPermission)

| 字段 | 类型 | 必填 | 说明 | 示例 |
|------|------|------|------|------|
| `Id` | `Guid` | 是 | 权限ID | 主键 |
| `PermissionCode` | `string` | 是 | 权限编码 | 业务：`customer.read`；系统：`system.org.users.read`；主数据：`biz.brand.read`（见 §12.1 与 [管理角色分级 PRD §四–§六](./管理角色分级与权限体系PRD.md)） |
| `PermissionName` | `string` | 是 | 权限名称 | 如 "员工-查看", "品牌-查看" |
| `PermissionType` | `string` | 是 | 权限类型 | `"menu"`(菜单), `"api"`(接口), `"button"`(按钮), `"data"`(数据) |
| `Resource` | `string` | 是 | 资源标识 | 如 `"customer"`, `"system.org.users"`, `"biz.brand"` |
| `Action` | `string` | 是 | 操作类型 | 如 `"read"`, `"write"`, `"impersonate"` |
| `Description` | `string` | 否 | 权限描述 | 可承载扩展 JSON；**规划字段** `DomainTag` / `MenuGroup` / `PermissionKind` 见 [管理角色分级 PRD §8.2](./管理角色分级与权限体系PRD.md#82-权限元数据sys_permission-扩展) |
| `Status` | `short` | 是 | 状态 | `1=启用`, `0=禁用` |

### 3.4 关联关系实体

#### 3.4.1 用户-部门关系 (RbacUserDepartment)
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `UserId` | `Guid` | 是 | 用户ID |
| `DepartmentId` | `Guid` | 是 | 部门ID |
| `IsPrimary` | `bool` | 是 | 是否主部门 |

#### 3.4.2 用户-角色关系 (RbacUserRole)
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `UserId` | `Guid` | 是 | 用户ID |
| `RoleId` | `Guid` | 是 | 角色ID |

#### 3.4.3 角色-权限关系 (RbacRolePermission)
| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `RoleId` | `Guid` | 是 | 角色ID |
| `PermissionId` | `Guid` | 是 | 权限ID |

---

## 四、权限计算逻辑

### 4.1 用户权限汇总流程

```csharp
// 伪代码：GetUserPermissionSummaryAsync
1. 获取用户所有角色 → RoleCode列表
2. 通过角色获取所有权限 → PermissionCode列表  
3. 获取用户所有部门 → DepartmentId列表
4. 确定主部门 → 从主部门获取IdentityType、SaleDataScope、PurchaseDataScope
5. 判断是否为系统管理员 → 检查是否包含"SYS_ADMIN"角色
6. 计算组织层级 → 通过最高角色编码确定层级值
7. **（规划）** 计算 `hasManagementAccess` → `SYS_ADMIN` \| `SYS_MANAGER` \| 任一 `SYS_MGR_*`；非管理身份汇总时 **剥离** 全部 `system.*` 权限码（见 [管理角色分级 PRD §五](./管理角色分级与权限体系PRD.md#五系统管理双重门槛)）
```

### 4.1.1 功能权限与系统管理门禁（规划）

| 规则 | 说明 |
|------|------|
| 业务 `*.read` | 仅控制业务菜单/API；**不能** 打开系统管理 |
| `system.*` | 须 **权限码 + `hasManagementAccess`**；普通员工误配仍 403 / 无菜单 |
| `biz.*.options` | 表单下拉；**不** 等同主数据 `read`（品牌见专篇 §4.2） |
| `rbac.manage` | 兼容超集别名；新功能改用 `system.*` |

### 4.2 数据权限过滤服务 (DataPermissionService)

**核心方法：**
1. `FilterCustomersAsync(userId, customers)` - 过滤客户数据
2. `FilterVendorsAsync(userId, vendors)` - **供应商主数据列表**：仅当部门 **`PurchaseDataScope == 4`** 时返回空；否则返回全部入参（不按 `VendorInfo.PurchaseUserId` 缩小，便于报价选商；专属供应商待模型字段）。采购订单、对供应商付款/进项仍按 `PurchaseUserId` + 采购范围，见同服务内 `FilterPurchaseOrdersAsync`、`FilterFinancePaymentsAsync` 等。
3. `FilterSalesOrdersAsync(userId, orders)` - 过滤销售订单
4. `FilterPurchaseOrdersAsync(userId, orders)` - 过滤采购订单
5. **`ApplyPurchaseOrderDataScopeAsync(userId, query)`** - 将采购数据范围套用到可翻译的 `IQueryable<PurchaseOrder>`；采购执行链路列表经关联采购单复用同一口径（见 **§4.2.1**）。

**业务员—客户与采购员—供应商对照（与实现同步）**：见 `document/System/权限/数据权限-业务员客户与采购员供应商.md`。

**列级补充：** 销售方向且 **`PurchaseDataScope == 4`** 时，除上述服务方法外，须在 UI/API 上对采购敏感列脱敏，字段清单见 **§5.1.1**；采购方向且 **`SaleDataScope == 4`** 时，须在 UI/API 上对销售敏感列脱敏，字段清单见 **§5.2.1**。**附件补充：** 与 **§5.6** 一致，对跨业务线「上传单据」接口与界面做禁止访问/隐藏，与列级规则使用同一套用户摘要判定。

#### 4.2.1 采购执行链路列表（`PurchaseDataScope` 经采购单关联）

以下列表在服务端注入当前用户 Id 后，通过 **`PurchaseOrder.PurchaseUserId` / `Assistor`** 套用 **`ApplyPurchaseOrderDataScopeAsync`**（与采购订单列表规则一致；系统管理员或 **`PurchaseDataScope == 0`** 不缩小）：

| 前端页面 | API | 实现类 | 关联路径 |
|---------|-----|--------|---------|
| 到货通知 | `GET /api/v1/logistics/arrival-notices` | `ArrivalNoticeListQuery` | `stockin_notify.PurchaseOrderId` → `purchaseorder` |
| 质检 | `GET /api/v1/logistics/qcs` | `QcListQuery` | `qcinfo.StockInNotifyId` → `stockin_notify` → 采购单 |
| 入库批次记录 | `GET /api/v1/stock-in/batches` | `StockInBatchListQuery` | `stock_in_batch` → 入库明细扩展 → 采购明细 → 采购单 |
| 批次对账 | `GET /api/v1/batch-reconciliation`（含导出、消耗明细） | `BatchReconciliationListQuery` | 入库批次 join 采购单 |

**说明：** 无法关联到可见采购单的行（如缺少采购明细扩展）在 **`PurchaseDataScope` 为 1/2/3/4** 时不展示；仅 **`0`（全部）** 或系统管理员可见全量。

#### 4.2.2 库存与入库（销售 ∨ 采购 ∨ 物流 OR）

以下列表采用 **多数据范围 OR**：满足 **销售**、**采购** 或 **物流** 任一可见条件即展示（系统管理员不缩小；对应范围 **`0`（全部）** 视为该维度开放）。

| 前端页面 | API | 实现方法 | 销售路径 | 采购路径 | 物流路径 |
|---------|-----|---------|---------|---------|---------|
| 入库单列表 | `GET /api/v1/stock-in` | `ApplyStockInListDataScopeAsync` | 明细扩展 → 销售明细 → 销售单 | 明细扩展 → 采购明细 → 采购单 | `CreateByUserId` / `CreatedBy` |
| 库存明细列表 | `GET /api/v1/inventory-center/stock-items` | `ApplyStockItemListDataScopeAsync` | `SalespersonId` / 销售明细 / 客户 | `PurchaserId` | — |
| 库存总览（分页） | `GET /api/v1/inventory-center/overview/paged` | `ApplyStockAggregateListDataScopeAsync` | 至少一条明细命中销售范围 | 至少一条明细命中采购范围 | — |
| 采购申请 | `GET /api/v1/purchase-requisitions` | `ApplyPurchaseRequisitionListDataScopeAsync` | 关联销售单在销售范围 | `PurchaseUserId` 在采购范围 | — |
| 盘点计划 | `GET /api/v1/inventory-center/count-plans` | `ApplyLogisticsCreatorUserScopeAsync` | — | — | `CreatorId` |
| 分桶下钻明细 | `GET /api/v1/inventory-center/stocks/{id}/stock-items` | `IStockInListQuery.IsVisibleToUserAsync` | 同入库单 | 同入库单 | 同入库单 |
| 物料入库追溯 | `GET /api/v1/inventory-center/materials/{id}/traces` | `IStockInListQuery.IsVisibleToUserAsync` | 同入库单 | 同入库单 | 同入库单 |

**说明：** 三维度均为 **`4`（禁止）** 时列表为空；仅某一维度禁止时，仍可通过其他维度 OR 命中。入库单不再对销售范围与物流创建人做 **AND** 叠加。

**过滤逻辑：**
```sql
-- 示例：销售订单过滤
SELECT * FROM sales_orders
WHERE 
  -- 业务身份过滤（销售方向只能看销售数据）
  (CurrentUserIdentityType = 1 OR CurrentUserIdentityType IN (5,6))
  AND
  -- 数据范围过滤
  (
    (CurrentUserSaleDataScope = 0) -- 全部
    OR (CurrentUserSaleDataScope = 1 AND SalesUserId = CurrentUserId) -- 自己
    OR (CurrentUserSaleDataScope = 2 AND SalesDepartmentId = CurrentUserDepartmentId) -- 本部门
    OR (CurrentUserSaleDataScope = 3 AND SalesDepartmentPath LIKE CurrentUserDepartmentPath + '%') -- 本部门及下级
  )
  AND
  -- 组织层级过滤
  SalesUserId IN (SELECT UserId FROM GetVisibleUserIds(CurrentUserId, CurrentUserRoleLevel))
```

### 4.3 组织层级可见用户计算

**函数：** `GetVisibleUserIds(userId, roleLevel)`

| 用户角色层级 | 可见用户范围 |
|--------------|--------------|
| **总监 (3)** | 下属所有经理和员工 |
| **经理 (2)** | 下属所有员工 |
| **员工 (1)** | 仅自己 |

---

## 五、业务场景权限示例

### 5.1 业务员（销售方向）

**部门配置：**
```json
{
  "IdentityType": 1,          // SalesMan
  "SaleDataScope": 1,         // 只能查看自己的销售数据
  "PurchaseDataScope": 4,     // 禁止查看采购数据
  "DepartmentName": "销售一部",
  "Path": "Root/销售部/销售一部"
}
```

**角色分配：** `DEPT_EMPLOYEE` (员工层级)

**权限效果：**
- ✅ 可查看自己创建的客户
- ✅ 可查看自己创建的销售订单
- ✅ 可查看自己负责的收款记录
- ❌ 不可查看同事的客户数据
- ❌ 不可查看采购相关数据
- ❌ 不可查看财务数据（除非财务部门）

#### 5.1.1 销售方向：采购数据「禁止」时的列级 / 字段级不可见范围（补充）

**适用条件（与 §5.1 典型配置一致，以实现为准）：**

- 用户主部门（权限摘要）**`PurchaseDataScope == 4`（禁止查看采购数据）**；
- 且 **`IdentityType` 为销售方向**（典型为业务员 **`1`**；客服/物流/财务等身份见各自章节，不在此表默认套用）；
- **系统管理员**（`SYS_ADMIN` / `IsSysAdmin`）**不适用**本表脱敏，仍按管理员规则全量可见。

**说明：** 下列字段指**任意界面**（含跨模块联查、入库/出库/质检/审批详情等）在同时满足上述条件时应**不展示或以占位符替代**（如 `—` / `***`），避免通过销售可见页面**间接泄露采购价格与供应商深度档案**。与 `FilterVendorsAsync` 等**行级**过滤互补：行级无权限时列表为空；行级因业务需要仍展示少量采购关联信息时，须遵守本表**列级**约束。

| 序号 | 业务字段名（中文） | 含义与典型数据来源（API/表字段以实现为准） |
|------|-------------------|---------------------------------------------|
| 1 | **供应商英文名** | 供应商主数据英文名称（如 `VendorInfo` 英文名字段、双语档案中的 EN 分量）。 |
| 2 | **供应商英中文名** | 英中文对照展示名（如「EN / 中文」拼接字段或前端组合展示列）。 |
| 3 | **供应商全称** | 供应商法定/工商**全称**（如 `officialName`、企业全称；不含仅内部简称时可单独约定是否脱敏简称）。 |
| 4 | **采购单价** | 采购订单明细**未税/含税单价**（如采购行 `cost`、单价类展示列）。 |
| 5 | **采购总额** | 采购侧**金额合计**（单行 `qty × cost`、订单头采购总金额、入库单上回显的采购金额等）。 |
| 6 | **采购折算美元单价** | 采购行**折合 USD 的单价**（如 `convertPrice` / 折算美金单价）。 |
| 7 | **采购折算美元总额** | 折合 USD 的**行金额或订单级折算总额**（由折算单价与数量派生或独立存储字段）。 |

**实现提示（非规范性代码）：** 前端可基于 `GET /api/v1/auth/permission-summary` 中的 **`purchaseDataScope`**、**`identityType`**、**`isSysAdmin`** 做列显隐；敏感列建议统一走占位组件，避免漏网。后端对导出、打印、聚合接口亦应按同一口径脱敏或拒绝字段。

### 5.2 采购经理（采购方向）

**部门配置：**
```json
{
  "IdentityType": 2,          // Purchaser
  "SaleDataScope": 4,         // 禁止查看销售数据
  "PurchaseDataScope": 0,     // 可查看全部采购数据
  "DepartmentName": "采购部",
  "Path": "Root/采购部"
}
```

**角色分配：** `DEPT_MANAGER` (经理层级)

**权限效果：**
- ✅ 供应商主数据列表：`PurchaseDataScope ≠ 4` 时可浏览**全部**供应商（`FilterVendorsAsync` 不按责任采购员缩小；报价等任选）
- ✅ 可查看本部门所有采购订单（`FilterPurchaseOrdersAsync` 仍按 `PurchaseUserId` + 采购范围）
- ✅ 可查看下属员工创建的采购申请
- ❌ 不可查看销售相关数据
- ❌ 不可查看财务发票（除非财务部门）

#### 5.2.1 采购方向：销售数据「禁止」时的列级 / 字段级不可见范围（补充）

**适用条件（与 §5.2 典型配置及采购部门员工场景一致，以实现为准）：**

- 用户主部门（权限摘要）**`SaleDataScope == 4`（禁止查看销售数据）**；
- 且 **`IdentityType` 为采购方向**（**`2` 采购员**、**`3` 采购助理**；与 `RbacService` 中「采购侧部门」判定一致时，亦可视作采购部门员工；**客服 / 物流 / 财务**等身份见各自章节，**不在此表默认套用**）；
- **系统管理员**（`SYS_ADMIN` / `IsSysAdmin`）**不适用**本表脱敏，仍按管理员规则全量可见。

**说明：** 下列字段指**任意界面**（含跨模块联查、出库/报关/审批详情、与订单关联的报表等）在同时满足上述条件时应**不展示或以占位符替代**（如 `—` / `***`），避免通过采购可见页面**间接泄露客户深度档案与销售价格**。与 `FilterCustomersAsync`、`FilterSalesOrdersAsync` 等**行级**过滤互补：行级无权限时列表为空或已缩小；行级因业务需要仍展示少量销售关联信息时，须遵守本表**列级**约束。

| 序号 | 业务字段名（中文） | 含义与典型数据来源（API/表字段以实现为准） |
|------|-------------------|---------------------------------------------|
| 1 | **客户英文名** | 客户主数据英文名称（如 `CustomerInfo` / 客户档案中的 EN 名字段、双语展示中的英文分量）。 |
| 2 | **客户中文名** | 客户中文名称或主展示名（如法定名称之外的中文简称/全称中的中文展示列）。 |
| 3 | **客户全称** | 客户法定/工商**全称**（如 `officialName`、企业全称；不含仅内部简称时可单独约定是否脱敏简称）。 |
| 4 | **销售单价** | 销售订单明细**未税/含税单价**（如销售行单价、报价转订单后的单价类展示列）。 |
| 5 | **销售总额** | 销售侧**金额合计**（单行 `qty × 单价`、订单头销售总金额、出库/对账页面上回显的销售金额等）。 |
| 6 | **销售折算美元单价** | 销售行**折合 USD 的单价**（如 `convertPrice` / 折算美金单价）。 |
| 7 | **销售折算美元总额** | 折合 USD 的**行金额或订单级折算总额**（由折算单价与数量派生或独立存储字段）。 |

**实现提示（非规范性代码）：** 前端可基于 `GET /api/v1/auth/permission-summary` 中的 **`saleDataScope`**、**`identityType`**、**`isSysAdmin`**（及与实现一致的 **`belongsToPurchaseDept`** 若需与侧栏采购侧判定对齐）做列显隐；敏感列建议统一走占位组件，避免漏网。后端对导出、打印、聚合接口亦应按同一口径脱敏或拒绝字段。

### 5.3 财务总监（双向权限）

**部门配置：**
```json
{
  "IdentityType": 5,          // Finance
  "SaleDataScope": 0,         // 可查看全部销售数据
  "PurchaseDataScope": 0,     // 可查看全部采购数据
  "DepartmentName": "财务部",
  "Path": "Root/财务部"
}
```

**角色分配：** `DEPT_DIRECTOR` (总监层级)

**权限效果：**
- ✅ 可查看所有销售和采购数据
- ✅ 可查看所有财务数据（发票、收款、付款）
- ✅ 可查看下属财务人员的数据
- ✅ 可进行财务审核操作

### 5.4 系统管理员

**角色分配：** `SYS_ADMIN`

**权限效果：**
- ✅ 所有数据权限（不受部门身份类型限制）
- ✅ 所有功能权限（包括RBAC管理）
- ✅ 可查看系统所有数据
- ✅ 可管理所有用户、角色、权限

### 5.5 主菜单（侧栏）：采购侧员工不展示「出库管理」

**业务目的：** 采购部门员工以采购、入库、库存内勤为主，**不提供**侧栏进入「出库管理」板块的入口，与职责划分一致。

**判定条件（与 `RbacService` 采购侧部门一致）：**

- 权限摘要 `UserPermissionSummaryDto.BelongsToPurchaseDept == true`（主部门 `IdentityType` 为采购/采购助理 **2 / 3**；或用户兼任部门中存在采购侧部门；或主部门 `IdentityType` 未维护时按部门名称兜底识别「采购部」等）。
- **系统管理员**（`IsSysAdmin`）**不受**本条限制，侧栏始终展示全部菜单项。

**表现：**

| 菜单板块 | 采购侧员工（`BelongsToPurchaseDept` 且非管理员） | 其他部门用户 |
|----------|--------------------------------------------------|----------------|
| 出库管理（出库通知、拣货单、出库、出库明细） | **不展示** | 展示（与既有路由/权限一致；仍受 `LogisticsDataScope=4` 等物流维度约束） |

**实现说明：**

- 前端侧栏：`CRM.Web/src/layouts/AppLayout.vue` 中 `showStockOutMenus`（依赖 `showLogisticsMenus` 与 `belongsToPurchaseDept`）。
- 摘要字段：`GET /api/v1/auth/permission-summary` 返回 `belongsToPurchaseDept`（JSON camelCase）。

### 5.5.1 报关板块：仅物流部、财务部与系统/平台管理员

**业务目的：** 报关（待报关、报关公司、报关单、报关明细、报关客户移库）为关务与财务协同场景，**不对**销售、采购、商务等其他部门开放模块入口与 API。

**模块准入（与 `SaleDataScope` / `PurchaseDataScope` 无关）：**

| 身份 | 判定 | 报关侧栏 | 报关 API |
|------|------|----------|----------|
| 系统管理员 | `IsSysAdmin`（`SYS_ADMIN`） | ✅ | ✅ |
| 平台管理员 | `IsSysManager`（`SYS_MANAGER`） | ✅ | ✅（报关单/明细列表默认全量） |
| 业务数据 bypass | `HasBizDataBypass` | ✅ | ✅（列表默认全量） |
| 财务部 | `IdentityType` **5** | ✅ | ✅（报关单/明细列表默认全量） |
| 物流部 | `IdentityType` **6** | ✅ | ✅（报关单列表可按 `LogisticsDataScope` 收窄创建人） |
| 销售 / 采购 / 商务 / 客服等 | 其他 | ❌ | **403** |

**覆盖范围：**

| 前端 | API |
|------|-----|
| `/customs/*` | `GET/POST/PATCH/DELETE /api/v1/customs-*` |
| 路由守卫拦截直链 | `GET/PATCH /api/v1/inventory/transfers-customers`（报关移库） |

**待报关列表：** 历史曾按 **`SaleDataScope`** 经销售订单过滤；现改为模块准入后，有权限用户见**全量** pendlist（不再按业务员缩小）。

**实现说明：**

- 规则：`CRM.Core/Utilities/CustomsModuleAccessRules.cs`；API：`CRM.API/Utilities/CustomsModuleAccessHttp.cs`。
- 控制器：`CustomsPendlistsController`、`CustomsBrokersController`、`CustomsDeclarationsController`、`CustomsDeclarationItemsController`、`StockTransfersController`。
- 前端：`showCustomsMenus`（`AppLayout.vue`）；`canAccessCustomsModule` / `isCustomsModuleRoute`（`departmentModuleGate.ts`、`router/index.ts`）。

**文档对照：** 详见 `document/实现方案/RBAC权限节点清单.md` 第四节「4.1 主菜单（侧栏）…」。

### 5.6 跨业务线「上传单据」附件：销售侧不可见采购/付款附件，采购侧不可见销售/收款附件

**业务目的：** 在已实现 **§5.1.1**（销售方向对采购域列级脱敏）与 **§5.2.1**（采购方向对销售域列级脱敏）的基础上，进一步防止通过**文件内容**（合同扫描件、水单、往来函等）泄露对侧业务信息。约束对象为**上传文档管理系统**中挂在下列 **BizType** 下的附件（列表、上传、下载、预览、删除及管理端分页中对应类型）。

| 用户侧 | 判定（与 §5.1.1 / §5.2.1 及实现一致） | 禁止访问的 BizType | 说明 |
|--------|----------------------------------------|-------------------|------|
| **销售部门员工（销售方向）** | `PurchaseSensitiveFieldMask511.ShouldMask(permissionSummary)` 为真 | `PURCHASE_ORDER`、`FINANCE_PAYMENT` | 不可列出/上传/下载/预览/删除上述业务的附件；管理端文档查询中应排除上述 BizType。 |
| **采购部门员工（采购方向）** | `SaleSensitiveFieldMask521.ShouldMask(permissionSummary)` 为真 | `SALES_ORDER`、`FINANCE_RECEIPT` | 同上，针对销售订单与收款单附件。 |

**系统管理员**（`IsSysAdmin` / `SYS_ADMIN`）**不适用**本条，可访问全部附件。

**实现说明（与代码对齐）：**

- 策略类：`CRM.Core/Utilities/CrossSideDocumentAttachmentPolicy.cs`（内部复用 `PurchaseSensitiveFieldMask511.ShouldMask` / `SaleSensitiveFieldMask521.ShouldMask`）。
- API：`CRM.API/Controllers/DocumentsController.cs` 在上传、按业务查询、下载、预览、删除及管理端 `GET .../documents/admin` 中按当前用户权限摘要执行拒绝或过滤。
- 前端：与 `usePurchaseSensitiveFieldMask` / `useSaleSensitiveFieldMask` 一致，在采购订单/付款、销售订单/收款及待审批弹窗等界面**隐藏**附件区域或**不请求**附件列表，避免无意义 403；以后端拦截为准。

---

## 六、API接口规范

### 6.1 API路径规范

所有RBAC相关API必须使用 `/api/v1/rbac/` 前缀：

| 功能模块 | API路径 | 方法 | 说明 |
|----------|---------|------|------|
| **部门管理** | `/api/v1/rbac/departments` | GET | 获取部门列表 |
| | `/api/v1/rbac/departments` | POST | 创建部门 |
| | `/api/v1/rbac/departments/{id}` | PUT | 更新部门 |
| | `/api/v1/rbac/departments/{id}` | DELETE | 删除部门 |
| **角色管理** | `/api/v1/rbac/roles` | GET | 获取角色列表 |
| | `/api/v1/rbac/roles` | POST | 创建角色 |
| | `/api/v1/rbac/roles/{id}` | PUT | 更新角色 |
| **权限管理** | `/api/v1/rbac/permissions` | GET | 获取权限列表 |
| | `/api/v1/rbac/permissions` | POST | 创建权限 |
| **用户权限** | `/api/v1/rbac/users/{userId}/permissions` | GET | 获取用户权限汇总 |
| | `/api/v1/rbac/users/{userId}/departments` | PUT | 更新用户部门 |
| | `/api/v1/rbac/users/{userId}/roles` | PUT | 更新用户角色 |

### 6.2 权限验证属性

**后端控制器使用：**
```csharp
[RequirePermission("customer.view")]    // 需要客户查看权限
[RequirePermission("customer.edit")]    // 需要客户编辑权限
[RequirePermission("rbac.manage")]      // 需要RBAC管理权限（仅系统管理员）
```

### 6.3 数据权限过滤

**服务层调用：**
```csharp
// 在服务方法中调用数据权限过滤
var filteredCustomers = await _dataPermissionService.FilterCustomersAsync(
    currentUserId, 
    allCustomers
);
```

---

## 七、前端实现要求

### 7.1 权限指令

**Vue指令：**
```vue
<template>
  <!-- 按钮级权限控制 -->
  <button v-permission="'customer.edit'">编辑客户</button>
  
  <!-- 菜单级权限控制 -->
  <el-menu-item v-permission="'salesorder.view'">销售订单</el-menu-item>
</template>
```

### 7.2 权限检查函数

**TypeScript工具函数：**
```typescript
// 检查是否拥有权限
import { hasPermission } from '@/utils/permission';

// 使用示例
if (hasPermission('customer.edit')) {
  // 显示编辑按钮
}

// 检查是否系统管理员
import { isSystemAdmin } from '@/utils/permission';
if (isSystemAdmin()) {
  // 显示管理功能
}
```

### 7.3 权限数据存储

**Pinia Store：**
```typescript
interface PermissionState {
  // 用户权限汇总
  permissionSummary: PermissionSummary | null;
  // 权限编码列表
  permissionCodes: string[];
  // 角色编码列表
  roleCodes: string[];
  // 部门信息
  departments: Department[];
  // 主部门信息
  primaryDepartment: Department | null;
}

// 初始化时从API获取权限数据
await permissionStore.fetchUserPermissions();
```

### 7.4 路由权限控制

**路由守卫：**
```typescript
// 路由守卫检查权限
router.beforeEach((to, from, next) => {
  const permissionStore = usePermissionStore();
  
  // 检查路由meta中的权限要求
  if (to.meta.requiresPermission) {
    if (!permissionStore.hasPermission(to.meta.requiresPermission)) {
      next('/403'); // 无权限页面
      return;
    }
  }
  
  next();
});
```

---

## 八、数据库设计

### 8.1 表结构

```sql
-- 部门表
CREATE TABLE sys_department (
    id UUID PRIMARY KEY,
    department_code VARCHAR(50) UNIQUE NOT NULL,
    department_name VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES sys_department(id),
    path TEXT NOT NULL,
    level INTEGER NOT NULL DEFAULT 1,
    identity_type SMALLINT NOT NULL DEFAULT 1,
    sale_data_scope SMALLINT NOT NULL DEFAULT 1,
    purchase_data_scope SMALLINT NOT NULL DEFAULT 1,
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 角色表
CREATE TABLE sys_role (
    id UUID PRIMARY KEY,
    role_code VARCHAR(50) UNIQUE NOT NULL,
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    status SMALLINT NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 权限表
CREATE TABLE sys_permission (
    id UUID PRIMARY KEY,
    permission_code VARCHAR(100) UNIQUE NOT NULL,
    permission_name VARCHAR(100) NOT NULL,
    permission_type VARCHAR(20) NOT NULL,
    resource VARCHAR(50) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description TEXT,
    status SMALLINT NOT NULL DEFAULT 1
);

-- 用户-部门关联表
CREATE TABLE sys_user_department (
    user_id UUID NOT NULL REFERENCES sys_user(id),
    department_id UUID NOT NULL REFERENCES sys_department(id),
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    PRIMARY KEY (user_id, department_id)
);

-- 用户-角色关联表
CREATE TABLE sys_user_role (
    user_id UUID NOT NULL REFERENCES sys_user(id),
    role_id UUID NOT NULL REFERENCES sys_role(id),
    PRIMARY KEY (user_id, role_id)
);

-- 角色-权限关联表
CREATE TABLE sys_role_permission (
    role_id UUID NOT NULL REFERENCES sys_role(id),
    permission_id UUID NOT NULL REFERENCES sys_permission(id),
    PRIMARY KEY (role_id, permission_id)
);
```

### 8.2 索引优化

```sql
-- 部门路径查询优化
CREATE INDEX idx_department_path ON sys_department(path);

-- 用户部门查询优化
CREATE INDEX idx_user_department_user ON sys_user_department(user_id);
CREATE INDEX idx_user_department_dept ON sys_user_department(department_id);

-- 权限查询优化
CREATE INDEX idx_permission_code ON sys_permission(permission_code);
CREATE INDEX idx_permission_resource ON sys_permission(resource, action);
```

---

## 九、测试验收标准

### 9.1 功能测试

| 测试场景 | 测试要点 | 预期结果 |
|----------|----------|----------|
| **部门创建** | 创建销售部门，设置IdentityType=1 | 部门创建成功，业务员身份生效 |
| **角色分配** | 为用户分配DEPT_MANAGER角色 | 用户获得经理层级权限 |
| **权限验证** | 业务员尝试访问采购数据 | 返回403无权限 |
| **数据过滤** | 采购经理查看采购订单 | 只能看到本部门及下级数据 |
| **组织层级** | 总监查看下属数据 | 可看到所有下属经理和员工数据 |

### 9.2 集成测试

| 测试场景 | 测试要点 |
|----------|----------|
| **全流程权限** | 创建用户→分配部门→分配角色→验证权限 |
| **数据隔离** | 销售员和采购员互相无法访问对方数据 |
| **层级控制** | 经理可查看员工数据，员工不能查看经理数据 |
| **系统管理员** | 系统管理员可访问所有功能和数据 |

### 9.3 性能测试

| 测试指标 | 要求 |
|----------|------|
| **权限查询响应时间** | < 100ms |
| **数据过滤性能** | 万级数据过滤 < 500ms |
| **并发用户权限验证** | 支持1000+并发用户 |

---

## 十、部署与维护

### 10.1 初始化数据

**必须初始化的数据：**
```sql
-- 初始化角色
INSERT INTO sys_role (role_code, role_name, description) VALUES
  ('SYS_ADMIN', '系统管理员', '拥有系统所有权限'),
  ('DEPT_DIRECTOR', '部门总监', '部门最高权限，可查看下属所有数据'),
  ('DEPT_MANAGER', '部门经理', '可查看下属员工数据'),
  ('DEPT_EMPLOYEE', '部门员工', '仅能查看自己的数据');

-- 初始化权限（示例）
INSERT INTO sys_permission (permission_code, permission_name, permission_type, resource, action) VALUES
  ('rbac.manage', 'RBAC管理', 'menu', 'rbac', 'manage'),
  ('customer.view', '客户查看', 'api', 'customer', 'view'),
  ('customer.edit', '客户编辑', 'api', 'customer', 'edit'),
  ('vendor.view', '供应商查看', 'api', 'vendor', 'view'),
  ('salesorder.view', '销售订单查看', 'api', 'salesorder', 'view');

-- 为角色分配权限
INSERT INTO sys_role_permission (role_id, permission_id) 
SELECT r.id, p.id 
FROM sys_role r, sys_permission p 
WHERE r.role_code = 'SYS_ADMIN';
```

### 10.2 监控与日志

**需要监控的指标：**
1. 权限验证失败率
2. 数据过滤性能
3. 用户权限查询频率
4. 部门/角色变更日志

**审计日志记录：**
- 用户权限变更
- 部门配置修改
- 角色权限分配
- 数据访问越权尝试

### 10.3 备份与恢复

**定期备份：**
1. 部门结构数据
2. 角色权限配置
3. 用户权限关系

**恢复策略：**
1. 优先恢复角色权限配置
2. 恢复部门结构
3. 恢复用户权限关系

---

## 十一、版本历史

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|----------|
| v1.0 | 2026-04-08 | AI助手 | 初始版本，基于代码梳理的完整RBAC权限系统PRD |
| v1.1 | 2026-04-18 | AI助手 | 增补 5.5：采购侧员工侧栏隐藏「出库管理」「报关」及 `belongsToPurchaseDept` 摘要字段说明 |
| v1.2 | 2026-04-22 | AI助手 | 增补 §5.1.1：销售方向 + `PurchaseDataScope=4` 时采购域**列级/字段级**不可见清单（供应商英/全称、采购价与折算美元等） |
| v1.3 | 2026-04-22 | AI助手 | 增补 §5.2.1：采购方向 + `SaleDataScope=4` 时销售域**列级/字段级**不可见清单（客户英/中文/全称、销售价与折算美元等）；§4.2 列级补充与 §5.2.1 交叉引用 |
| v1.4 | 2026-04-24 | AI助手 | 增补 §5.6：销售侧不可见采购订单/付款上传附件，采购侧不可见销售订单/收款上传附件；§4.2 与附件策略交叉引用；实现见 `CrossSideDocumentAttachmentPolicy` 与 `DocumentsController` |
| v1.5 | 2026-06-11 | AI助手 | §5.5 拆分为出库/报关：§5.5.1 报关板块仅 **物流(6)/财务(5)/管理员** 可访问；待报关取消 `SaleDataScope` 过滤；API 403 + 前端 `showCustomsMenus` |
| v1.6 | 2026-06-03 | AI助手 | 新增 §2.4 管理角色、§4.1.1 系统管理门禁；权限实体扩展说明；附录 §12.1 对齐 `system.*` / `biz.*` 分层；专篇 [管理角色分级与权限体系PRD](./管理角色分级与权限体系PRD.md) |
| v1.7 | 2026-08-17 | AI助手 | §5.5.1 报关板块准入补齐 **平台管理员**（`IsSysManager` / `SYS_MANAGER`）与 `HasBizDataBypass`：侧栏、路由守卫、API 使用同一规则，避免菜单可见却被重定向到 `/dashboard` |
| v1.8 | 2026-08-17 | AI助手 | §2.4：L1/L2 置 `HasBizDataBypass`（平台管理员看全量业务数据）；L3 仍不改业务单据数据范围；与 [管理角色分级 PRD v1.1](./管理角色分级与权限体系PRD.md) 对齐 |

---

## 十二、附录

### 12.1 权限编码规范

**命名规则（现行 + 规划）：**

| 前缀 | 模式 | 说明 |
|------|------|------|
| 业务功能 | `{resource}.read` / `.write` | 如 `customer.read`、`sales-order.write` |
| 系统管理 | `system.{域}.{资源}.{动作}` | 如 `system.org.users.read`、`system.params.purchase.write` |
| 业务主数据 | `biz.{resource}.read` / `.write` | 如 `biz.brand.read` — 主数据 **列表/详情/维护** |
| 表单辅助 | `biz.{resource}.options` 或仅 `[Authorize]` | 如品牌下拉 — **不** 开放主数据页 |
| 兼容 | `rbac.manage` | Admin 超集别名；见专篇 §6.6 |

**完整清单、菜单映射、按域批量配置（方式 C）：** [管理角色分级与权限体系PRD §六–§八](./管理角色分级与权限体系PRD.md)。

| 资源分类 | 示例 | 说明 |
|----------|------|------|
| **系统管理** | `system.org.*`, `system.params.*`, `system.logs.*`, `system.platform.*` | 须管理身份 + 权限码 |
| **业务主数据** | `biz.brand.read`, `biz.brand.write` | 主菜单（如品牌管理）；默认不授予 `DEPT_EMPLOYEE` |
| **客户管理** | `customer.read`, `customer.write` | 客户相关权限 |
| **供应商管理** | `vendor.read`, `vendor.write` | 供应商相关权限 |
| **销售管理** | `sales-order.read`, `sales-order.write` | 销售订单权限 |
| **采购管理** | `purchase-order.read`, `purchase-order.write` | 采购订单权限 |
| **财务管理** | `finance-payment.read`, `finance-receipt.read` | 财务相关权限 |

### 12.2 常见配置示例

**销售部门配置示例：**
```json
{
  "departmentCode": "SALES_DEPT_01",
  "departmentName": "销售一部",
  "identityType": 1,
  "saleDataScope": 2,
  "purchaseDataScope": 4,
  "parentDepartment": "销售部"
}
```

**采购部门配置示例：**
```json
{
  "departmentCode": "PURCHASE_DEPT",
  "departmentName": "采购部", 
  "identityType": 2,
  "saleDataScope": 4,
  "purchaseDataScope": 0,
  "parentDepartment": null
}
```

**财务部门配置示例：**
```json
{
  "departmentCode": "FINANCE_DEPT",
  "departmentName": "财务部",
  "identityType": 5,
  "saleDataScope": 0,
  "purchaseDataScope": 0,
  "parentDepartment": null
}
```