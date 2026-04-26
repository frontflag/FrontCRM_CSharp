# 强制删除（一期）— BizType 实体边界表（**已确认**）

> **状态**：**已确认**（与 [强制删除_产品约定与总原则](./强制删除_产品约定与总原则.md) 一致）。表名/实体以 `CRM.Core` 中 `[Table]` 为准。  
> **一期范围**：采购申请 → 入库链路；出库通知 → 出库链路；**含拣货任务、库存分桶 `stock`、在库层 `stock_item`、`stockledger` 库存流水**。  
> **前提**：已过账 / 已核销等 **不允许** 强制删除（须先 **两步人工** 完成财务/库存冲销后再删，见总原则 §4）；软删后一期 **全员不可见**；下游已软删视同无下游。

---

## 0. 与总原则对齐（摘录）

- 删除种类与 UI：**选项 A**（普通用户软删「删除」+ Admin「强制删除」；**不暴露物理删**）。见总原则 §1。  
- **RFQ 状态 `7`**：不可普通删除，**可**强制删除（满足无下游等）。见总原则 §2。  
- **报表/导出**：不可选已删除记录。见总原则 §6。  
- **Quote 强删后**：清空销售明细 `QuoteId` 并重算关联字段（总原则 §7；Quote 可与一期并行注册处理器）。

---

## 1. 列说明（每张 BizType 子表均建议填全）

| 列名 | 含义 |
|------|------|
| **BizType 编码** | API/处理器注册用稳定字符串，如 `PURCHASE_ORDER`。 |
| **业务对象** | 用户理解的「一张单」。 |
| **主实体（C#）** | 强制删除入口对应的主表实体。 |
| **主表（DB）** | PostgreSQL 表名（来自 `[Table]`）。 |
| **级联软删（同事务）** | 随主单一并软删的子表/扩展；**须列全**，避免漏表。 |
| **关联但未必随主单删** | 可能被引用、需单独规则（仅断开 / 禁止删 / 另开 BizType）。 |
| **库存与流水** | `stock` / `stock_item` / `stockledger` 及占用、回滚策略。 |
| **CanForceDelete** | 禁止条件摘要（实现时展开为代码与单测）。 |
| **SyncAfterDelete** | 删后须重算/回写的业务汇总。 |
| **实现备注** | 仍须在开发阶段落地的技术细节。 |

---

## 2. BizType：`PURCHASE_REQUISITION`（采购申请）

| 项 | 内容 |
|----|------|
| **业务对象** | 采购申请 PR |
| **主实体** | `PurchaseRequisition` |
| **主表** | `purchaserequisition` |
| **级联软删** | 无子表（单行头表）；若后续有 PR 扩展表需补列。 |
| **关联但未必随主单删** | 指向 `sell_order_item_id` / `sell_order_id`（销售侧不删）；**未软删的** `purchaseorder` / `purchaseorderitem` 若由本 PR 生成则视为下游 → **禁止** 强删或先处理 PO。 |
| **库存与流水** | 申请本身不产生 `stock_item` / `stockledger`；**不直接碰库存**。 |
| **CanForceDelete** | 无未软删下游 PO/入库；无未冲销的过账/核销；状态等业务规则见总原则。 |
| **SyncAfterDelete** | 释放销售明细侧请购占用；重算 `SellOrderItemExtend` 等与 PR 占用相关字段（复用现有 `Recalculate`/占用口径）。 |
| **实现备注** | 与「非取消 PR 占用销售行」防重规则在强删后 **必须释放** 占用。 |

---

## 3. BizType：`PURCHASE_ORDER`（采购订单）

