# 列表扩展列规范 PRD（V1.1 · 已实现）

## 1. 文档目的

统一业务列表中**「客户」复合信息列**（Extend Column）的**功能、交互、样式与接入方式**。在**不长期占满三列横向空间**的前提下，支持：

- **收起态**：只显示一个子字段（可切换中文名 / 英文名 / 编号）；
- **展开态**：三子字段**并排**展示；
- **宽度**：整体列宽与子列宽度均可调，并**本机持久化、全局共用**。

交互上借鉴 **[《列表操作列规范》](./列表操作列规范.md)** 的展开/收起切换，但**切换图标方向与操作列相反**（见 §6.1）。

---

## 2. 产品决策（已确认）

| 议题 | 决策 |
|------|------|
| 默认收起子字段 | **中文全称**（`nameZh`）；无需管理员系统默认 |
| 偏好作用域 | **全局**（`crm-table-extend-col:v1:global:customer`，跨列表共用） |
| `CustomerList` 客户主档 | **不使用**扩展列，保持独立列 |
| 展开态子字段 | **3 个**：中文全称、英文全称、客户编号 |
| 英文名为空 | 显示 **`—`**，**不** fallback 中文名 |
| 列设置抽屉 | 扩展列占 **一行**（一个 `key`），整列显隐/排序，**不拆**子字段 |

---

## 3. 适用范围

### 3.1 适用

- `CrmDataTable` 业务单据列表中的「客户」列（销售、采购、库存、物流、财务等）。
- **已接入试点**：`CRM.Web/src/views/Inventory/StockOutList.vue`（`/inventory/stock-out`）。

### 3.2 豁免

- `CustomerList.vue`（客户主档列表）。
- 调试页、无列设置的裸 `el-table`（除非评审后接入）。
- 详情内嵌表 / 弹窗小表：V1 不强制。

### 3.3 后续

- 供应商列可复用同一套组件与 composable（单独评审）。

---

## 4. 术语与状态

### 4.1 扩展列（Extend Column）

逻辑上的**一列**，内含多个**子字段**。在 `CrmDataTable` / `el-table` 中仅对应 **一个** `CrmTableColumnDef`（建议 `key: 'customer'`，`prop: 'customer'`），**不是**三个独立数据列。

### 4.2 子字段（V1 · 客户列）

| 子字段 key | 表头短标签（i18n） | 行数据 prop | 空值 |
|------------|-------------------|-------------|------|
| `nameZh` | 中文 | `customerName` | `—` |
| `nameEn` | 英文 | `customerEnglishName` | `—` |
| `code` | 编号 | `customerCode` | `—` |

### 4.3 收起态（Collapsed）

- 列宽较窄（默认 **160px**，可调）。
- 单元格**单行**显示 `activeField` 对应值。
- 列头：`客户` + **`▾`**（选子字段）+ **`>`**（展开，在标题区右侧）。

### 4.4 展开态（Expanded）

- 列宽较宽（默认约 **516px**，随子列与整体拖拽变化）。
- 列头：**`<`**（收起，在**最左侧**）+ 子标题 `中文 | 英文 | 编号` 并排。
- 单元格：三列值**并排**（CSS Grid，与列头子列对齐）。
- 子列分界处可**拖拽调宽**。

### 4.5 默认状态

无本机偏好时：**收起态** + `activeField = nameZh`。

---

## 5. 功能清单（已实现）

| 功能 | 说明 |
|------|------|
| 展开 / 收起 | 列头 `<` / `>` 切换；状态写入 `expanded` |
| 收起态子字段切换 | `▾` 下拉：中文全称 / 英文全称 / 客户编号 |
| 展开态子列调宽 | 列头「中文\|英文」「英文\|编号」间拖拽手柄，相邻两列联动 |
| 整体列宽调宽 | `CrmDataTable` 表头原生列宽拖拽（`header-dragend`） |
| 宽度持久化 | `subColWidths`、`outerWidthExpanded`、`outerWidthCollapsed` 与展开态一并保存 |
| 全局偏好 | 同一浏览器内所有已接入列表共用 |
| 销售脱敏 | `masked` 时整列显示 `—` |
| 列设置兼容 | 抽屉中「客户」为一行，不参与子字段拆分 |

---

## 6. 交互规范

### 6.1 列头布局（强制）

**展开态：**

