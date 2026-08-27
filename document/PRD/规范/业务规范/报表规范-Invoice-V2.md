# 报表规范 — Commercial Invoice **V2**

> **版本标识：V2（与 V1 不是同一套版式）**  
> V1 为现网三租户竖版皮肤，本文**不约束、不改写 V1**。V1 实现见 [CommercialInvoice打印-三租户皮肤-设计与实现](../../../System/物流/CommercialInvoice打印-三租户皮肤-设计与实现.md)。  
> 改 V2 观感时先改本文，再改 `InvoiceReportV2Body.vue` / V2 皮肤变量。  
> 配色与分区 chrome 对齐 [报表规范-装箱单-V2](./报表规范-装箱单-V2.md)（藏青 `#090e1d` / 青 `#00d2ef`）。

| 项 | 内容 |
|----|------|
| 状态 | 已上线（semicore 消费） |
| 规范名称 | 报表规范-Invoice-V2 |
| 适用对象 | Commercial Invoice 打印 / 预览页，且全局参数 `System.Report.StyleVersion = V2`，且租户为 **semicore** |
| 模版基准 | 无独立 PDF；chrome 对齐装箱/PO V2，业务字段对齐 V1 |
| 根 class | `.po-doc.po-doc--v2.po-doc--inv-v2` |
| 页面 | `/inventory/stock-out/:id/invoice-report`、`/inventory/packing/:packingId/invoice-report` |

---

## 1. 适用范围

| 条件 | 版式 |
|------|------|
| 未配置 / 非法值 / 参数 **V1** | **V1**（现网三租户皮肤） |
| 参数 **V2** 且租户 **semicore** | **V2**（本文） |
| 参数 **V2** 且租户 **idesemi / ecoinf** | 仍为该租户 **V1** |
| 销售订单 | 另见 [报表规范-销售订单-V2](./报表规范-销售订单-V2.md) |

切换参数后须**重新打开或刷新**打印页。Logo / 抬头 / 印章仍走公司信息。

---

## 2. V1 与 V2 对照（摘要）

| 项 | V1 | V2 |
|----|----|-----|
| 顶栏 | 租户深色顶栏 + Meta 卡片 | 藏青通栏 + 渐变线 + 2 格元数据条 |
| 地址 | Bill To / Ship To 多行 | 同语义，V2 双栏框 + 分区标题「账单与收货 / BILL & SHIP」 |
| 明细 | 7 列 + 表内合计 | 同列定义；藏青表头中英两行 |
| 银行 / 签章 | V1 分区 | V2 分区 chrome；签章仍为出口方 / 收货方 |
| 语言工具栏 | 中/英切换 | **保留**；壳子固定双语，切换影响 Bill/Ship 栏名、银行/签章/合计等 `labels` |

---

## 3. 实现文件

| 路径 | 职责 |
|------|------|
| `CRM.Web/src/components/stockOut/invoiceReport/InvoiceReportV2Body.vue` | V2 版式主体 |
| `.../skins/InvoiceReportV2SkinIdesemi.vue` | semicore 薄壳 + CSS 变量 |
| `.../resolveInvoiceReportSkin.ts` | 租户 + 样式版本 → 组件 |
| `CRM.Web/src/views/Inventory/StockOutInvoiceReportPage.vue` | 读 `getEffectiveStyleVersion()` |

---

## 4. 验证要点

- semicore + 参数 V2：藏青 chrome、2 格 meta、Bill To/Ship To、7 列明细、银行与双签章。
- 参数 V2 + idesemi/ecoinf：仍为 V1 三皮肤。
- 工具栏中/英切换在 V2 下仍可用；顶栏与 meta 键名保持中英对照。
- 出库 Invoice 与装箱 Invoice 两入口版式一致。
