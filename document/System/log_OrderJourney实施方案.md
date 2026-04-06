# log_OrderJourney（订单旅程表）实施方案

## 1. 目标与边界

| 项目 | 说明 |
|------|------|
| **目标** | 按时间顺序记录销售订单/采购订单及其**主表、明细**从创建到完结（及关键中间态）的**步骤变化**，支撑订单旅程图与事后完整还原。 |
| **与 `log_operation` 关系** | **`log_operation` 不变**：继续只记「用户操作行为」（谁在何时做了什么动作）。**订单旅程**用 **`log_OrderJourney`** 专用表，语义是「业务实体在时间轴上的状态/步骤演进」，可包含系统动作、供应商确认等非 `log_operation` 场景。 |
| **覆盖实体** | 销售订单主表、销售订单明细、采购订单主表、采购订单明细（首期）；后续可扩展 PR、入库通知、出库申请等，本方案预留扩展字段。 |

---

## 2. 表设计（PostgreSQL）

**表名**：`log_orderjourney`（小写，与 `log_operation` 风格一致；对外文档可称 log_OrderJourney）。

**设计原则**：

- 一行 = **一次步骤事件**（同一时刻同一实体可多字段变化时，可合并为一行 + `PayloadJson`）。
- **强类型枚举**用短字符串常量（`EntityKind`、`EventCode`），便于前后端与报表共用，避免魔法数字。
- **状态类变化**同时写 `FromState` / `ToState`（存 **smallint 字符串形式** 或与代码一致的数值字符串，如 `"10"`），便于过滤与展示。
- **供应商确认**等非用户账号动作：`ActorKind` 区分 `User` / `Vendor` / `System`，供应商侧可填 `ActorDisplayName` 或门户账号 Id。

### 2.1 字段定义

| 列名 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | `TEXT` PK | 是 | `gen_random_uuid()::text` |
| `EntityKind` | `VARCHAR(32)` | 是 | 实体类型，见 §3.1 |
| `EntityId` | `TEXT` | 是 | 主键：SellOrderId / SellOrderItemId / PurchaseOrderId / PurchaseOrderItemId |
| `ParentEntityKind` | `VARCHAR(32)` | 否 | 父级类型（如明细行填 `SellOrder`，主单可空） |
| `ParentEntityId` | `TEXT` | 否 | 父级 Id（如 `sell_order_id`） |
| `DocumentCode` | `VARCHAR(64)` | 否 | 冗余单号：`SellOrderCode` / `PurchaseOrderCode`，减少联表 |
| `LineHint` | `VARCHAR(200)` | 否 | 明细行展示用：如 PN+Brand 摘要，非匹配键 |
| `EventCode` | `VARCHAR(64)` | 是 | 稳定事件码，见 §3.2 |
| `EventLabel` | `VARCHAR(200)` | 否 | 中文/展示文案（可服务端 i18n 或由前端映射） |
| `FromState` | `VARCHAR(32)` | 否 | 变更前状态（数值或枚举名，团队统一一种） |
| `ToState` | `VARCHAR(32)` | 否 | 变更后状态 |
| **`EventTime`** | **`TIMESTAMPTZ`** | **是** | **事件时间**：该步骤在业务上发生的时刻（与 `log_operation.OperationTime` 语义类似；默认 `NOW()`，统一用 **UTC**） |
| **`Quantity`** | **`NUMERIC(18,4)`** | **否** | **数量**：与数量相关的事件填写（如采购申请数量、出库数量、明细行数量变更等）；无关则 `NULL` |
| **`Amount`** | **`NUMERIC(18,6)`** | **否** | **金额**：与金额相关的事件填写（如申请付款金额、核销金额、行金额等）；无关则 `NULL` |
| **`Currency`** | **`SMALLINT`** | **否** | **币别**：与 `Amount` 配套，编码与全库 **`CurrencyCode`（1=RMB…）** 一致；无金额或本位统一时可 `NULL`；**建议** `Amount` 非空时尽量带 `Currency` |
| **`Remark`** | **`VARCHAR(500)`** | **否** | **备注**：辅助说明（如驳回原因摘要、供应商确认附言等） |
| `PayloadJson` | `TEXT` | 否 | 结构化扩展（关联单号、多维度明细等 JSON）；与上列并存时，**可筛选、可汇总的口径优先写列字段**，Payload 作补充 |
| `RelatedEntityKind` | `VARCHAR(32)` | 否 | 关联实体类型（如关联的 PO、PR） |
| `RelatedEntityId` | `TEXT` | 否 | 关联实体 Id |
| `ActorKind` | `VARCHAR(16)` | 否 | `User` / `Vendor` / `System` |
| `ActorUserId` | `TEXT` | 否 | 内部用户 Id |
| `ActorUserName` | `VARCHAR(100)` | 否 | 内部用户显示名 |
| `ActorVendorId` | `TEXT` | 否 | 供应商确认等场景 |
| `Source` | `VARCHAR(64)` | 否 | 写入来源：`SalesOrderService`、`PurchaseOrderService`、`ApprovalsController` 等 |

