# 列表操作列视觉规范 PRD（V1）

## 1. 文档目的

将**客户列表**（`CustomerList.vue`）中「操作列」的完整 UI 逻辑固化为规范，供全站原生表、`el-table` 固定列等场景对齐实现，避免各页操作区底色、分隔线与按钮显隐不一致。

**与下列规范配套阅读：**

- [列表操作按钮颜色规范 PRD](./列表操作按钮颜色规范PRD.md)（按钮语义色：primary / danger / warning 等）
- [列表字段宽度规范 PRD](./列表字段宽度规范PRD.md)（操作列宽度等）

---

## 2. 适用范围

- 列表页中**行级操作**所在列（通常为表头「操作」）
- **原生 HTML 表格**：`.table-wrapper` + `table.data-table`（客户列表、供应商列表等）
- **Element Plus `el-table`**：操作列常用 `fixed="right"`，需单独用 `:deep` 覆盖固定列层级底色（与原生末列视觉等效）

---

## 3. 列位置约定

1. **操作列必须为数据区最后一列**（表头 `th` / 单元格 `td` 的**最后一个**）。
2. 若业务上「创建时间」等在操作列右侧，应调整列顺序，使**操作列固定在最右**（原生表即末列；`el-table` 建议 `fixed="right"` 且为最后一列），以保证与下列选择器及固定列阴影逻辑一致。

---

## 4. 操作列区域（单元格底色与分隔线）

### 4.1 设计语义

- 操作列使用**第三层面板色**（`$layer-3`，`#162233`），与列表主体（`$layer-2`）形成**嵌套面板**层次，突出可操作区域。
- 左侧用**内阴影竖线**与数据区分隔，不使用列间竖线（全表统一去竖线风格下，仅靠 inset 表达分界）。

### 4.2 原生 `.data-table`（实现位置：`CRM.Web/src/assets/styles/crm-unified-list.scss`）

| 状态 | 规则 |
|------|------|
| 默认 | `thead th:last-child`、`tbody td:last-child`：`background: $layer-3`；`box-shadow: inset 1px 0 0 rgba(0, 212, 255, 0.1)` |
| 行 hover | 同上末列：`background` 在 `$layer-3` 之上叠一层 `rgba(0, 212, 255, 0.065)`（可用 `linear-gradient` 与 `$layer-3` 组合）；`inset` 线加强为 `rgba(0, 212, 255, 0.14)` |

### 4.3 行 hover 与操作列关系（客户列表）

- 数据行使用 `.table-row`，`tr:hover` 时整行背景为 `rgba(0, 212, 255, 0.04)`（与全表 hover 基调一致）。
- **末列单元格**因有独立底色，需在上述 4.2 中单独定义 hover，使操作列在 hover 时仍呈现「层 3 + 淡青高光」，避免与透明单元格观感脱节。

### 4.4 `el-table` 固定右侧操作列

全局 `main.scss` 中固定列多为透明背景，**必须在具体列表页**用页面根类 + `:deep` 为 `.el-table__fixed-right` 整链路透传 `$layer-3`，并对 `th.el-table__cell` / `td.el-table__cell` 应用与 4.2 相同的 **inset 左线** 与 **行 hover 叠青**。

参考实现（节选结构，非完整代码）：

- 固定列容器、表体包装、表、`tr`、`.cell` 等：`background-color: $layer-3 !important`
- 仅 **`th`/`td` 单元格**增加 `box-shadow: inset 1px 0 0 ...`
- `tr:hover` / `.hover-row` 下固定列 `td`：与 4.2 末列 hover 一致
- 固定列整体：`box-shadow: -12px 0 18px -10px rgba(0, 0, 0, 0.72)`；`::before` / patch 与项目内采购/销售明细、报价列表、需求明细等页面对齐

---

## 5. 操作列内按钮容器与显隐

### 5.1 容器 `.action-btns`

- `display: flex`；`align-items: center`；`gap: 6px`
- `white-space: nowrap`；`flex-wrap: nowrap`（禁止换行）
- **默认 `opacity: 0`**，`transition: opacity 0.15s`
- **当鼠标悬停在该数据行（`tr`）上时**，将 `.action-btns` 设为 **`opacity: 1`**（客户列表在 `.table-row:hover .action-btns` 中实现）

目的：减少静态时的视觉噪音；悬停行时再露出操作。

### 5.2 单个按钮 `.action-btn`

- `padding: 4px 10px`；`font-size: 12px`；`border-radius: 5px`；`cursor: pointer`；`transition: all 0.15s`；`flex-shrink: 0`
- **颜色与语义**严格遵循 [列表操作按钮颜色规范 PRD](./列表操作按钮颜色规范PRD.md)：
  - `action-btn--primary`：查看 / 详情 / 编辑等
  - `action-btn--danger`：删除等高危
  - `action-btn--warning`：提交审核等业务流转（非蓝非红非绿）

具体色值以客户列表 `CustomerList.vue` 中现有块为准，与颜色规范一致。

---

## 6. 交互与可访问性

1. **行双击进详情**的列表：操作列单元格内需 **`@click.stop` / `@dblclick.stop`**（或等价阻止冒泡），避免双击按钮触发行级双击。（参见《列表双击进入详情规范 PRD》若已发布。）
2. 操作列内按钮保持**可键盘聚焦**时，若采用 `opacity: 0` 隐藏，需评估是否仅在「行 hover」时显示导致键盘用户不可见——当前客户列表以鼠标悬停行为主；若后续要求无障碍达标，可改为「常驻可见」或「焦点行内可见」的变体，并更新本规范。

---

## 7. 实现对照表

| 能力 | 客户列表（参考） | 全局/共享样式 |
|------|------------------|----------------|
| 表格外框 | `.table-wrapper` | `crm-unified-list.scss` |
| 末列底色 + 分隔线 + 末列 hover | 末列 `th`/`td` | `crm-unified-list.scss` → `.data-table` |
| 行 hover + 按钮显隐 | `.table-row` + `.action-btns` | `CustomerList.vue` scoped |
| 按钮形色 | `.action-btn` 及修饰类 | `CustomerList.vue`；语义见颜色规范 PRD |
| `el-table` 固定操作列 | 各业务页 `:deep` | 与 4.4 同逻辑，变量用 `variables.scss` 中 `$layer-3` |

---

## 8. 修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| V1 | 2026-03-28 | 初版：从客户列表 + `crm-unified-list.scss` 抽象操作列视觉与按钮显隐逻辑 |
