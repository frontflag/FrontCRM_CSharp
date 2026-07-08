# 报关 V2 — DDL 与表结构说明

> **版本**：讨论定稿（2026-06）  
> **配套脚本**：[`scripts/customs_v2_schema_postgresql.sql`](../../scripts/customs_v2_schema_postgresql.sql)  
> **范围**：数据库结构；不含 API / 前端实现。  
> **旧流程**：`CompleteDeclarationAndTransferAsync` / 移库过账 **废弃**；`stocktransfer_customers` **不再新增**（表保留只读）。

---

## 1. 业务对象与表映射

| 业务对象 | 表 | 说明 |
|----------|-----|------|
| 销售出库通知 | `stockout_notify` | `StockOutType=10` |
| 待报关 | **`customs_pendlist`** | 与销售出库通知 **1:1**，无独立业务单号 |
| 报关出库通知 | `stockout_notify` | `StockOutType=20`；与 pendlist **1:1** |
| 报关装箱单 | `packing` | `StockOutType=20`；`customs_broker_id` |
| 报关装箱明细 | `packing_item` | **1:1** 绑定报关出库通知（`stockout_notify_id`） |
| 报关记录 | `customs_declaration` | 与装箱 **1:1**；**去掉** `StockOutRequestId` |
| 报关明细 | `customs_declaration_item` | 装箱确认时生成；拣货后回写源在库行 |
| 报关到货 | `stockin_notify` | `StockInType=20` |
| 报关 QC | `qcinfo` | `StockInType=20` |
| 报关入库 | `stock_in` | `StockInType=20` |

---

## 2. 端到端 FK 关系（简图）

```text
stockout_notify (销售, Type=10)
    │ 1:1
customs_pendlist
    │ 1:1
stockout_notify (报关出库, Type=20)
    │ N:1  packing_item.stockout_notify_id
packing (Type=20, customs_broker_id)
    │ 1:1  customs_declaration.packing_id
customs_declaration
    │ 1:N
customs_declaration_item ──→ stockin_notify.customs_declaration_item_id
                              └── qcinfo → stock_in
```

---

## 3. 枚举（应用层常量，库内为 `smallint`）

### 3.1 `stockout_notify.Status`

| 值 | 常量 | 含义 |
|----|------|------|
| -1 | Cancelled | 已取消 |
| **5** | **PendingCustoms** | **待报关**（禁销售装箱） |
| 10 | PendingPacking | 待装箱 |
| 20 | Packed | 已装箱 |
| 100 | StockedOut | 已出库 |

报关入库完成后：销售通知 **5 → 10**（自动）。

### 3.2 `customs_pendlist.status`

| 值 | 含义 |
|----|------|
| 1 | **Open（新建）**：可生成报关出库通知 |
| 2 | **CustomsOutNotifyCreated**：已生成报关出库通知 |
| 3 | **InCustomsProcess**：已进装箱/报关单流程 |
| 10 | **Closed**：报关入库完成 |
| -1 | **Cancelled**：随销售出库通知取消 |

装箱作废/移除明细：pendlist **恢复为 1（Open）**（应用层）。

### 3.3 `stockout_notify.StockOutType`

| 值 | 含义 |
|----|------|
| 10 | 销售出库通知 |
| 20 | 报关出库通知 |

---

## 4. 新建表：`customs_pendlist`

| 列名 | 类型 | 空 | 说明 |
|------|------|----|------|
| `id` | varchar(36) | N | PK |
| `sales_stockout_notify_id` | varchar(36) | N | 销售出库通知 FK → `stockout_notify."ID"`，**唯一**（未删） |
| `sell_order_item_id` | varchar(36) | N | 销售明细冗余 |
| `qty` | integer | N | 待报关数量（= 销售出库通知数量） |
| `status` | smallint | N | 见 §3.2，默认 **1** |
| `customs_stockout_notify_id` | varchar(36) | Y | 报关出库通知 FK，**唯一**（未删且非空） |
| `overseas_warehouse_id` | varchar(36) | Y | 创建时快照主境外仓（可选，辅助列表） |
| `create_time` / `modify_time` | timestamptz | | 审计 |
| `create_by_user_id` / `modify_by_user_id` | varchar(36) | Y | |
| `is_deleted` | boolean | N | 默认 false |

**索引**

