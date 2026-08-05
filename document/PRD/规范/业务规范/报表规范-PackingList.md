# 报表规范 — Packing List

## 1. 文档说明

| 项 | 内容 |
| --- | --- |
| 规范名称 | 报表规范-PackingList |
| 适用对象 | 装箱单 Packing List 打印 / 预览页 |
| 模版基准 | **三租户皮肤**（`semicore` / `idesemi` / `ecoinf`）；版式组件可对调，见 §2.3 |
| 关联总规范 | [Web 业务报表打印与导出规范](./Web业务报表打印与导出规范.md) |
| 实现说明 | [装箱单 PackingList 打印-三租户皮肤-设计与实现](../../../System/物流/装箱单PackingList打印-三租户皮肤-设计与实现.md) |
| 当前实现 | 页面 `StockOutPackingReportPage.vue` + `packingReport/skins/*`（按 `VITE_TENANT_ID` 选择） |

**原则**

- 报表 caption / label 由工具栏「中文/英文」切换（见 `getPackingReportLabels`）；**不**随系统 UI 语言自动切换。
- 业务数据（客户名、地址、物料、备注正文等）保持源数据语言。
- **数据字段与区块顺序**三租户一致；**视觉版式**按租户分叉，须像三家不同公司的单据。
- Logo / 公司名 / 印章 / 备注正文仍来自公司档案（部署数据），与皮肤选择无关。
- 修改实现时须同步更新本文档与 System 设计说明。

---

## 2. 路由与页面结构

### 2.1 路由

```
/inventory/packing/:packingId/packing-report/:packingInspection
```

| 参数 | 取值 | 含义 |
| --- | --- | --- |
| `packingId` | 装箱单主键 | 必填 |
| `packingInspection` | `with-inspection` / `without-inspection` | 是否含「Outbound Inspection」区块 |

### 2.2 组件职责

| 文件 | 职责 |
| --- | --- |
| `CRM.Web/src/views/Inventory/StockOutPackingReportPage.vue` | 拉数、工具栏、数据映射；`<component :is="packingReportSkin">` |
| `CRM.Web/src/components/stockOut/packingReport/resolvePackingReportSkin.ts` | `LOGIN_TENANT_ID` → 皮肤组件 |
| `CRM.Web/src/components/stockOut/packingReport/types.ts` | 三皮肤共用 props |
| `CRM.Web/src/components/stockOut/packingReport/skins/PackingReportSkinSemicore.vue` | 橙表版式（挂到 ecoinf 租户） |
| `CRM.Web/src/components/stockOut/packingReport/skins/PackingReportSkinIdesemi.vue` | 深紫/琥珀（挂到 semicore 租户） |
| `CRM.Web/src/components/stockOut/packingReport/skins/PackingReportSkinEcoinf.vue` | 工业极简（挂到 idesemi 租户） |
| `CRM.Web/src/components/stockOut/StockOutPackingReportDocument.vue` | 兼容入口（转发 Semicore） |
| `CRM.Web/src/components/stockOut/packingReportLabels.ts` | 中/英 label |
| `CRM.Web/src/assets/styles/print-purchase-order.scss` | 打印隐藏壳层 + `print-color-adjust` |

### 2.3 三租户皮肤要点

| 租户 | 根 class | 视觉要点 |
| --- | --- | --- |
| `semicore` | `.po-doc--idesemi`（组件 `PackingReportSkinIdesemi`） | 深紫顶栏 + 琥珀细线；Meta 右侧卡片；表头深紫底琥珀底边；地址栏左竖条无色块 |
| `idesemi` | `.po-doc--ecoinf`（组件 `PackingReportSkinEcoinf`） | 大标题左上追踪字距、Logo 右上；青 `#6DC5F6` section 竖条；无色块表头 + 斑马纹；QC 清单式 checkbox |
| `ecoinf` | `.po-doc--semicore`（组件 `PackingReportSkinSemicore`） | 橙 `#e5913e` 表头/分区条；Logo 左 + 公司/标题居中（§3–§4 详述原橙表规范） |

字段列（No / PN / Brand / Qty / Carton / Remark）与 QC 五项文案三皮肤共用。

---

## 3. 纸张与全局版式（橙表 / Semicore 组件基准）

