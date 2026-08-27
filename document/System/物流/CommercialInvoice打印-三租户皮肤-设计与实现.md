# Commercial Invoice 打印 — 三租户皮肤

**状态：** 已上线  
**页面：** `/inventory/stock-out/:id/invoice-report`、`/inventory/packing/:packingId/invoice-report`  
**色板：** [租户主题色规范](../../PRD/规范/UI规范/租户主题色规范.md)  
**对照：** [装箱单 PackingList 打印-三租户皮肤-设计与实现](./装箱单PackingList打印-三租户皮肤-设计与实现.md)  
**QA：** [CommercialInvoice打印-三租户皮肤-测试对照说明](../../QA/物流/CommercialInvoice打印-三租户皮肤-测试对照说明.md)

---

## 1. 目标

三租户构建产物打印 Commercial Invoice 时，**版式语言与配色**与同租户 Packing List 一致（同一套组件映射与主题色），避免 Invoice 仍停留在旧橙表、与 Packing 观感割裂。

非目标：财务模块销项/进项发票 CRUD 页；PO 报表皮肤。

---

## 2. 架构

| 层 | 说明 |
|----|------|
| 租户来源 | `VITE_TENANT_ID` → `LOGIN_TENANT_ID` |
| 皮肤选择 | `resolveInvoiceReportSkin(tenantId, styleVersion)`；V2 仅 semicore，见 [报表规范-Invoice-V2](../../PRD/规范/业务规范/报表规范-Invoice-V2.md) |
| 数据 | 既有 `docBind`（明细含单价/金额、Bank Details）；不变 |
| 版式 | V1：三套 Vue 皮肤；V2：`InvoiceReportV2Body` + `InvoiceReportV2SkinIdesemi` |

```
StockOutInvoiceReportPage
  → reportParamsApi.getEffectiveStyleVersion()
  → resolveInvoiceReportSkin(LOGIN_TENANT_ID, styleVersion)
  → V1: InvoiceReportSkin{Semicore|Idesemi|Ecoinf}
  → V2 (semicore): InvoiceReportV2SkinIdesemi
```

---

## 3. 关键文件

| 路径 | 职责 |
|------|------|
| `CRM.Web/src/components/stockOut/invoiceReport/types.ts` | props / 补空行 |
| `CRM.Web/src/components/stockOut/invoiceReport/resolveInvoiceReportSkin.ts` | 租户 → 组件 |
| `.../skins/InvoiceReportSkinSemicore.vue` | 绿表（挂到 `ecoinf`） |
| `.../skins/InvoiceReportSkinIdesemi.vue` | 深色顶栏（挂到 `semicore`） |
| `.../skins/InvoiceReportSkinEcoinf.vue` | 工业极简（挂到 `idesemi`） |
| `CRM.Web/src/views/Inventory/StockOutInvoiceReportPage.vue` | `<component :is>` |
| `CRM.Web/src/components/stockOut/StockOutInvoiceReportDocument.vue` | 兼容入口（转发 Semicore） |

> 组件文件名保留原命名；映射与 Packing 对齐（见 `resolveInvoiceReportSkin.ts`）。

---

## 4. 皮肤差异摘要

| 租户 | 实际皮肤组件 | 主色 / 背景 / 强调（主题规范） | 页眉 | 表头 / 分区 |
|------|--------------|--------------------------------|------|-------------|
| semicore | Idesemi | `#B3EEF6` / `#0D1F35` / `#020612` | 深色顶栏 + 强调线；Meta 右卡片 | 深色表头 + 强调底边；地址左竖条 |
| idesemi | Ecoinf | `#6DC5F6` / `#FFFFFF` / `#11161F` | 大标题左、Logo 右；底边双线 | 无色块表头 + 斑马纹；分区青竖条 |
| ecoinf | Semicore | `#A8D070` / `#FFFFFF` / `#101010` | Logo 左、公司/标题居中 | 绿色填充表头与 Bank Details 条 |

区块顺序一致：页眉 → Bill/Ship → 明细（含单价/金额）→ Bank Details → 签字。

与 Packing 差异：无 QC / Remarks；明细为 7 列（含 UP / Amount）；addon 为银行资料。

---

## 5. 验证要点

- 同一租户下 Invoice 与 Packing List 页眉/表头色系、版式语言一致。
- 三租户分别构建预览，观感明显不同。
- 金额权限掩码、印章开关、中英文 label 三皮肤均可用。
