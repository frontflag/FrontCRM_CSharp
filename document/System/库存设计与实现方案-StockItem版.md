# 库存设计与实现方案（StockItem 版）

## 1. 文档目的与范围

本文档约定以 **`stockitem` 为库存数量与业务维度的唯一事实来源**，**`stock`（现有 `StockInfo` / 表 `stock`）为按分桶聚合的汇总表**，**`stockledger`（`InventoryLedger`）为出入库审计流水、不参与数量校验与业务规则**。  

**首版假设**：无历史数据迁移；不处理「修改销售订单业务员后刷新库存冗余」等低频变更（后续迭代）。

---

## 2. 概念与表职责

| 对象 | 表 / 实体 | 职责 |
|------|-----------|------|
| 入库单据行 | `stockinitem` / `StockInItem` | 采购侧事实：本次入库数量、单价、物料、批次等；**不承载**在库结余的多维状态。 |
| 在库载体（新） | `stockitem`（新表） | **1 条 `stockinitem` 对应 1 条 `stockitem`**。承载入库后「这一层」的全部业务冗余：采购维度 + 销售维度 + 数量（入库量、已出、在库、锁定/预占等）。 |
| 汇总库存 | `stock` / `StockInfo` | **由 `stockitem` 按分桶键聚合刷新**，用于列表、权限过滤、快速总览；**不单独作为扣减真相**。 |
| 流水 | `stockledger` / `InventoryLedger` | 仅记录 `STOCK_IN` / `STOCK_OUT` 等事件，便于对账与审计；**不作为幂等主依据**（见 6.2）。 |
| 出库单据行 | `stockoutitem` / `StockOutItem` | 每次实际从某一 `stockitem` 上扣减（或预占）时，**每条出库明细对应当时选中的 1 条 `stockitem`**；同一 `stockitem` 可被多条 `stockoutitem` 引用（分多次出库）。 |

### 2.1 `stockinitem` 与 `stockitem` 的差异（语义）

- **`stockinitem`**：偏**单据**——供应商、采购员（若在头或行上）、本次入库数量、采购单价等；过账后行本身一般不再改数量含义。  
- **`stockitem`**：偏**库存层**——在 `stockinitem` 基础上扩展**客户、业务员、销售价、销售订单明细**等冗余，并维护**动态数量**（已出库、在库、拣货占用、销售预占等）。

### 2.2 冗余策略（ID + 名称）

为提升查询性能，**同时存储外键 ID 与展示用名称/编号**，例如：

- `CustomerId` + 客户全称（或简称，口径需统一）  
- `SellOrderItemId` + 销售订单明细业务编号（如 `SellOrderItemCode`）  
- `SalespersonId`（或业务员用户 ID）+ 业务员显示名  

**约定**：首版以**入库过账（生成 `stockitem`）时点**的快照为准；主数据后续变更**不自动回写** `stockitem`（与产品确认一致，后期可做单次修复任务）。

---

## 3. 分桶键（与现有实现对齐）

当前入库过账在 `InventoryCenterService` 中使用的 **`StockInfo` 合并分桶键**为：

**`PurchasePn` + `PurchaseBrand` + `WarehouseId` + `StockType` + `RegionType` + `SellOrderItemId`**

（`PurchasePn` / `PurchaseBrand` 由采购订单明细解析；文本比较忽略大小写，空串归一为空键；`RegionType` 经 `RegionTypeCode.Normalize` 比较。）

**StockItem 版要求**：

- **`stock` 行的聚合维度与上述分桶键保持一致**，避免出现「`stockitem` 能算出来、列表 `stock` 对不上」的双口径。  
- 单条 **`stockitem`** 除分桶字段外，还需保留**入库层**独有信息：如 `StockInItemId`（唯一）、入库时间、`StockInId`、可选批次/库位等，以支持 FIFO 与追溯。

---

## 4. `stockitem` 建议字段（实现时可微调）

下列为逻辑字段清单，具体列名、长度、索引以 EF 迁移为准。