以下为 **橙表组件**（`PackingReportSkinSemicore`，当前挂到 `ecoinf`）版式规范；另两套见 §2.3 与 System 设计文档，纸张仍为 A4。

| 项 | 规范值 |
| --- | --- |
| 纸张 | A4 竖版 |
| 文档根 `.po-doc` 宽度 | `210mm` |
| 最小高度 | `297mm` |
| 页边距（屏幕预览） | 上 `10mm`、左右 `12mm`、下 `14mm` |
| 页边距（`@media print`） | `8mm 10mm` |
| 正文字号 | `10pt` |
| 行高 | `1.5` |
| 字体 | `'Microsoft YaHei', 'SimHei', 'SimSun', system-ui, sans-serif` |
| 主色（表头背景） | `#e5913e`（橙色） |
| 边框色 | `#222` |
| 正文色 | `#111` |

---

## 4. 页眉（Masthead · Semicore）

### 4.1 布局

```
┌──────────┬────────────────────────────────────┐
│  Logo    │     公司名称（居中、加粗）            │
│  36mm    │     仓库地址（可选，居中）            │
│          │     【空白行 1.5em】                 │
│          │     PACKING LIST（居中）             │
└──────────┴────────────────────────────────────┘
Date / Packing No. / Shipping Method（左对齐，位于标题下方）
【空白行 1.5em】
```

### 4.2 样式明细

| 元素 | 字号 | 其他 |
| --- | --- | --- |
| 公司名称 `.po-doc__masthead-company` | `16pt` | `font-weight: 700`，字距 `0.02em` |
| 仓库地址 | `10pt` | 颜色 `#333` |
| 标题 `.po-doc__masthead-title` | **17pt** | 字距 `0.2em`，`text-indent: 0.2em` |
| 标题上方空白 `.po-doc__masthead-title-gap` | — | 高度 `1.5em` |
| 元信息区 `.po-doc__masthead-meta` | `10pt` | 左对齐，行高 `1.65` |
| 元信息区下方空白 `.po-doc__masthead-meta-gap` | — | 高度 `1.5em` |
| Logo | 最大 `14mm × 28mm` | `object-fit: contain` |
| Label 加粗 `.po-doc__k` | — | `font-weight: 600` |

### 4.3 元信息字段

| Label（英文固定） | 数据来源 |
| --- | --- |
| `Date: ` | 出库日期 `stockOut.stockOutDate`，格式 `YYYY-MM-DD` |
| `Packing No.: ` | 装箱单编号 `packing.code` |
| `Shipping Method: ` | 装箱单 `deliveryMethod`：`10 → Delivery`，`20 → Self Pick-up`；缺省回退出库单 `shipmentMethod` 文本 |

---

## 5. Bill To / Ship To 地址块

### 5.1 表头

两列等宽，表头文案：**Bill To**、**Ship To**（英文固定）。

### 5.2 单元格行结构（每列 4 行）

| 行序 | 内容 | Label |
| --- | --- | --- |
| 1 | 客户名称 | 无（直接显示） |
| 2 | 地址 | 无 |
| 3 | 联系人 | 前缀 **`Attn: `** |
| 4 | 电话 | 前缀 **`Tel: `** |

示例：

```
彩晶光电科技(昆山)有限公司
这里是账单地址
Attn: 张三
Tel: 13800138000
```

### 5.3 数据来源

| 字段 | 后端来源 |
| --- | --- |
| 客户名称 | 出库单 `customerName`（销售敏感字段权限下显示 `—`） |
| Bill 地址 / 联系人 / 电话 | `packing_extend_ship.bill_address / bill_attn / bill_tel` |
| Ship 地址 / 联系人 / 电话 | `packing_extend_ship.ship_address / ship_attn / ship_tel` |

空值占位：组装阶段无内容时用 `—`；第 3、4 行仍保留 `Attn: ` / `Tel: ` 前缀。

---

## 6. 明细表（Item Grid）

### 6.1 列定义

| 列 | 表头（英文） | 宽度 | 对齐 |
| --- | --- | --- | --- |
| 序号 | `No.` | 8% | 居中 |
| 料号 | `PN` | 24% | 左（默认） |
| 品牌 | `Brand` | 24% | 左 |
| 数量 | `Qty` | 12% | 右 |
| 箱号 | `Carton` | 12% | 居中 |
| 备注 | `Remark` | 20% | 左 |

