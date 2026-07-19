# import 路径大小写规范

> **状态：强制** — 所有 `CRM.Web` 前端改动均须遵守。  
> **原因：** Windows 文件系统大小写不敏感，Linux CI/生产构建大小写敏感；路径大小写不一致会在 `vue-tsc` 报 **TS1261**，部署失败。

---

## 1. 核心要求

**`import` / `export` / 动态 `import()` 中的路径，必须与磁盘上文件、文件夹名称的大小写完全一致。**

含但不限于：

- `@/` 别名路径（如 `@/components/Logistics/...`）
- 相对路径（如 `../utils/foo.ts`）
- Vue 单文件组件引用（`.vue`）

---

## 2. 正反例

### ✅ 正确

组件实际位于 `CRM.Web/src/components/Logistics/ShipmentExpressFields.vue`：

```ts
import ShipmentExpressFields from '@/components/Logistics/ShipmentExpressFields.vue'
```

同项目已有参考：

```ts
import ArrivalNoticeSearchPanel from '@/components/Logistics/ArrivalNoticeSearchPanel.vue'
```

### ❌ 错误

```ts
// 目录实为 Logistics（大写 L），小写 logistics 在 Linux 上会 TS1261
import ShipmentExpressFields from '@/components/logistics/ShipmentExpressFields.vue'
```

---

## 3. 目录命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| `components/` 下业务模块目录 | **PascalCase** | `Logistics/`、`Inventory/`、`Document/` |
| 文件名 | 与现有同目录文件保持一致 | `ShipmentExpressFields.vue` |

新建目录前：

1. 在资源管理器或 `git ls-files` 中确认**不存在**仅大小写不同的同名目录。
2. 优先复用已有目录，不要另建 `logistics` 与 `Logistics` 混用。

---

## 4. 开发与提交检查

### 4.1 本地必做（Windows 同样执行）

在 `CRM.Web` 目录：

```powershell
npm run typecheck
# 或发布前完整构建
npm run build
```

仅 `npm run dev` **不能**替代；`vue-tsc` 才会严格校验路径大小写。

### 4.2 Git（推荐）

仓库根目录启用大小写敏感，避免 Git 忽略仅大小写的重命名：

```gitconfig
core.ignorecase=false
```

（团队统一配置后，改目录大小写须用 `git mv` 显式操作。）

### 4.3 代码评审

- [ ] 新增 `import` 路径是否与 `git ls-files` 中路径一致？
- [ ] 是否复制了同模块已有文件的正确写法？
- [ ] 是否已跑过 `npm run typecheck`？

---

## 5. 典型故障

| 现象 | 原因 |
|------|------|
| 本地 dev 正常，部署 `vue-tsc` 失败 TS1261 | import 大小写与磁盘/Git 索引不一致 |
| Windows 上两个路径「指向同一文件夹」 | NTFS 不区分大小写，TS 仍视为两个路径 |

---

## 6. 实现参考

| 项 | 路径 |
|----|------|
| 曾触发 TS1261 的修复 | `PackingCreatePage.vue`、`SalesOrderDetail.vue` → `@/components/Logistics/...` |
| Cursor 规则 | `CRM.Web/.cursor/rules/import-path-casing.mdc` |
| 提交前类型检查 | `CRM.Web/.cursor/rules/typecheck-after-frontend-edit.mdc` |