| 项 | 内容 |
|----|------|
| **业务对象** | 采购订单 PO |
| **主实体** | `PurchaseOrder` |
| **主表** | `purchaseorder` |
| **级联软删** | `purchaseorderitem`（`PurchaseOrderItem`）、`purchaseorderitemextend`（`PurchaseOrderItemExtend`）、`purchaseorderextend`（`PurchaseOrderExtend`，若存在 1:1 行）。 |
| **关联但未必随主单删** | `purchaserequisition`（上游，不删）；`stockinnotify`（到货通知：见 §4，**默认由 PO/STOCK_IN 处理器校验或级联软删**，一期 **不单独** 暴露 Admin API）；供应商主数据。 |
| **库存与流水** | 若存在已关联入库 / 在库事实且 **已过账**，**禁止强删**；草稿链路由 `CanForceDelete` 精确状态判定。 |
| **CanForceDelete** | 无未软删入库单；无未冲销付款/核销（有则禁止）；到货通知未处于不可逆已入库阻塞（细则开发填）。 |
| **SyncAfterDelete** | 对涉及 `SellOrderItemId` 的行触发 `SellOrderItemExtend` 类重算（与现有 `PurchaseOrderService.DeleteAsync` 后 `RecalculateAsync` 对齐）。 |
| **实现备注** | `stockinnotify` / `qcinfo`：开发阶段核对 FK 后写入处理器（级联软删 **或** 仅阻断）。 |

---

## 4. BizType：`STOCK_IN_NOTIFY`（到货通知）— **一期不单独 API**

| 项 | 内容 |
|----|------|
| **业务对象** | 到货通知 |
| **主实体** | `StockInNotify` |
| **主表** | `stockinnotify` |
| **级联软删** | 单表；质检 `qcinfo` 等子实体以实现核对为准。 |
| **关联** | `purchaseorder` / `purchaseorderitem`；下游 `stock_in`。 |
| **库存与流水** | 不产生 `stockledger`；已驱动不可逆入库流程则 **禁止强删**。 |
| **CanForceDelete** | 无未软删关联 `stock_in`；无未冲销关联；状态未阻断。 |
| **SyncAfterDelete** | 回写采购/到货相关统计。 |
| **实现备注** | **一期 UI 不提供独立「强制删除到货通知」**；由 `PURCHASE_ORDER` / `STOCK_IN` 处理器在需要时 **级联软删** 或 **校验阻断**。 |

---

## 5. BizType：`STOCK_IN`（入库单）

| 项 | 内容 |
|----|------|
| **业务对象** | 入库单（含采购入库） |
| **主实体** | `StockIn` |
| **主表** | `stock_in` |
| **级联软删** | `stock_in_item`（`StockInItem`）、`stock_in_item_extend`（`StockInItemExtend`）、`stock_in_extend`（`StockInExtend`）、`stock_in_batch`（`StockInBatch`）。 |
| **关联** | `stockinnotify` / `qcinfo`；`purchaseorderitem`。 |
| **库存与流水** | 若已生成在库事实：`stock_item`、`stock`、`stock_extend`、`stockledger`。**已入库/已过账禁止强删**；仅 **草稿且无 `StockItem`、无 ledger** 链路由 `CanForceDelete` 放行（与总原则一致）。 |
| **CanForceDelete** | 见上；无未处理下游出库/拣货占用本 `StockItem`。 |
| **SyncAfterDelete** | 回写 `purchaseorderitem` / `purchaseorder` 入库进度；`SellOrderItemExtend` 等若受影响则重算。 |
| **实现备注** | 与 `InventoryCenterService` 过账语义对齐单元测试。 |

---

## 6. BizType：`STOCK_OUT_REQUEST`（出库申请 / 出库通知）

| 项 | 内容 |
|----|------|
| **业务对象** | 出库申请单 |
| **主实体** | `StockOutRequest` |
| **主表** | `stockoutrequest` |
| **级联软删** | **`pickingtask`**、**`pickingtaskitem`**。 |
| **关联** | `stock_out`；销售订单明细（不删）。 |
| **库存与流水** | 释放 `stock` 拣货占用等；对齐 `PickingTaskItem.stock_item_id`。 |
| **CanForceDelete** | **无未软删的已执行出库** `stock_out`；**`Status = 1`（已出库）一律禁止强删**；已过账/已核销禁止；拣货终态规则以实现为准写单测。 |
| **SyncAfterDelete** | 重算销售行可出库数量/申请占用；刷新 `stock` 占用。 |
| **实现备注** | 占用释放必须与现有拣货/出库服务一致，避免双删。 |