表头：橙色底 `#e5913e`，字号 `8.6pt`，加粗，居中，单行显示（**PN / Brand 表头不换行**）。

单元格：`padding 4px 5px`，`word-break: break-all`，垂直居中。

### 6.2 PN / Brand 双行显示（核心规范）

**表头仅一行**；**数据区** PN、Brand 列采用双行结构：

```
┌─────────────────┐
│ IMX577-AACK-C   │  ← 第 1 行：物料 PN / Brand（正常样式）
│ CUST-PN-001     │  ← 第 2 行：Customer PN / Customer Brand（可选）
└─────────────────┘
```

| 行 | 字段 | 样式类 | 规则 |
| --- | --- | --- | --- |
| 第 1 行 | `pn` / `brand` | 默认 | 无值显示 `—` |
| 第 2 行 | `customerPn` / `customerBrand` | `.po-doc__cell-sub` | **有值才渲染**（`v-if`）；无值整行省略，不占位 |
| 第 2 行样式 | — | — | 字号 `9pt`，*斜体*，颜色 `#666`，上边距 `2px` |

**数据来源（与装箱单详情一致）**

| 字段 | 优先级 |
| --- | --- |
| `pn` / `brand` | `packing_item` 物料值 |
| `customerPn` / `customerBrand` | `packing_item_extend` → 缺省回退关联 `sell_order_item` |
| `qty` | `packing_item.qty`，千分位格式，最多 4 位小数 |
| `carton` | 预留；无值 **留空** |
| `remark` | `packing_item.comment`；无值 **留空** |

### 6.3 空值规则（明细列）

| 字段 | 无值时 |
| --- | --- |
| PN / Brand（第 1 行） | `—` |
| Customer PN / Customer Brand | 不显示第 2 行 |
| Carton / Remark | **留空**（不显示 `—`） |

### 6.4 填充行与合计

| 区块 | 规则 |
| --- | --- |
| 空白填充行 | 明细少于 5 行时，补空行至共 5 行数据行（仅空格，无边框内容） |
| 「以下空白」行 | 有明细时显示一行 **`Blank below`**，跨 6 列居中 |
| 无明细 | 显示 **`No items`**，不显示 Blank below / Total |
| 合计行 | 首列 **`Total`**，Qty 列合计；加粗 |

---

## 7. 出货检验区块（可选变体）

**路由参数** `with-inspection` 时显示。

| 项 | 规范 |
| --- | --- |
| 区块标题 | `Outbound Inspection`（橙色 bar） |
| 表格列 | `No.` / `Item` / `Result` |
| 检验项 | 固定 5 条英文描述（见 `packingReportLabels.ts` → `qcItems`） |
| 表头背景 | 橙色 35% 透明度 |
| 页脚 | 左 `Inspector: `，右 `Inspection Date: `（留空待手填） |

---

## 8. 备注（Remarks）

| 项 | 规范 |
| --- | --- |
| 标题 bar | `Remarks` |
| 正文来源 | **固定读取英文** sysparam：`Company.Profile.ReportInfo.PackingList.Remark.EN` |
| 配置入口 | 系统 → 公司信息 → 报表信息 → Packing 报表 → 备注(英文) |
| 分行 | 按换行符拆分为多行；空行忽略 |
| 正文样式 | 字号 `8.8pt`，行高 `1.45`，两端对齐 |

> 中文备注参数 `…Remark.CN` 仅用于后台维护，**不出现在 Packing List 打印件**。

---

## 9. 签章区（Sign）

### 9.1 布局（3 行 × 2 列 Grid）

```
Shipper (Signature/Stamp)     Consignee (Signature/Stamp)
[签章区 26mm 高]               [空白签章区 26mm 高]
Date: 2026-05-24               Date:
```

| 规则 | 说明 |
| --- | --- |
| 左右列等宽 | `grid-template-columns: 1fr 1fr`，列间距 `12mm` |
| 签章区高度 | 左右均为 `min-height: 26mm`，保证日期行垂直对齐 |
| Shipper 印章 | 公司默认印章图，左下对齐；最大 `26mm × 32mm`；可通过工具栏开关隐藏 |
| Consignee 日期 | 仅 label `Date: `，值留空 |
| Shipper 日期 | `Date: ` + 出库日期（`YYYY-MM-DD`） |
| 对齐 | Consignee 区块 **左对齐**（标题与日期均与左缘对齐） |

