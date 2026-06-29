# RFQ 明细状态（`rfqitem.status`）

## 权威定义

与代码枚举 **`CRM.Core.Models.RFQ.RfqItemStatus`**、实体注释及前端 **`CRM.Web/src/types/rfq.ts`** 中 `RFQItemStatus` 一致。

| 值 | 枚举名 | 含义 |
|----|--------|------|
| 0 | `Pending` | 待报价 |
| 1 | `Quoted` | 已报价 |
| 2 | `Accepted` | 已接受 |
| 3 | `Rejected` | 已拒绝 |
| 4 | `Closed` | 已关闭 |
| 5 | `NoQuoteFound` | **查无报价**（采购确认无货/无价，不通过本操作创建报价单） |

## 展示口径（列表 / 详情）

与 **`effectiveRfqItemLineStatus`**（`CRM.Web/src/utils/rfqItemLineStatus.ts`）一致：

| 库内 `status` | 报价条数 | 展示状态 | 说明 |
|---------------|----------|----------|------|
| 0 | 0 | 0 待报价 | 与库一致 |
| 0 | >0 | 1 已报价 | 待报价但有报价记录，展示为已报价 |
| 5 | 任意 | 5 查无报价 | **不被报价条数覆盖** |
| 1/2/3/4 | 任意 | 与库一致 | 按枚举文案展示 |

标签颜色（Element Plus `el-tag`）：待报价 `info`（灰）；查无报价 `warning`（黄）；其余默认 `primary`（蓝）。

## 与主单状态

明细状态变更（含查无报价 **0→5**）**不联动** `rfq.status` 主单状态。

## 相关文档

- 查无报价功能要求与实现：[需求明细查无报价-设计与实现](./需求明细查无报价-设计与实现.md)
- 需求主单状态：[RFQ 主状态枚举](./RFQ主状态枚举.md)

## 维护

若新增明细状态值：先改 **`RfqItemStatus`** 与本文档，再改前后端映射、列表筛选与 i18n（`rfqItemList.status.*`）。
