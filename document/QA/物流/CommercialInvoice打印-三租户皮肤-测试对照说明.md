# Commercial Invoice 打印 — 三租户皮肤 — 测试对照说明

> **设计文档：** [CommercialInvoice打印-三租户皮肤-设计与实现](../../System/物流/CommercialInvoice打印-三租户皮肤-设计与实现.md)  
> **色板：** [租户主题色规范](../../PRD/规范/UI规范/租户主题色规范.md)

---

## 1. 范围

- 分别在 **semicore / idesemi / ecoinf** 前端构建包（或对应 Vite mode）中打开：
  - 出库单 Invoice：`/inventory/stock-out/:id/invoice-report`
  - 或装箱 Invoice：`/inventory/packing/:packingId/invoice-report`
- 与同租户 Packing List 报表对照版式与主色。

---

## 2. 通过标准

| 检查项 | 期望 |
|--------|------|
| 租户差异 | 三套观感明显不同（不只换 Logo） |
| 与 Packing 对齐 | 同租户 Invoice 与 Packing 页眉结构、主色一致 |
| semicore | 深色顶栏（约 `#0D1F35`）+ 深色强调点缀 |
| idesemi | 左大标题「INVOICE」、右 Logo；青竖条点缀（约 `#6DC5F6`） |
| ecoinf | 绿表头/Bank 条（约 `#A8D070`）；公司名深色 |
| 字段 | 含单价、金额列；Bank Details；无 QC 清单 |
| 打印 | 壳层隐藏；色块/顶栏保留 |

任一套皮肤若与另两套「只差 Logo」→ 不通过。  
同租户 Invoice 若仍为旧橙色而 Packing 已为新色系 → 不通过。
