# Debug 页 PRD

**实现与版本日志细节：** [内外版本日志 — 设计与实现](../System/内外版本日志-设计与实现.md)

---

## 路由

| 路径 | 说明 | 登录 |
|------|------|------|
| `/debug` | Debug 主列表（版本 / 数据库 / 记录面板） | 免登录 |
| `/debug/internal-version-log` | 内部版本日志全文 | 登录 + 系统管理员 |
| `/debug/data` | Debug 模拟数据 | 登录 + 系统管理员 |
| `/debug/tools` | Debug 工具 | 登录 + 系统管理员 |
| `/debug/ai` | AI 物料规格调试 | 登录 + 系统管理员 |
| `/debug/material-intel` | AI 物料情报对照 | 登录 + 系统管理员 |
| `/release-notes` | 对外版本更新日志（独立页，非 Debug 子路由） | 免登录 |

常见误输 `/ldebug` 重定向至 `/debug`。

---

## 页面内容（`/debug`）

### 版本面板

- 显示 **当前前端构建版本**（常量 `FRONTEND_DEBUG_VERSION`，来源见设计与实现文档）。
- **用途：** 部署后核对线上前端是否为预期 Git 提交对应构建。
- **格式：** `1.1.{MMdd}-{HHmm} {最近一次有效 Git commit 说明}`；由 `post-commit` 钩子自动维护，**非** `package.json` 版本号。
- **链接：**
  - 「版本更新日志」→ `/release-notes`（用户向说明，手工维护 Markdown）
  - 「内部版本日志」→ `/debug/internal-version-log`（完整提交历史，仅 sysAdmin）

### 数据库面板

- 显示后端连接数据库的连接字串（脱敏）。
- 密码前若干位显示为星号；按 PRD 仅展示库名等必要信息供环境核对。

### 记录面板

- 显示 `debug` 表内容，列表展示（Name / Value）。

---

## 权限原则

- **免登录：** 仅 `/debug` 主列表（便于运维快速核对版本与库连接）。
- **sysAdminOnly：** 所有 Debug 子功能页（含内部版本日志全文），与 RBAC 中平台管理员能力一致。

---

## 维护

| 变更类型 | 维护位置 |
|----------|----------|
| 内部版本号 / 提交日志 | 自动（`scripts/update-internal-version-log.mjs`）；勿手改除非修复异常 |
| 对外版本说明 | `help/pages/version/版本更新日志_MENU_RELEASE_NOTES.md` |
| 路由 / 权限 | `CRM.Web/src/router/routes.ts`、`router/index.ts` + 同步 PRD 与设计文档 |
