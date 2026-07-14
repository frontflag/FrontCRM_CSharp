# 帮助手册（Markdown）

## 当前约定（菜单驱动）

1. **注册表**：`menu-registry.json` 描述每个主菜单项的 `id`、`label`、路由名与路径前缀。
2. **文档路径**：`pages/{菜单名称}_{菜单ID}.md`（例：`pages/销售订单_MENU_SALES_ORDERS.md`）。
3. **目录**：根目录 `帮助文档目录.md` 由 `scripts/sync-help.mjs` 根据注册表自动生成，仅展示菜单名称链接。
4. **同步**：`npm run dev` / `npm run build` 前执行 `sync-help.mjs`，将 `help/` 复制到 `CRM.Web/public/help/`，并补全缺省占位页（不覆盖已有文件）。
5. **前端解析**：`CRM.Web/src/utils/helpDocPath.ts` 按当前路由解析菜单项并 `fetch` 对应 `.md`；右栏「帮助」内点击 `.md` 链接仅在面板内切换，不跳转整页。

## 读者与正文要求（必读）

右侧「帮助」页签面向**业务用户**，不是开发人员。`help/pages/*.md` 中：

- **应写**：能做什么、如何操作、状态/权限限制的业务说法、注意事项。
- **勿写**：权限码、路由、库表/字段名、CSS/i18n、实现文档链接、（与原 status=1 对应）等开发对照。

完整规则见产品文档 **[扩展面板.帮助规范 §2.6](../document/PRD/规范/UI规范/扩展面板.帮助规范.md)**。从 `document/System/` 设计文档摘录时须改写为用户语言。

## 单页内容结构（建议）

- 第 1 行：返回 [帮助文档目录](../帮助文档目录.md)
- 一级标题：`# {菜单名称}`（或与产品约定使用 `help-h1--offset-down` 等扩展 class，见规范 §2.5）
- `## 页面功能` — 业务说明
- `## 操作说明` — 操作列按钮及前置条件；**推荐**使用 `.help-op-block` 卡片版式，详见 [扩展面板.帮助规范](../document/PRD/规范/UI规范/扩展面板.帮助规范.md)。**无实际限制时不要写「前置条件：无」**，有权限/状态等限制时才写第三行。

## 编写 / 生成后自检

```bash
# 可选：移除无意义的「前置条件：无」
node scripts/clean-help-prerequisites.mjs

# 推荐：将残留的开发向表述改为用户语言（AI 或脚本批量生成后默认执行）
node scripts/clean-help-user-facing.mjs

# 构建后自动校验（npm run build 的 postbuild 会执行；也可手动跑）
node scripts/verify-help-dist.mjs
```

`verify-help-dist.mjs` 会逐文件比对 `help/` 与 `CRM.Web/dist/help/`，任一页面或目录不一致则构建/部署失败，避免线上仍是旧版帮助。

使用 `scripts/generate-help-pages-content.mjs` 批量生成时会自动调用 `clean-help-user-facing.mjs`（该生成脚本**会覆盖**已有页面，慎用）。

## 历史说明

旧版 `help/{模块}/{route.name}.md` 目录结构已移除；帮助内容仅以 `menu-registry.json` + `pages/` 为准。