---

## 10. 固定英文 Label 清单

实现文件：`CRM.Web/src/components/stockOut/packingReportLabels.ts`

| Key | 文案 |
| --- | --- |
| `docTitle` | `PACKING LIST` |
| `date` | `Date: ` |
| `packingNo` | `Packing No.: ` |
| `shipMethod` | `Shipping Method: ` |
| `no` | `No.` |
| `noItems` | `No items` |
| `blankBelow` | `Blank below` |
| `total` | `Total` |
| `remarks` | `Remarks` |
| `attn` | `Attn: ` |
| `tel` | `Tel: ` |
| `shipperSign` | `Shipper (Signature/Stamp)` |
| `consigneeSign` | `Consignee (Signature/Stamp)` |
| `outboundInspection` | `Outbound Inspection` |
| `item` / `result` | `Item` / `Result` |
| `qcInspector` / `qcDate` | `Inspector: ` / `Inspection Date: ` |

---

## 11. 数据接口

### 11.1 主接口（推荐）

```
GET /api/v1/packing/{packingId}/packing-report-bundle?withInspection={bool}
```

返回 `StockOutPackingReportBundle`：

- `stockOut` — 出库单概要
- `companyProfile` — 公司名、Logo、印章、报表备注等
- `packingCode` / `packingAddresses` / `deliveryMethod` / `warehouseAddress`
- `packingLines[]` — 明细行（PN / CustomerPn / Brand / CustomerBrand / Qty / …）

### 11.2 兼容接口

```
GET /api/v1/stock-out/{id}/packing-report-bundle?withInspection={bool}
```

若 bundle 未带 `packingLines`，前端降级调用 `GET /api/v1/packing/{id}` 取装箱单明细。

### 11.3 公司资料

| 用途 | 来源 |
| --- | --- |
| 页眉公司名 | `companyProfile.basicInfos` 默认项 |
| Logo | 默认且含文档的 logo 记录 |
| 印章 | 默认且含文档的 seal 记录 |
| 页脚备注 | `companyProfile.reportInfo.packingList.remarkEn` |

---

## 12. 打印实现要点

1. 挂载报表页时 `body.classList.add('po-order-report-print')`，卸载时移除。
2. 工具栏、Loading 带 `no-print`；打印目标为 `.po-doc`，非 `.print-root` 外框。
3. 表头橙色背景在 `print-purchase-order.scss` 中设置 `print-color-adjust: exact`。
4. 预览外框 `.print-root` 屏幕态灰底 `#525659`，打印时白底无 padding。

---

## 13. 变更检查清单

修改 Packing List 报表时，请核对：

- [ ] caption / label 是否仍为英文且集中在 `packingReportLabels.ts`
- [ ] PN / Brand 双行：第 1 行主值、第 2 行 Customer 值、子行样式 `#666` 斜体
- [ ] Carton / Remark / Customer 子行空值是否仍「留空不显示 —」
- [ ] Bill To / Ship To 第 3、4 行是否仍有 `Attn: ` / `Tel: `
- [ ] 备注是否仍只读 `Remark.EN`
- [ ] 签章区 3×2 Grid 与日期行对齐
- [ ] 本文档是否已同步更新

---

## 14. 参考文件索引

| 类型 | 路径 |
| --- | --- |
| 报表页 | `CRM.Web/src/views/Inventory/StockOutPackingReportPage.vue` |
| 报表文档 | `CRM.Web/src/components/stockOut/StockOutPackingReportDocument.vue` |
| 英文 Label | `CRM.Web/src/components/stockOut/packingReportLabels.ts` |
| 打印样式 | `CRM.Web/src/assets/styles/print-purchase-order.scss` |
| Bundle 加载 | `CRM.API/Services/PackingReportBundleLoader.cs` |
| DTO | `CRM.API/Models/DTOs/StockOutPackingReportDtos.cs` |
| 明细 Customer 回退 | `CRM.Infrastructure/Packings/PackingService.cs` |
| 备注参数 | `CRM.API/Constants/CompanyProfileParamCodes.cs` |

---

**维护**：版式或字段规则变更时，由改动的开发者在同一 PR 内更新本文档对应章节。