### 4.1 主键与绑定

| 字段 | 说明 |
|------|------|
| `Id` | 主键 GUID |
| `StockInItemId` | **唯一**，对应 `stockinitem`，1:1 |
| `StockInId` | 入库单头，便于按单查询 |

### 4.2 物料与仓库

| 字段 | 说明 |
|------|------|
| `MaterialId` | 与入库行一致 |
| `WarehouseId` | 仓库 |
| `LocationId` | 库位，可空 |
| `BatchNo` / `ProductionDate` / `ExpiryDate` | 与入库行一致时可冗余 |

### 4.3 分桶与类型（与 `stock` 对齐）

| 字段 | 说明 |
|------|------|
| `StockType` | 客单 / 备货 / 样品等，与现有 `StockInfo.StockType` 一致 |
| `RegionType` | 境内 / 境外，与 `stockin` 一致 |
| `PurchasePn` / `PurchaseBrand` | 与当前 `StockInfo` 分桶一致 |
| `SellOrderItemId` / `SellOrderItemCode` | 可空；未绑定销售行时参与「空键」分桶 |

### 4.4 采购侧冗余

| 字段 | 说明 |
|------|------|
| `PurchaseOrderItemId` / `PurchaseOrderItemCode` | 与头单/行一致 |
| `VendorId` + 供应商名称 | 可空视业务 |
| `PurchaserId` + 采购员名称 | 若系统有采购员字段则冗余 |
| `PurchasePrice` / `PurchaseAmount` | 可与 `stockinitem` 单价、金额一致 |
| `PurchaseCurrency` | 采购单价币别（与 `CurrencyCode` / 采购明细 `currency` 一致） |
| `PurchasePriceUsd` | 采购单价折合 USD（入库过账时按财务基准汇率计算，与 `ExchangeRateToUsdConverter` 一致） |

### 4.5 销售侧冗余

| 字段 | 说明 |
|------|------|
| `CustomerId` + 客户名称 | |
| `SellOrderItemId` / `SellOrderItemCode` | 与分桶字段可合并设计，避免重复列时需在模型层统一 |
| `SalespersonId` + 业务员名称 | |
| `SalesPrice` | 若有按行销售价则快照 |
| `SalesCurrency` | 销售单价币别（与 `sellorderitem.currency` 一致）；无销售行时为 null |
| `SalesPriceUsd` | 销售单价折合 USD（过账时按财务基准汇率计算）；无销售行时为 null |

### 4.6 数量（整数，与当前 `StockInfo` 一致）

| 字段 | 含义建议 |
|------|----------|
| `QtyInbound` | 本层入库数量（通常等于 `stockinitem.Quantity` 过账值） |
| `QtyStockOut` | 本层已累计出库数量 |
| `QtyOccupy` | 拣货占用 |
| `QtySales` | 销售预占（若业务仍区分） |
| `QtyRepertory` | 在库 = `QtyInbound - QtyStockOut`（或由服务层统一重算） |
| `QtyRepertoryAvailable` | 可用 = `QtyRepertory - QtyOccupy - QtySales`（口径与 `StockInfo` 一致） |

**约束**：所有扣减、占用类更新在应用层或 DB 约束中保证 **不出现负数**（见 7.2）。

### 4.7 并发控制

| 字段 | 说明 |
|------|------|
| `RowVersion` / `xmin` | 建议为 `stockitem`（及必要时 `stock`）增加乐观并发令牌，防止并发拣货覆盖写 |

---

## 5. `stock`（`StockInfo`）与 `stockitem` 的关系

### 5.1 数据流

1. **入库过账**：按 `stockinitem` **插入 1 条 `stockitem`**，写入快照字段与 `QtyInbound`；再 **按分桶聚合更新或插入 1 条 `stock`**（对桶内 `SUM` 各数量字段）。  
2. **出库 / 拣货确认**：更新被选中的 **`stockitem`** 的数量；**同一事务内**重算并更新对应 **`stock` 分桶行**。  
3. **预占 / 释放**：仅动 `stockitem` 的 `QtyOccupy` / `QtySales` 等，再同步 `stock`。