```
[ < ]  中文  |  英文  |  编号
 ↑      └── 子列间可拖拽调宽 ──┘
 收起（须在左侧，避免与 el-table 列右缘拖拽区重叠）
```

**收起态：**

```
[ > ]  客户 ▾
 ↑
展开（切换钮在最左；右侧为标题 + 子字段下拉）
```

- 切换钮**始终在最左侧**（展开态为 `<`，收起态为 `>`）。
- 收起态右侧为「客户」+ **`▾`**；展开态右侧为子标题 **中文 | 英文 | 编号**（子列间可拖拽）。
- **禁止**将收起/展开钮放在列最右缘（会与 Element Plus 表头列宽拖拽区重叠导致点击失效）。

### 6.2 子字段选择（收起态 · `▾`）

- `el-dropdown`，选项对应 `common.customerExtendCol.fields.*`。
- 选中后更新 `activeField` 并 `persist()`。
- 表头文案保持「客户」，不改为子字段名。

### 6.3 展开 / 收起按钮

- `type="button"`；`@click.stop.prevent` + `@mousedown.stop`。
- `aria-label`：`common.customerExtendCol.expand` / `collapse`。
- 切换后同步更新列 `width` / `min-width`（见 §7）。

### 6.4 展开态子列调宽

- 类名：`customer-extend-sub-col-resizer`；`role="separator"`。
- `mousedown` 启动拖拽；**左侧列 +Δ、右侧列 −Δ**；单列最小 **56px**（`CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH`）。
- 拖拽结束写入 `subColWidths`，并 `syncOuterWidthExpandedFromSubCols()`。
- 悬停/拖动时竖线高亮（主题色）。

### 6.5 整体列宽调宽（el-table）

- 列表页监听 `CrmDataTable` 的 **`@header-dragend`**。
- 仅当 `column.property === 'customer'` 时处理（`isCustomerExtendTableColumn`）。
- **收起态**：更新 `outerWidthCollapsed`。
- **展开态**：按当前子列比例缩放 `subColWidths`，并更新 `outerWidthExpanded`。
- 调整后 `persist()`。

---

## 7. 列宽与单元格

### 7.1 默认常量（`listCustomerExtendColumnSpec.ts`）

| 常量 | 值 | 含义 |
|------|-----|------|
| `LIST_CUSTOMER_EXTEND_COL_COLLAPSED_WIDTH` | 160 | 收起态默认外宽 |
| `CUSTOMER_EXTEND_SUB_COL_DEFAULT_WIDTHS` | `[180, 180, 100]` | 展开态子列默认宽 |
| `CUSTOMER_EXTEND_SUB_COL_MIN_WIDTH` | 56 | 子列最小宽 |
| `CUSTOMER_EXTEND_SUB_COL_GAP_PX` | 8 | 子列间距 |
| `CUSTOMER_EXTEND_TOGGLE_RESERVE_PX` | 32 | 列头切换钮占位 |
| `CUSTOMER_EXTEND_COL_PADDING_PX` | 16 | 列内边距余量 |

展开态默认外宽 = 子列之和 + 2×gap + 切换钮占位 + padding（约 **516px**）。

### 7.2 展开态单元格

- Grid 三列，与列头 `gridTemplateColumns` **同源**（`subColGridTemplateColumns`）。
- 每格单行，`text-overflow: ellipsis`；`title` 为完整文本。
- **不**使用纵向「标签 + 值」堆叠（V1.1 已改为并排纯值；标签仅在列头）。

### 7.3 收起态单元格

- 单值；`pickCustomerExtendFieldValue(row, activeField)`。
- `emptyText` 默认 `—`（试点页用 `quoteList.na` 时以页面传入为准）。

### 7.4 行高密度

- 与 **[《业务列表规范》](./业务列表规范.md)** §2 兼容；展开态允许多行内容仅当未来扩展，V1 为单行三列。

---

## 8. 样式规范（已实现）

样式集中在 **`CRM.Web/src/assets/styles/crm-unified-list.scss`**（§扩展列块）。

### 8.1 列级 class

