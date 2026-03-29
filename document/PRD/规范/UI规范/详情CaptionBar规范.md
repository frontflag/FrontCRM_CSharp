# 详情 Caption Bar 规范

## 1. 定义与适用范围

**详情 Caption Bar**（简称 **CaptionBar**）指业务「详情页」顶部、横跨内容区的一条**水平标题与操作栏**：左侧承载**返回导航 + 主实体识别信息**（头像、标题、状态、编号、标签等），右侧承载**主操作按钮组**（编辑、更多、业务主入口等）。

- **本规范来源**：`/customers/:id` 客户详情页顶部红框区域（`CustomerDetail.vue` 中 `.page-header` 及子结构）。
- **适用场景**：各类主数据/单据详情页（客户、供应商、订单、RFQ 等）需保持**同一套顶栏布局与视觉层级**时，应优先对齐本规范；具体文案与按钮集合可按业务裁剪。

---

## 2. 布局结构

### 2.1 整体

| 项目 | 规范 |
|------|------|
| 容器类名建议 | `page-header`（或与页面 BEM 前缀组合，如 `xxx-detail-page__caption`） |
| 布局 | `display: flex; align-items: center; justify-content: space-between;` |
| 与下方内容间距 | `margin-bottom: 24px`（与当前客户详情一致） |
| 页面内边距 | 详情页容器一般为 `padding: 24px`，CaptionBar 不单独再加顶栏全宽背景条；与页面同背景（`$layer-1`） |

### 2.2 左侧 `header-left`

| 项目 | 规范 |
|------|------|
| 布局 | `display: flex; align-items: center; gap: 16px;` |
| 内容顺序 | **返回按钮** → **标题组**（`customer-title-group` 同类结构） |

**标题组内部**（从左到右）：

1. **实体头像**（`customer-avatar-lg` 类）：圆角方形容器，单字缩写；约 `48×48px`，`border-radius: 12px`，渐变底 + 青色描边（见参考实现）。
2. **文字列**（纵向多块）：
   - **第一行 `page-title-row`**：`flex` 横向，`align-items: center`，`gap: 10px`，`margin-bottom: 6px`。
     - **标题区 `page-title-with-icons`**：`h1.page-title`（主标题，约 20px / semibold）+ **业务状态图标**（如 `PartyStatusIcons`：冻结、黑名单等）。
     - **收藏按钮** `btn-favorite-star`：与标题行对齐；空心/实心星切换；已收藏为金色（`#ffc94d` 系）。
   - **第二行 `title-meta`**：编号（如 `customer-code`，等宽字体小字号）+ **分级/类型徽标**（如 `level-badge`）。
   - **第三行 `title-tags-row`**（可选）：标签展示组件 + 「添加标签」等弱操作链（`btn-add-tag` 虚线胶囊）。

冻结、黑名单等**状态**优先用专用图标组件展示在**标题旁**，与收藏星标同属「标题行右侧附属控件」。

### 2.3 右侧 `header-right`

| 项目 | 规范 |
|------|------|
| 布局 | `display: flex; align-items: center; gap: 10px;` |
| 按钮顺序（客户详情参考） | **主色操作**（`btn-primary`，如「编辑」）→ **更多**（`btn-more-actions`，「⋯」）→ **成功色主业务入口**（`btn-success`，如「创建需求」） |

说明：

- **主流程/正向业务**（新建、创建下游单据）使用 **绿色渐变** `btn-success`，作为右侧**视觉焦点之一**。
- **编辑、保存类主操作**使用 **青蓝渐变** `btn-primary`。
- **次要、危险操作**放入「更多」下拉，避免顶栏堆叠过多按钮。

---

## 3. 组件与交互要求

| 控件 | 要求 |
|------|------|
| 返回 `btn-back` | 左箭头 SVG +「返回」文案；弱背景、边框；`hover` 略提亮 |
| 主标题 `page-title` | 单行优先；若实体停用/黑名单等，使用修饰类（如 `page-title--muted`）降低对比度 |
| 收藏 `btn-favorite-star` | `aria-pressed` / `title` 随状态切换；禁用态降低透明度；收藏状态与后端/本地收藏列表同步后可派发业务事件（如各模块自定义 `*-favorites-changed`） |
| 标签行 | 无标签时由标签组件展示占位文案；「添加标签」为轻量按钮，不与主按钮抢视觉 |

---

## 4. 视觉与 Token

实现时应使用全局 SCSS 变量（`@/assets/styles/variables.scss`），与客户详情保持一致，例如：

- 页面背景：`$layer-1`
- 主文字：`$text-primary`；次要：`$text-muted`、`$text-secondary`
- 边框：`$border-panel`；圆角：`$border-radius-md` 等
- 强调色：`$cyan-primary`（头像、链接感）

**字号参考（客户详情当前实现）**

- 主标题：20px，font-weight 600  
- 返回/主按钮：13px  
- 编号 `customer-code`：11px，Space Mono  
- 级别徽标：10px  

---

## 5. 响应与可访问性

- 标题区允许 `flex-wrap`，避免窄屏下与图标严重重叠。
- 收藏、更多操作按钮需有 `title` 或 `aria-label`。
- 图标按钮（更多「⋯」）`min-width` / `height` 建议与主按钮行高协调（客户详情「更多」为约 36px 高）。

---

## 6. 参考实现路径

| 说明 | 路径 |
|------|------|
| 结构与样式参考 | `CRM.Web/src/views/Customer/CustomerDetail.vue`（模板顶部 `.page-header`；样式段「页面头部」至 `.btn-more-actions`） |
| 状态图标 | `CRM.Web/src/components/party/PartyStatusIcons.vue` |
| 标签展示 | `CRM.Web/src/components/Tag/TagListDisplay.vue` |

---

## 7. 扩展说明

- 若某详情页**无**头像或**无**标签行，可删除对应块，但应保持 **左侧：返回 + 标题列**、**右侧：操作组** 的左右分栏结构不变。
- 单据类详情（如销售/采购订单）可将「单号」放入 `title-meta` 或与 `page-title` 组合展示，仍属同一 CaptionBar 信息层级。

---

*文档版本：与客户详情页当前实现对齐；后续若全局设计 token 变更，以 `variables.scss` 与客户详情实现为准同步更新本文。*