---

## 7. BizType：`PICKING_TASK`（拣货任务）— **仅级联，不单独 API**

| 项 | 内容 |
|----|------|
| **业务对象** | 拣货任务 |
| **主实体** | `PickingTask` |
| **主表** | `pickingtask` |
| **级联软删** | `pickingtaskitem`。 |
| **关联** | `stockoutrequest`；`stock_out.picking_task_id`。 |
| **库存与流水** | 同出库申请策略。 |
| **CanForceDelete** | **不提供独立强制删除 API**；随 `STOCK_OUT_REQUEST` / `STOCK_OUT` 处理。 |
| **实现备注** | 无 |

---

## 8. BizType：`STOCK_OUT`（出库单）

| 项 | 内容 |
|----|------|
| **业务对象** | 出库单 |
| **主实体** | `StockOut` |
| **主表** | `stock_out` |
| **级联软删** | `stock_out_item`、`stock_out_item_extend`。 |
| **关联** | `stockoutrequest`；`pickingtask`；销售明细。 |
| **库存与流水** | `StockOutItemExtend.StockItemId`、`stockledger`；**已出库/已过账禁止强删**。 |
| **CanForceDelete** | 仅草稿/未过账链路由细则锁定；有报关/物流 FK 则 **禁止**（开发核对后写入）。 |
| **SyncAfterDelete** | 销售明细扩展出库数量/利润；`stock`/`stock_item`；禁止删已过账时仅处理草稿场景。 |
| **实现备注** | 报关/物流外键扫描后补全 `CanForceDelete`。 |

---

## 9. 跨表公共项（所有涉及库存的 BizType）

| 实体 | DB 表 | 说明 |
|------|--------|------|
| `StockInfo` | `stock` | 分桶汇总；拣货占用、销售预占等字段可能需回滚。 |
| `StockExtend` | `stock_extend` | 与 `stock` 1:1；建议随 `stock` 级联软删。 |
| `StockItem` | `stock_item` | 在库明细；与 `StockInItem` 1:1；出库拣货强关联。 |
| `InventoryLedger` | `stockledger` | **已过账则禁止强删主单**；冲销须先走财务/库存两步人工。 |

---

## 10. 处理器注册建议（与「按 BizType 注册」一致）

| BizType 编码 | 建议独立 API | 备注 |
|--------------|--------------|------|
| `PURCHASE_REQUISITION` | 是 | 一期范围。 |
| `PURCHASE_ORDER` | 是 | 含对 `stockinnotify` 的校验/级联策略。 |
| `STOCK_IN_NOTIFY` | **否** | 并入 PO / 入库处理器。 |
| `STOCK_IN` | 是 | 含批次、扩展、在库层时须一致。 |
| `STOCK_OUT_REQUEST` | 是 | **必须**含拣货子表与占用释放。 |
| `PICKING_TASK` | **否** | 仅级联。 |
| `STOCK_OUT` | 是 | 含 `stock_out_item_extend` 与库存回写。 |
| `QUOTE`（跨期） | 是（建议） | 强删后 **清空** `sellorderitem.QuoteId` 并重算，见总原则 §7。 |

---

## 11. 开发落地检查清单（技术）

- [ ] 各 BizType `CanForceDelete` 与状态字段 **精确到枚举值** 并实现单测。  
- [ ] `stockinnotify` / `qcinfo` 与强删路径：**级联软删或阻断** 已实现并与文档一致。  
- [ ] 报关、物流、财务外键扫描结果写入处理器。  
- [ ] `log_operation` 的 `BizType` / `ActionType` 与约定一致；**强制删除** 请求校验 **单据号输入**。  
- [ ] 全局查询 `is_deleted = false`；报表/导出 **无**「含已删除」开关（一期）。  
- [ ] **后期**：`SYS_ADMIN` 回收站（与总原则 §3 一致）。

---

*维护：与 [强制删除_产品约定与总原则](./强制删除_产品约定与总原则.md) 同步；代码变更后回写本节。*