| class | 挂载位置 | 说明 |
|-------|----------|------|
| `customer-extend-col` | `CrmTableColumnDef.className` / `labelClassName` | 列头/单元格标识 |
| `customer-extend-col-header` | 列头根容器 | `display:flex`；`.is-expanded` 表展开 |
| `customer-extend-col-header__cols` | 展开态子标题区 | CSS Grid |
| `customer-extend-col-header__col-wrap` | 单个子标题 + resizer | `position:relative` |
| `customer-extend-col-header__col-label` | 子标题文案 | 12px 次要色 |
| `customer-extend-col-toggle-btn` | `<` / `>` | `z-index:5`；`min 24×24` |
| `customer-extend-col-field-picker` | `▾` | 透明按钮 |
| `customer-extend-sub-col-resizer` | 子列分界拖拽柄 | `cursor:col-resize`；`::after` 竖线 |
| `customer-extend-cell` | 单元格根 | `.is-expanded` 时满宽 |
| `customer-extend-cell__cols` | 展开态三列容器 | Grid |
| `customer-extend-cell__col` | 单格值 | 省略号 |
| `customer-extend-cell__value--single` | 收起态单值 | 块级满宽 |

### 8.2 表头溢出

```scss
.el-table th.customer-extend-col.el-table__cell .cell {
  overflow: visible; /* 保证 resizer / 下拉可点 */
}
```

### 8.3 i18n

- 命名空间：`common.customerExtendCol`（`zh-CN.ts` / `en-US.ts`）。
- 键：`columnTitle`、`expand`、`collapse`、`pickField`、`resizeSubCol`、`fields.*`、`fieldShort.*`。

---

## 9. 持久化

### 9.1 存储键

```
crm-table-extend-col:v1:global:customer
```

### 9.2 存储内容

```json
{
  "expanded": false,
  "activeField": "nameZh",
  "subColWidths": [180, 180, 100],
  "outerWidthExpanded": 516,
  "outerWidthCollapsed": 160
}
```

| 字段 | 写入时机 |
|------|----------|
| `expanded` | 点击 `<` / `>` |
| `activeField` | `▾` 选择子字段 |
| `subColWidths` | 子列拖拽结束 |
| `outerWidthExpanded` | 子列拖拽结束 / 展开态整体拖拽 / 按比例缩放后 |
| `outerWidthCollapsed` | 收起态整体拖拽 |

### 9.3 与列设置的关系

- 列顺序/显隐：`crm-table-columns:v1:<columnLayoutKey>`（不变）。
- 「恢复默认」**不**清除扩展列偏好。
- 接入扩展列时若列 `key` 由 `customerName` 改为 `customer`，应**升级** `column-layout-key` 版本后缀，避免旧布局缓存冲突（试点：`stock-out-list-main-v4`）。

---

## 10. 列设置抽屉

1. 扩展列在抽屉中 **一行**（label 一般为「客户」）。
2. 勾选 = 整列显隐；拖拽 = 整列移动。
3. **禁止**为 `nameZh` / `nameEn` / `code` 各建一条抽屉项。

---

## 11. 数据与权限

### 11.1 列表 API

行数据须包含：

- `customerName`
- `customerEnglishName`（JOIN `customerinfo.EnglishOfficialName`）
- `customerCode`（JOIN `customerinfo.CustomerCode`）

**试点后端：** `StockOutListItemDto` + `StockOutService.ProjectStockOutListDtosForOutsAsync`。

### 11.2 销售脱敏

- `CustomerExtendCell` 传 `:masked="maskSaleSensitiveFields"`（或等价）。
- 后端 `SaleSensitiveFieldMask521` 清空 `CustomerName` / `CustomerEnglishName` / `CustomerCode`。

---

## 12. 技术实现（代码地图）

### 12.1 文件一览

| 路径 | 职责 |
|------|------|
| `CRM.Web/src/constants/listCustomerExtendColumnSpec.ts` | 默认宽度、存储键、类型、Grid 计算 |
| `CRM.Web/src/composables/useCustomerExtendColumn.ts` | 全局状态、持久化、拖拽、外宽同步 |
| `CRM.Web/src/components/list/CustomerExtendColumnHeader.vue` | 列头 UI |
| `CRM.Web/src/components/list/CustomerExtendCell.vue` | 单元格 UI |
| `CRM.Web/src/assets/styles/crm-unified-list.scss` | 扩展列样式 |
| `CRM.Web/src/views/Inventory/StockOutList.vue` | **参考接入** |

### 12.2 `useCustomerExtendColumn()` 导出