- `UX_customs_pendlist_sales_sor`：`sales_stockout_notify_id` WHERE `is_deleted = false`
- `UX_customs_pendlist_customs_sor`：`customs_stockout_notify_id` WHERE `is_deleted = false AND customs_stockout_notify_id IS NOT NULL`
- `IX_customs_pendlist_sell_line`：`sell_order_item_id` WHERE `is_deleted = false`

**无业务单号**；列表展示销售出库通知 `Code`。

---

## 5. 变更表：`stockout_notify`

| 变更 | 列 | 说明 |
|------|-----|------|
| 注释 | `Status` | 增加 **5=待报关** |
| 新增 | `customs_pendlist_id` | varchar(36) NULL；**仅 Type=20** 使用；指向 pendlist，**唯一**（未删且非空） |

> 销售出库通知（Type=10）与 pendlist 的关联在 **`customs_pendlist.sales_stockout_notify_id`**，不在销售通知行上重复 FK。

---

## 6. 变更表：`packing`

| 变更 | 列 | 说明 |
|------|-----|------|
| 新增 | `customs_broker_id` | varchar(36) NULL → `customs_broker."Id"`；**报关装箱必填**（应用校验） |
| 新增 | `customs_declaration_id` | varchar(36) NULL；装箱确认后写入；**唯一**（未删且非空） |

报关装箱 **不使用** `customer_id` 表示报关公司。

---

## 7. 变更表：`packing_item`

| 变更 | 列 | 说明 |
|------|-----|------|
| 已有 | `stockout_notify_id` | 报关场景 **1:1 绑定报关出库通知**（Type=20） |
| 新增 | `customs_pendlist_id` | varchar(36) NULL；冗余，便于回退/溯源；**唯一**（未删且非空） |

---

## 8. 变更表：`customs_declaration`

| 变更 | 说明 |
|------|------|
| **删除** | `StockOutRequestId` 列及 FK、唯一索引 |
| 新增 | `packing_id` varchar(36) NOT NULL → `packing."Id"`，**唯一**（`is_deleted = false`） |
| 保留 | `FromWarehouseId`（明细源境外仓 **自动带出**）、`ToWarehouseId`（**手动**） |

---

## 9. 变更表：`customs_declaration_item`

| 变更 | 列 | 说明 |
|------|-----|------|
| **改可空** | `SourceStockItemId` | 装箱生成时可为空；**拣货后回写** |
| 保留 | `StockOutRequestId` | 仍指向 **销售**出库通知 |
| 新增 | `customs_pendlist_id` | varchar(36) NOT NULL |
| 新增 | `customs_stockout_notify_id` | varchar(36) NOT NULL（报关出库通知） |
| 新增 | `packing_item_id` | varchar(36) NOT NULL，**唯一**（未删） |
| 新增 | `original_purchase_price` | numeric(18,6) DEFAULT 0；P0 快照 |
| 新增 | `vendor_id` | varchar(36) NULL；原始供应商 |

---

## 10. 变更表：`stock_out_item_extend`

| 变更 | 列 | 说明 |
|------|-----|------|
| 新增 | `original_purchase_price` | numeric(18,6) NOT NULL DEFAULT 0；**P0** |
| 新增 | `vendor_id` | varchar(36) NULL |
| 新增 | `customs_declaration_item_id` | varchar(36) NULL；报关溯源 |

**语义（定稿）**

| 场景 | `original_purchase_price` | `PurchasePrice` |
|------|---------------------------|-----------------|
| 报关出库 extend | P0 | P0 |
| 销售出库 extend | P0 | **P1（报关采购价）** |

---

## 11. 变更表：`stockin_notify`（报关到货）

| 新增 | 列 | 说明 |
|------|-----|------|
| `customs_declaration_item_id` | varchar(36) NULL | 从报关明细关联；**唯一**（未删且非空） |

**发起方式（V2 定稿，2026-07）**

| 项 | 约定 |
|----|------|
| **触发** | **人工**：报关单详情/列表点击「生成报关到货通知」 |
| **API** | `POST /api/v1/customs-declarations/{id}/create-arrival-notifies` |
| **前置** | ① 海关状态 = **已结关**（`CustomsClearanceStatus=100`）；② 已维护 **目标境内仓库**（`ToWarehouseId`）；③ 各明细对应 **报关出库通知已执行出库**（`Status=100`）；④ 该明细尚无到货通知（幂等） |
| **不再自动** | ~~报关出库完成时系统自动创建~~（已移除，避免关务未结关时境内仓过早出现待检） |
| **粒度** | 按报关单头 **批量** 为本单所有满足条件的明细各生成一条 `StockInType=20` 到货通知 |

