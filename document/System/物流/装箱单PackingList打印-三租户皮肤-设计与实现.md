# 装箱单 Packing List 打印 — 三租户皮肤

**状态：** 已上线  
**页面：** `/inventory/packing/:packingId/packing-report/:packingInspection`  
**PRD（V1）：** [报表规范-PackingList](../../PRD/规范/业务规范/报表规范-PackingList.md)  
**PRD（V2）：** [报表规范-装箱单-V2](../../PRD/规范/业务规范/报表规范-装箱单-V2.md)（仅 semicore + 参数 V2；竖版 + 横版）  
**QA：** [装箱单PackingList打印-三租户皮肤-测试对照说明](../../QA/物流/装箱单PackingList打印-三租户皮肤-测试对照说明.md)  
**报表参数：** [报表参数-设计与实现](../系统管理/报表参数-设计与实现.md)

---

## 1. 目标

三租户构建产物（`semicore` / `idesemi` / `ecoinf`）打印 Packing List 时，除公司档案中的 Logo / 公司名外，**版式语言**须明显不同，避免「换 Logo 的同一张单」。

全局参数 `System.Report.StyleVersion = V2` 时：

| 条件 | 版式 |
|------|------|
| semicore + **竖版** | V2 9 列中英（藏青/青，见装箱单 V2 规范） |
| semicore + **横版** | V2 14 列英文表头（图1） |
| idesemi / ecoinf | 该租户 V1，与参数 V1 时相同 |

非目标：装箱业务列表页皮肤；为 V2 改 V1 皮肤文件。Invoice / 销售订单仍不读样式版本。采购订单 V2 见对应规范。

---

## 2. 架构

| 层 | 说明 |
|----|------|
| 租户来源 | 构建时 `VITE_TENANT_ID` → `LOGIN_TENANT_ID` |
| 方向 | 工具栏竖/横；默认横版；`localStorage` 键 `frontcrm.packingReport.orientation` |
| 样式版本 | `GET /api/v1/report-params/effective-style-version`（登录即可；非法/空 → V1） |
| 皮肤选择 | `resolvePackingReportView(orientation, tenantId, styleVersion)` |
| 数据 | 共用 `packing-report-bundle`；V1 竖/横与 V2 各自 `docBind` |
| 版式 | V1 竖版三套皮肤；V1 横版单组件 + `theme`；V2 仅 `PackingReportV2SkinIdesemi` |

```
StockOutPackingReportPage
  → GET effective-style-version（无参数管理权限也可）
  → GET packing-report-bundle
  → resolvePackingReportView(orientation, LOGIN_TENANT_ID, styleVersion)
  → landscape + V2 + semicore: PackingReportV2LandscapeSkinIdesemi
  → landscape 其它: PackingReportLandscapeDocument（V1）
  → portrait + V2 + semicore: PackingReportV2SkinIdesemi
  → 否则: PackingReportSkin{Semicore|Idesemi|Ecoinf}
```

横版行字段扩展：`CustomerPo`、`Dc`、`Co`（`packing_item.CO`）、`Cod`、`Size`/`Nw`/`Gw`（可空）、`Carton`。DC/COD 由 `PackingReportBundleLoader.EnrichPackingLineBatchFieldsAsync` 从拣货→库存→批次聚合（多值 `, ` 拼接）。

V2 竖版字段映射见装箱单 V2 规范 §5（发货人=我方，收货人=客户；空 carton → `01` 行序；检验块仍跟 `with-inspection`）。

---

## 3. 关键文件

