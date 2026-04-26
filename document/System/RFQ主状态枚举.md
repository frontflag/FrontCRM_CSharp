# RFQ 主状态（`rfq.status`）

## 权威定义

与代码枚举 **`CRM.Core.Models.RFQ.RfqMainStatus`** 及迁移 **`20260426180000_NormalizeRfqStatus6To7AndComment`** 一致。

| 值 | 枚举名 | 含义 |
|----|--------|------|
| 0 | `PendingAssign` | 待分配 |
| 1 | `Assigned` | 已分配 |
| 2 | `Quoting` | 报价中 |
| 3 | `Quoted` | 已报价 |
| 4 | `PriceSelected` | 已选价 |
| 5 | `ConvertedToOrder` | 已转订单 |
| 6 | `LegacyObsoleteClosed` | **已废弃**：历史「已关闭」占位码；**勿新写入**。服务层将写入的 `6` 归一为 `7`；存量由迁移 `UPDATE … SET status=7 WHERE status=6` 统一。 |
| 7 | `Closed` | **已关闭（终态）**：已生成销售订单，需求完成使命。 |
| 8 | `Cancelled` | **已取消（终态）**：不再继续执行，终止使命；**保留记录留痕**。 |

## 与删除规则（产品约定）

- 普通删除：状态 **0、8** 可删；**7** 不可删（见删除方案讨论稿）。
- 分配采购员等操作：终态 **7、8** 禁止（见 `RFQService.AssignPurchaserAsync`）。

## 前端

- 列表筛选与展示文案：`rfqList.status.*`（含 `closed`=`7`、`cancelled`=`8`）。
- TypeScript 枚举：`CRM.Web/src/types/rfq.ts` 中 `RFQStatus`，与后端一一对应。

## 维护

若新增状态值：先改 **`RfqMainStatus`** 与本文档，再改前后端映射与迁移注释。