**索引**：

- `IX_log_orderjourney_entity` ON (`EntityKind`, `EntityId`, `EventTime`);
- `IX_log_orderjourney_parent` ON (`ParentEntityKind`, `ParentEntityId`, `EventTime`);
- `IX_log_orderjourney_document_code` ON (`DocumentCode`, `EventTime`) WHERE `DocumentCode` IS NOT NULL;
- `IX_log_orderjourney_event` ON (`EventCode`, `EventTime`);

### 2.1.1 数量 / 金额 / 币别 / 备注（填写约定）

| 场景示例 | Quantity | Amount | Currency | Remark |
|----------|----------|--------|----------|--------|
| 采购申请提交 | 申请数量 | 可空 | 可空 | 可选 |
| 采购订单创建/变更行 | 行数量或汇总 | 行金额或汇总 | 行币别 | 可选 |
| 申请付款 / 核销 | 可空 | 本次金额 | 币别 | 可选 |
| 纯状态变更 | 全 `NULL` | 全 `NULL` | `NULL` | 可选 |
| 供应商确认 | 可空 | 可空 | `NULL` | 可写确认说明 |

### 2.2 建表 SQL（草案）

```sql
CREATE TABLE IF NOT EXISTS public.log_orderjourney (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "EntityKind" VARCHAR(32) NOT NULL,
    "EntityId" TEXT NOT NULL,
    "ParentEntityKind" VARCHAR(32),
    "ParentEntityId" TEXT,
    "DocumentCode" VARCHAR(64),
    "LineHint" VARCHAR(200),
    "EventCode" VARCHAR(64) NOT NULL,
    "EventLabel" VARCHAR(200),
    "FromState" VARCHAR(32),
    "ToState" VARCHAR(32),
    "EventTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Quantity" NUMERIC(18,4),
    "Amount" NUMERIC(18,6),
    "Currency" SMALLINT,
    "Remark" VARCHAR(500),
    "PayloadJson" TEXT,
    "RelatedEntityKind" VARCHAR(32),
    "RelatedEntityId" TEXT,
    "ActorKind" VARCHAR(16),
    "ActorUserId" TEXT,
    "ActorUserName" VARCHAR(100),
    "ActorVendorId" TEXT,
    "Source" VARCHAR(64)
);

CREATE INDEX IF NOT EXISTS IX_log_orderjourney_entity
    ON public.log_orderjourney ("EntityKind", "EntityId", "EventTime");
CREATE INDEX IF NOT EXISTS IX_log_orderjourney_parent
    ON public.log_orderjourney ("ParentEntityKind", "ParentEntityId", "EventTime");
```

---

## 3. 枚举与事件码约定

### 3.1 EntityKind（建议常量类 `OrderJourneyEntityKinds`）

| 常量值 | 含义 |
|--------|------|
| `SellOrder` | 销售订单主表 |
| `SellOrderItem` | 销售订单明细 |
| `PurchaseOrder` | 采购订单主表 |
| `PurchaseOrderItem` | 采购订单明细 |

### 3.2 EventCode（首期最小集，可迭代扩充）

**销售主单（SellOrder）**  

| EventCode | 说明 |
|-----------|------|
| `SO_CREATED` | 创建 |
| `SO_UPDATED` | 主信息或明细整体替换（可在 Payload 中带 `itemRows`） |
| `SO_STATUS_CHANGED` | 主状态变化（配合 FromState/ToState） |
| `SO_DELETED` | 删除（若业务允许） |

**销售明细（SellOrderItem）**  

| EventCode | 说明 |
|-----------|------|
| `SO_ITEM_CREATED` | 随主单创建或更新后新增行 |
| `SO_ITEM_REMOVED` | 更新时被删除（若实现为整单替换，可只记主单 `SO_UPDATED` + Payload 清单，二选一团队定） |
| `SO_ITEM_STATUS_CHANGED` | 明细状态（如取消） |

**采购主单（PurchaseOrder）**  

| EventCode | 说明 |
|-----------|------|
| `PO_CREATED` | 创建 |
| `PO_UPDATED` | 主信息/明细更新 |
| `PO_STATUS_CHANGED` | 主状态变化（含 **待确认→已确认(30)**） |
| `PO_VENDOR_CONFIRMED` | **供应商确认**（可与 `PO_STATUS_CHANGED` 合并为一行或两行：建议同一事务内先 `PO_STATUS_CHANGED` 再 `PO_VENDOR_CONFIRMED` 带备注，或单行 Payload 标明 `confirmedByVendor=true`） |
| `PO_DELETED` | 删除 |

**采购明细（PurchaseOrderItem）**  

| EventCode | 说明 |
|-----------|------|
| `PO_ITEM_CREATED` | 创建 |
| `PO_ITEM_STATUS_CHANGED` | 与主单同步或独立状态变化 |