| 成员 | 用途 |
|------|------|
| `expanded` | 是否展开（模块级单例 ref） |
| `activeField` | 收起态显示字段 |
| `subColWidths` | `[中文宽, 英文宽, 编号宽]` |
| `colWidth` / `colMinWidth` | 绑定列 `width` / `minWidth` |
| `subColGridTemplateColumns` | 列头/单元格 Grid 模板 |
| `toggleExpanded` | 展开/收起 |
| `setActiveField` | 写 `activeField` |
| `startSubColResize` | 子列拖拽（列头内调用） |
| `applyOuterWidthFromTable` | `header-dragend` 回调 |
| `isCustomerExtendTableColumn` | 判断是否客户扩展列 |

> composable 使用**模块级单例** ref，Header / Cell / 列表页共享同一偏好。

### 12.3 列表页接入清单

1. **后端**：列表 DTO 增加 `customerEnglishName`、`customerCode`；脱敏一并处理。
2. **列定义**：`key: 'customer'`，`prop: 'customer'`，`className` / `labelClassName: 'customer-extend-col'`，`width` / `minWidth` 绑定 `colWidth` / `colMinWidth`。
3. **computed 列数组**：读取 `customerExtendExpanded.value`、`customerExtendColWidth.value` 以触发列宽更新。
4. **插槽**：
   - `#col-customer-header` → `<CustomerExtendColumnHeader :active-field="..." @set-active-field="..." />`
   - `#col-customer` → `<CustomerExtendCell :row="row" :active-field="..." :masked="..." />`
5. **`CrmDataTable`**：`@header-dragend="onXxxHeaderDragEnd"`，内部调用 `applyOuterWidthFromTable`。
6. **API 归一化**：前端 `normalize*Row` 映射 `customerEnglishName`、`customerCode`。
7. **layout-key**：若替换原 `customerName` 列，递增 `column-layout-key` 版本。

### 12.4 接入示例（摘录）

```typescript
const {
  expanded: customerExtendExpanded,
  activeField: customerExtendActiveField,
  colWidth: customerExtendColWidth,
  colMinWidth: customerExtendColMinWidth,
  setActiveField: setCustomerExtendActiveField,
  applyOuterWidthFromTable: applyCustomerExtendOuterWidth
} = useCustomerExtendColumn()

const columns = computed(() => {
  void customerExtendExpanded.value
  void customerExtendColWidth.value
  return [
    // ...
    {
      key: 'customer',
      label: t('common.customerExtendCol.columnTitle'),
      prop: 'customer',
      width: customerExtendColWidth.value,
      minWidth: customerExtendColMinWidth.value,
      className: 'customer-extend-col',
      labelClassName: 'customer-extend-col',
      showOverflowTooltip: true
    }
  ]
})

function onHeaderDragEnd(newWidth: number, _old: number, column: { property?: string }) {
  if (!isCustomerExtendTableColumn(column)) return
  applyCustomerExtendOuterWidth(newWidth)
}
```

---

## 13. 分期与验收

### 13.1 实施状态

| 阶段 | 状态 | 说明 |
|------|------|------|
| 组件 + composable + 样式 | ✅ 完成 | V1.1 |
| 出库列表试点 | ✅ 完成 | `StockOutList.vue` |
| 其余业务列表推广 | ⏳ 待做 | SO 明细、收款等 |
| 供应商扩展列 | ⏳ 待评审 | — |

### 13.2 验收要点

1. 默认收起，显示中文全称。
2. `▾` 改英文名后，换页面仍为英文名（全局偏好）。
3. 点击 `>` 展开：列头左侧 `<`，三列并排；英文空为 `—`。
4. 子列拖拽、整体拖拽后刷新页面，宽度恢复。
5. 点击 `<` 收起为单行。
6. 列设置中「客户」仅一行。
7. `CustomerList` 无扩展列控件。
8. 脱敏时整列 `—`。

---

## 14. 参考文档

- [《业务列表规范》](./业务列表规范.md) §1.8  
- [《列表操作列规范》](./列表操作列规范.md)  
- `CRM.Web/src/components/CrmDataTable.vue`

---

## 15. 修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| V1.0 | 2026-06-04 | 初稿与产品确认 |
| V1.1 | 2026-06-04 | 对齐已实现：三列并排、切换钮置左、子列/整体宽度持久化、出库试点、样式与接入清单 |