服务实现：`CustomsV2FlowService.CreateCustomsArrivalNotifiesAsync`。

**完整设计与操作说明：** [报关到货通知（手工发起）— 设计与实现](../../System/报关/报关到货通知-设计与实现.md)、[报关业务溯源与供应商口径](../../System/报关/报关业务溯源与供应商口径-设计与实现.md)（`VendorId`、报关单 Hub 跳转）

---

## 12. 不变更 / 仅应用层

| 对象 | 说明 |
|------|------|
| `stock_item` | 报关入库时 `PurchasePrice` = P1（`TaxIncludedUnitPrice`） |
| `qcinfo` | 复用；`StockInType=20`；**部分通过按全拒** |
| `stocktransfer_customers` | 不删表；V2 **禁止新写入** |
| RBAC | 不新增节点 |

---

## 13. 约束摘要（须应用 + DB 协同）

1. **跨境外仓禁止**合并报关装箱：组箱前校验各报关出库通知源仓一致。  
2. **1 装箱 → 1 报关记录**：`customs_declaration.packing_id` 唯一。  
3. **1 pendlist → 1 销售 SOR / 1 报关出库通知**。  
4. **1 packing_item → 1 报关出库通知**（Type=20）。  
5. 已生成报关出库通知 → **禁止取消**销售出库通知。  
6. `POST .../customs-declarations/{id}/complete` → **410**。

---

## 14. 执行顺序

1. 阅读本文档。  
2. 在 **非生产** 库执行 [`scripts/customs_v2_schema_postgresql.sql`](../../scripts/customs_v2_schema_postgresql.sql)。  
3. 后续 EF Migration / 实体类与本文对齐（开发阶段）。  

**回滚**：脚本末尾含 `Down` 段（删除新表、恢复列需按环境评估）；生产执行前 **备份**。

---

## 15. 与旧文档关系

| 文档 | 关系 |
|------|------|
| `报关_移库.md` | **已作废**；移库一步过账废弃 |
| `System/报关/报关费用方案.md` | **费用定稿**（系数、汇率、公式、DDL 增量规划 §16） |
| `System/报关/报关费用面板-原型字段清单.md` | 详情页「报关费用」面板字段 |
| `报关模块完整实施方案.md` | 被本文 **V2 流程** supersede |
| `System/报关/报关到货通知-设计与实现.md` | 境内到货通知 **手工发起** 规则与实现 |
| `报关V2前备份` commit | 代码基线 |

---

## 16. 报关费用扩展（定稿 · 待 F1 迁移）

> 完整业务规则见《[报关费用方案](../System/报关/报关费用方案.md)》；UI 见《[报关费用面板 — 原型字段清单](../System/报关/报关费用面板-原型字段清单.md)》。

### 16.1 新表 `purchase_cost_param`

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | varchar(36) PK | |
| `ratio` | numeric(10,4) NOT NULL | 采购系数 |
| `start_time` | timestamptz NOT NULL | 生效开始时间 |
| `remark` | varchar(500) | |
| `is_deleted` | bool DEFAULT false | |

### 16.2 变更 `customs_broker`

| 新增 | `agency_rate` numeric(10,6) NOT NULL DEFAULT 1 | 1+代理费率 |

### 16.3 变更 `customs_declaration`

| 新增 | `broker_agency_rate` | 试算时快照 |
| 新增 | `fees_calculated_at` | 最后试算时间 |
| 新增 | `fees_locked` | 可选锁定 |
| 语义 | `exchange_rate` | **关务手工**，非财务自动锁定 |

### 16.4 变更 `customs_declaration_item`

| 新增 | `purchase_cost_param_id` | 系数配置 Id |
| 新增 | `purchase_ratio` | 系数快照 |
| 新增 | `purchase_currency` | 采购币别 |
| 新增 | `cost_usd` | 采购美金价 |
| 新增 | `duty_rate` | 关税税率 |
| 新增 | `vat_rate` | 增值税率，默认 0.13 |
| 可选 | `customs_usd_price` | 报关美金价 |

脚本增量在 F1 追加至 `scripts/customs_fees_f1_schema_postgresql.sql` 与 EF Migration `20260807120000_CustomsFeesF1Schema`。
