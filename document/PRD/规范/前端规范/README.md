# 前端规范总索引

本目录沉淀 **CRM.Web** 工程级前端约定（不限于 UI 视觉），与 [UI规范](../UI规范/README.md) 并列：UI 规范管页面长什么样，本目录管怎么写、怎么构建、怎么避免跨平台问题。

## 已发布规范

- [import 路径大小写规范](./import路径大小写规范.md)（**强制**）
  - Windows 开发、Linux 部署：`import` 路径必须与磁盘目录名大小写完全一致；提交前须 `vue-tsc` / `npm run build`。

## 使用建议

1. 新增/移动 `CRM.Web/src` 下文件前，先查目标目录在仓库中的**真实大小写**（可参考同目录已有文件的 `import`）。
2. 代码评审时，对 `@/`、`../` 路径做大小写核对。
3. 新增规范后同步更新本索引。

## 相关

- Cursor 规则：`CRM.Web/.cursor/rules/import-path-casing.mdc`
- 类型检查：`CRM.Web/.cursor/rules/typecheck-after-frontend-edit.mdc`