### 5.2 汇总公式（与桶键一致）

对固定分桶键 \(B\)：

- `stock.Qty` = \(\sum \text{stockitem.QtyInbound}\)（或与现有命名对齐的「总入库」语义）  
- `stock.QtyStockOut` = \(\sum \text{stockitem.QtyStockOut}\)  
- `stock.QtyOccupy` = \(\sum \text{stockitem.QtyOccupy}\)  
- 其余字段同理，**与现有 `StockInfo` 字段语义保持一致**，便于前端与报表零改或微改。

### 5.3 一致性原则

- **以 `stockitem` 为准**；`stock` 仅为缓存聚合。  
- 提供 **对账 / 修复** 入口（管理端或脚本）：按 `stockitem` 全量重算 `stock`，修复偶发不一致。

### 5.4 拣货与校验：`stock` 与 `stockitem` 分工

与《装箱拣货出库》§5.2 一致，避免混用数据源：

| 场景 | 使用表 | 说明 |
|------|--------|------|
| **判断是否具备足够库存数量**（申请出库、装箱前校验、展示某分桶可出总量等） | **`stock`（`StockInfo`）** | 读汇总字段如 **`QtyRepertoryAvailable`**；不对明细层逐行 SUM 作为唯一门槛（除非对账）。 |
| **具体拣货**（候选批次列表、FIFO 推荐、勾选/扫码、预占与扣减、出库明细绑定） | **`stockitem`** | 所有「从哪一层出、出多少」的操作均针对 **`stockitem`**，并写入 **`StockItemId`**。 |

---

## 6. 出库分配（FIFO + 人工裁定）

### 6.1 默认规则

- 在同一分桶内，对可满足需求的 **`stockitem` 候选集**按 **入库时间升序**（并列时按 `StockInItemId` 或 `stockitem.Id` 升序，保证稳定排序）——**先入先出**。  
- 系统输出 **推荐拣货方案**（列出 `stockitem` 列表及建议数量）；**物流/拣货人员可改选其他合法 `stockitem`**，只要该层 `QtyRepertoryAvailable` 足够。

### 6.2 `stockoutitem` 关联

- 增加 **`StockItemId`**（必填或「确认出库」时必填），指向被扣减的那一层。  
- 保留现有 **`StockId`** 的过渡策略（二选一，实现阶段定稿）：  
  - **方案 A**：过渡期双写 `StockId` + `StockItemId`，读路径以 `StockItemId` 为准；  
  - **方案 B**：首版即只用 `StockItemId`，`StockId` 仅由聚合维护供旧界面展示（若界面仍按 `stock` 行展示）。

### 6.3 幂等与流水

- **入库过账幂等**：以 **`stockitem.StockInItemId` 唯一约束**（或 `stockinitem` 上「已过账」状态位）为准，**不再依赖**「`stockledger` 是否已有 `STOCK_IN` 行」。  
- **`stockledger`**：仍在过账成功路径上 **追加写入**，仅作审计；重复业务请求应在写入 `stockitem` 之前被挡回。

---

## 7. 实现阶段建议

### 7.1 阶段划分

| 阶段 | 内容 |
|------|------|
| P1 模型与迁移 | 新建 `stockitem` 表；`stockoutitem` 增加 `StockItemId`；唯一索引 `UX_stockitem_stockinitem`；`stockitem` 乐观并发字段。 |
| P2 入库 | 改造 `PostStockInAsync`：每行创建/跳过 `stockitem`，再更新 `stock` 聚合；写 `stockledger`；移除对 ledger 的幂等依赖。 |
| P3 出库 | 改造出库确认路径：FIFO 推荐 API + 人工选定 `StockItemId`；更新 `stockitem` + `stock`；写 `stockledger`。 |
| P4 查询与前端 | 库存列表以 `stock` 为主；明细/追溯/拣货界面展示 `stockitem`。 |
| P5 测试与对账 | 集成测试覆盖 1:1 入库、多次出库、并发扣减、分桶汇总与 `stock` 一致；可选 SQL 对账脚本。 |