| 路径 | 职责 |
|------|------|
| `CRM.Web/src/components/stockOut/packingReport/types.ts` | 竖/横/V2 props、方向读写、`formatPackingV2Carton` |
| `CRM.Web/src/components/stockOut/packingReport/resolvePackingReportSkin.ts` | 租户 → 竖版组件 / 横版主题；V2 仅 semicore 竖+横 |
| `.../PackingReportLandscapeDocument.vue` | A4 横版 14 列明细（**不改版式迁就 V2**） |
| `.../skins/PackingReportSkinSemicore.vue` | 竖版橙表经典（当前挂到 `ecoinf`） |
| `.../skins/PackingReportSkinIdesemi.vue` | 竖版深紫 + 琥珀（当前挂到 `semicore` 的 V1） |
| `.../skins/PackingReportSkinEcoinf.vue` | 竖版工业极简 + 青点缀（当前挂到 `idesemi`） |
| `.../PackingReportV2Body.vue` | V2 竖/横共用正文（横版 14 列） |
| `.../skins/PackingReportV2SkinIdesemi.vue` | V2 竖版变量（挂到 semicore） |
| `.../skins/PackingReportV2LandscapeSkinIdesemi.vue` | V2 横版根 class |
| `CRM.Web/src/views/Inventory/StockOutPackingReportPage.vue` | `<component :is>` + 方向/语言/生效版本 |
| `CRM.API/Services/PackingReportBundleLoader.cs` | 行字段映射 + DC/COD enrichment |
| `CRM.Web/src/assets/styles/print-purchase-order.scss` | 打印壳层；`@page packing-landscape`；V2 `print-color-adjust` |

> 租户映射：`semicore` ↔ `ecoinf` 皮肤已对调；`idesemi` 使用 Eco 工业极简组件。组件文件名仍保留原命名。横版 `theme` 使用同一映射。

---

## 4. 皮肤差异摘要（V1）

| 租户 | 实际皮肤组件 | 主色 | 页眉 | 表头 / 分区 |
|------|--------------|------|------|-------------|
| semicore（参数 V1，或 V2 但横版） | Idesemi | `#2d1b4e` + `#f59e0b` | 深紫顶栏 + 琥珀线；Meta 右卡片 | 深紫表头琥珀底边；地址左竖条 |
| idesemi | Ecoinf | `#6DC5F6` / `#11161F` 点缀 | 大标题左、Logo 右；底边双线 | 无色块表头 + 斑马纹；QC 清单 checkbox |
| ecoinf | Semicore | `#e5913e` | Logo 左、公司/标题居中 | 橙色填充表头与 addon 条 |

V1 区块顺序：页眉 → Bill/Ship → 明细 → 可选 QC → Remarks → 签字。

semicore V2 竖版：页眉 → 元数据 → 发货人/收货人 → 包装明细 → 可选检验 → 运输+汇总 → 声明 → 签章。观感对齐采购订单 V2 藏青/青，**不要**再要求与上表 Idesemi 顶栏同构。

---

## 5. 验证要点

- 分别以 `production.semicore` / `idesemi` / `ecoinf` 构建或本地 `--mode` 打开同一装箱单报表，V1 三套观感明显不同（竖版与横版主题均核对）。
- 参数 V2：仅 semicore 竖/横换 PDF 版式；idesemi / ecoinf 不变。
- 默认进入为横版；semicore + V2 横版为 14 列英文表头，竖版为 9 列中英。
- 横版 CO 列：`packing_item.CO` 有值则显示，无值留空；DC/COD 多批次逗号拼接。
- 打印预览：壳层隐藏，色块/顶栏保留。
- with / without inspection：V1 竖/横与 V2 竖版均按路由显隐检验区。
- V1 中英文、印章开关可用；V2 隐藏中/英切换，印章开关仍可用。
- V2 签章（竖/横共用 `PackingReportV2Body`）：发货人、收货人「日期 / Date」均为下划线，不印出库日期。
- V2 顶栏（竖/横相同）：「装箱单」16pt、「PACKING LIST」6.5pt、「装箱单号码 / Packing List No.」7pt；左侧 Logo 与标语 `YOUR RELIABLE SUPPLIER` 水平居中对齐。
- Semicore V1 竖版与历史深紫/琥珀观感一致（无回归）。
