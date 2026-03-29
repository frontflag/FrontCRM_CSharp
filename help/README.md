# 帮助手册（Markdown）

## 当前约定（菜单驱动）

1. **注册表**：`menu-registry.json` 描述每个主菜单项的 `id`、`label`、路由名与路径前缀。
2. **文档路径**：`pages/{菜单名称}_{菜单ID}.md`（例：`pages/销售订单_MENU_SALES_ORDERS.md`）。
3. **目录**：根目录 `帮助文档目录.md` 由 `scripts/sync-help.mjs` 根据注册表自动生成，仅展示菜单名称链接。
4. **同步**：`npm run dev` / `npm run build` 前执行 `sync-help.mjs`，将 `help/` 复制到 `CRM.Web/public/help/`，并补全缺省占位页（不覆盖已有文件）。
5. **前端解析**：`CRM.Web/src/utils/helpDocPath.ts` 按当前路由解析菜单项并 `fetch` 对应 `.md`；右栏「帮助」内点击 `.md` 链接仅在面板内切换，不跳转整页。

## 单页内容结构（建议）

占位模板已包含：

- 第 1 行：返回 [帮助文档目录](../帮助文档目录.md)
- 一级标题：`# {菜单名称}`（或与产品约定使用 `help-h1--offset-down` 等扩展 class，见下）
- `## 页面功能` — 业务说明
- `## 操作说明` — 操作列按钮及前置条件；**推荐**使用扩展面板.帮助约定的 `.help-op-block` 卡片版式，详见产品文档 [扩展面板.帮助规范](../document/PRD/规范/UI规范/扩展面板.帮助规范.md)。旧内容可用表格，建议逐步迁移。

## 历史说明

旧版 `help/{模块}/{route.name}.md` 目录结构已移除；帮助内容仅以 `menu-registry.json` + `pages/` 为准。
