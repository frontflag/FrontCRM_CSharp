# 装箱单 Packing List 打印 — 三租户皮肤

**状态：** 已上线  
**页面：** `/inventory/packing/:packingId/packing-report/:packingInspection`  
**PRD：** [报表规范-PackingList](../../PRD/规范/业务规范/报表规范-PackingList.md)  
**QA：** [装箱单PackingList打印-三租户皮肤-测试对照说明](../../QA/物流/装箱单PackingList打印-三租户皮肤-测试对照说明.md)

---

## 1. 目标

三租户构建产物（`semicore` / `idesemi` / `ecoinf`）打印 Packing List 时，除公司档案中的 Logo / 公司名外，**版式语言**须明显不同，避免「换 Logo 的同一张单」。

非目标：装箱业务列表页皮肤；采购订单报表皮肤。Invoice / 销售订单报表已按同一注册表模式落地，见 [CommercialInvoice打印-三租户皮肤-设计与实现](./CommercialInvoice打印-三租户皮肤-设计与实现.md)、[销售订单报表打印-三租户皮肤-设计与实现](../销售/销售订单报表打印-三租户皮肤-设计与实现.md)。

---

## 2. 架构

| 层 | 说明 |
|----|------|
| 租户来源 | 构建时 `VITE_TENANT_ID` → `LOGIN_TENANT_ID`（`loginTenant.ts`） |
| 皮肤选择 | `resolvePackingReportSkin(tenantId)` |
| 数据 | 共用 `packing-report-bundle` + 公司档案；`docBind` 不变 |
| 版式 | 三套 Vue 皮肤，共享 `PackingReportDocumentProps` |

```
StockOutPackingReportPage
  → resolvePackingReportSkin(LOGIN_TENANT_ID)
  → PackingReportSkin{Semicore|Idesemi|Ecoinf}
```

---

## 3. 关键文件

| 路径 | 职责 |
|------|------|
| `CRM.Web/src/components/stockOut/packingReport/types.ts` | props / 补空行工具 |
| `CRM.Web/src/components/stockOut/packingReport/resolvePackingReportSkin.ts` | 租户 → 组件 |
| `.../skins/PackingReportSkinSemicore.vue` | 橙表经典（当前挂到 `ecoinf` 租户） |
| `.../skins/PackingReportSkinIdesemi.vue` | 深紫 + 琥珀（当前挂到 `semicore` 租户） |
| `.../skins/PackingReportSkinEcoinf.vue` | 工业极简 + 青点缀（当前挂到 `idesemi` 租户） |
| `CRM.Web/src/views/Inventory/StockOutPackingReportPage.vue` | `<component :is>` |
| `CRM.Web/src/assets/styles/print-purchase-order.scss` | 打印壳层 + 多皮肤 `print-color-adjust` |

> 租户映射：`semicore` ↔ `ecoinf` 皮肤已对调；`idesemi` 使用 Eco 工业极简组件（见 `resolvePackingReportSkin.ts`）。组件文件名仍保留原命名。

---

## 4. 皮肤差异摘要

| 租户 | 实际皮肤组件 | 主色 | 页眉 | 表头 / 分区 |
|------|--------------|------|------|-------------|
| semicore | Idesemi | `#2d1b4e` + `#f59e0b` | 深紫顶栏 + 琥珀线；Meta 右卡片 | 深紫表头琥珀底边；地址左竖条 |
| idesemi | Ecoinf | `#6DC5F6` / `#11161F` 点缀 | 大标题左、Logo 右；底边双线 | 无色块表头 + 斑马纹；QC 清单 checkbox |
| ecoinf | Semicore | `#e5913e` | Logo 左、公司/标题居中 | 橙色填充表头与 addon 条 |

区块顺序一致：页眉 → Bill/Ship → 明细 → 可选 QC → Remarks → 签字。

---

## 5. 验证要点

- 分别以 `production.semicore` / `idesemi` / `ecoinf` 构建或本地 `--mode` 打开同一装箱单报表，三套观感明显不同。
- 打印预览：壳层隐藏，色块/顶栏保留。
- with / without inspection、中英文、印章开关三皮肤均可用。
- Semicore 与历史橙表观感一致（无回归）。