### 7.2 事务与并发

- **同一事务**：`stockitem` 数量变更与对应 **`stock` 桶更新**必须同事务提交。  
- **并发**：对同一 `stockitem` 行使用 **乐观锁**（更新失败则重试或提示冲突）；对热点 `stock` 桶可减少冲突频率（先锁 `stockitem` 再聚合更新 `stock`）。

### 7.3 其他入库类型（首版可选）

当前 `stockin.StockInType` 含采购、退货、调拨、其他等。**首版可仅对「采购入库」生成 `stockitem`**，其他类型后续扩展相同模式；若只做采购，需在服务层显式分支并文档说明。

---

## 8. 与现有代码的映射

| 现有项 | 说明 |
|--------|------|
| `CRM.Core.Models.Inventory.StockInfo` + 表 `stock` | 保留为汇总实体，刷新逻辑改为源于 `stockitem`。 |
| `InventoryCenterService.PostStockInAsync` | 由「合并到单一 `StockInfo` 行」改为「每行 `StockInItem` → `StockItem` + 更新桶汇总」。 |
| `InventoryCenterService.RecordStockOutAsync` | 扣减应基于 `StockItemId` 定位行，再同步 `stock`；流水仍写 `InventoryLedger`。 |
| `InventoryLedger` + 表 `stockledger` | 保持为审计流水；`BizLineId` 继续对应 `stockinitem` / `stockoutitem` 行 ID。 |

---

## 9. 验收要点（摘要）

1. 每条已过账的 `stockinitem` 有且仅有一条 `stockitem`，且 `QtyInbound` 与入库数量一致。  
2. 任意时刻，按文档分桶键对 `stockitem` 聚合结果与对应 `stock` 行数量字段一致（允许维护窗口后通过对账任务修复）。  
3. 出库推荐按 FIFO 排序；人工改选后扣减正确，`QtyRepertoryAvailable` 不为负。  
4. 重复调用入库过账不产生重复 `stockitem`（唯一约束 + 业务返回明确）。  

---

## 10. 相关文档（需与本方案口径一致时优先同步）

| 文档 | 路径 | 同步要点 |
|------|------|----------|
| 业务逻辑与数据关系 | `document/System/业务逻辑与数据关系文档.md` | §4 库存模型、§7 ER/外键与 `StockItem` 目标关系 |
| 业务规则总览 | `document/System/业务规则总览.md` | §9 入库、§15 索引 |
| 系统架构与运行机制 | `document/System/系统架构与底层运行机制文档.md` | §4.3 库存模块实体列表 |
| 入库/库存/出库（EBS 对照） | `document/EBS/物流/入库库存出库业务关系文档.md` | 文首 FrontCRM 命名对照表 |
| 采购备货可用量 | `document/System/采购备货/销售订单使用采购备货-业务逻辑.md` | `StockInfo` 与 `stockitem` 汇总关系说明 |
| 装箱拣货出库 | `document/EBS/物流/装箱拣货出库.md` | §5.2：校验用 `stock`、拣货用 `stockitem` |

其他 PRD/EBS 中仅提及「库存表」「FIFO」而未区分汇总层/明细层时，**以本方案为准**解释实现演进。

---

## 11. 修订记录

| 日期 | 说明 |
|------|------|
| 2026-04-11 | 初稿：与用户确认的 StockItem 版职责、1:1/多对一关系、冗余与 FIFO 策略对齐现有 `StockInfo` 分桶实现。 |
| 2026-04-11 | 增加 §10 相关文档索引；与 System/EBS 多篇文档交叉引用对齐。 |
| 2026-04-11 | 新增 §5.4 拣货与校验分工；修正 §5.2 汇总公式笔误（`QtyStockOut` 对 `stockitem` 求和）；§10 增加装箱拣货文档索引。 |