**说明**：审批通过/拒绝若已由 `approval_record` 表达，旅程表仍建议记 **`PO_STATUS_CHANGED` / `SO_STATUS_CHANGED`**（或 `*_APPROVED` / `*_REJECTED` 专用码），以便**单表拉时间轴**画图，避免前端多源合并。

---

## 4. 技术实现路径

### 4.1 数据访问层

1. **EF Core**：新增实体 `OrderJourneyLog` 映射 `log_orderjourney`，`ApplicationDbContext` 增加 `DbSet<OrderJourneyLog>`（与 `ApprovalRecord` 一致，便于 LINQ 查询）。
2. **或服务层封装**：`IOrderJourneyLogService`  
   - `AppendAsync(OrderJourneyLogEntry entry, CancellationToken ct)`  
   - 内部仅 `INSERT`，失败策略：**建议不阻断主业务**（catch 记内部日志/指标，或依赖外部队列后续补录——二期）。

### 4.2 写入埋点（按优先级）

| 优先级 | 位置 | 记录内容 |
|--------|------|----------|
| P0 | `SalesOrderService`：`CreateAsync`、`UpdateAsync`、`UpdateStatusAsync`、`DeleteAsync` | 主单 + 必要时明细行创建/替换摘要 |
| P0 | `PurchaseOrderService`：`CreateAsync`、`UpdateAsync`、`UpdateStatusAsync`、`DeleteAsync` | 主单；**状态变为 30** 时写 `PO_STATUS_CHANGED` + 可选 `PO_VENDOR_CONFIRMED` |
| P0 | `ApprovalsController`：`PURCHASE_ORDER` / `SALES_ORDER` 审核通过/驳回 | 与状态变化一致的事件（避免重复可比较 `FromState`） |
| P1 | 采购订单「供应商确认」独立 API（若有） | `ActorKind=Vendor`，`PO_VENDOR_CONFIRMED` |
| P2 | PR、入库、出库与订单关联链路 | `RelatedEntityKind` / `RelatedEntityId` 指向 PR/通知单 |

**Actor 信息**：从 `HttpContext` Claims 取当前用户；后台任务用 `System`；供应商门户用 `Vendor` + `ActorVendorId`。

### 4.3 API（供旅程图）

- `GET /api/v1/order-journey/by-sell-order/{sellOrderId}`  
  - 查询：`EntityId = sellOrderId AND EntityKind = SellOrder` **或** `ParentEntityId = sellOrderId`，按 **`EventTime`** 排序。  
- `GET /api/v1/order-journey/by-purchase-order/{purchaseOrderId}`  
  - 同上。  
- 可选：`GET .../merged?sellOrderId=` 把关联 PO（经 `SellOrderItem`→`PurchaseOrderItem`）的旅程按 **`EventTime`** 合并（二期）。

### 4.4 与 `log_operation` 分工（再次强调）

| 表 | 职责 |
|----|------|
| `log_operation` | 用户操作审计（谁、何时、何种操作类型）。 |
| `log_orderjourney` | 订单/单据**业务时间轴**（实体维度、状态与关键业务步骤），可包含供应商、系统等非内部用户。 |

同一动作可同时写两张表：**不冲突**，查询场景不同。

---

## 5. 分阶段交付

| 阶段 | 内容 |
|------|------|
| **M1** | 迁移建表 + 实体 + `IOrderJourneyLogService` + `PurchaseOrder`/`SalesOrder` 的 `Create` + `UpdateStatus` 埋点 |
| **M2** | `Update`（含明细替换）Payload 规范 + 只读 API + 前端时间轴 PoC |
| **M3** | 审批、供应商确认、关联单据 Related 字段 + 合并查询 |
| **M4** | 与入库/出库/财务节点衔接（扩展 EntityKind 或仅用 Related） |

---

## 6. 文档与测试

- 在 [业务规则总览](./业务规则总览.md) 增加一节「订单旅程日志」指向本文件。  
- 单元测试：`OrderJourneyLogService` 插入与查询排序；集成测试：创建 SO → 改状态 → 断言行数与 `EventCode`。  

---

## 7. 风险与约定

- **重复事件**：状态接口被重复调用时，依赖业务层「仅在实际变化时写入」或唯一约束（一般不建唯一约束，靠逻辑防重）。  
- **时钟**：一律 **UTC** 写入 **`EventTime`（事件时间）**。  
- **明细整单替换**：若 `UpdateAsync` 为删后重建，建议主单记一行 `SO_UPDATED`/`PO_UPDATED`，`PayloadJson` 带 `replacedItemCount`，避免海量行；若需逐行级审计再补 `SO_ITEM_*`。  

---

*版本：2026-04-04；补充 `EventTime`、`Quantity`、`Amount`、`Currency`、`Remark`；时间列由 `OccurredAt` 统一为 `EventTime`（事件时间）。*
