# PostgreSQL 增量脚本编写规范

## 适用范围

- 仓库 `scripts/*.sql`：运维 / DBA 在 **DBeaver、Navicat、pgAdmin** 等客户端**手工执行**的增量脚本。
- 不适用于 EF Core `Migrations/*.cs` 内嵌 SQL（见文末「与 EF Migration 的区别」）。

目标：**脚本可直接整文件执行，不弹出「绑定参数 / Bind Parameters」窗口，且可重复执行（幂等）**。

---

## 1. 问题现象

在 DBeaver / Navicat 中执行 SQL 时，工具弹出 **「绑定参数」** 对话框，要求填写 `pn`、`raw_text`、`{{xxx}}` 等变量，不填无法继续。

常见原因：**客户端把脚本中的占位符语法误识别为「待绑定参数」**，并非 PostgreSQL 服务端报错。

---

## 2. 会触发参数窗口的写法（禁止出现在 scripts SQL 中）

| 写法 | 典型工具 | 说明 |
|------|----------|------|
| `{{field_name}}` | DBeaver、Navicat | AI Prompt 模板占位符；**注释里写也会触发** |
| `:field_name` | 部分 JDBC 风格客户端 | 冒号 + 标识符（勿与 PostgreSQL 类型转换混淆，见下） |
| `${field_name}` | 部分模板引擎 | 少见，仍应避免 |
| `?` | JDBC 预编译占位 | 手工脚本中勿用 |

### 2.1 安全写法（不会误识别）

| 写法 | 说明 |
|------|------|
| `::text`、`::jsonb`、`::bigint` | PostgreSQL **类型转换**，双冒号，**允许** |
| `'固定字符串'` | 普通字面量，**允许** |
| `decode('...', 'hex')` | 十六进制字节，**允许**（推荐承载中文与特殊字符） |
| `CHR(n)` 拼接 | 用于拼出 `{`、`}` 等字符，**允许** |

### 2.2 本项目高频场景：AI `user_prompt_template`

运行时 API 使用 `{{pn}}`、`{{raw_text}}` 等占位符（见 `AiJsonHelper.RenderTemplate`）。  
**入库值可以是 `{{raw_text}}`，但脚本文件里不能出现连续两个花括号字面量。**

---

## 3. 推荐解决方案

### 方案 A：CHR 拼接占位符（**首选**，可读性好）

拼出 `{{raw_text}}`：

```sql
|| CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125)
-- CHR(123) = '{', CHR(125) = '}'
```

拼出 `{{pn}}`：

```sql
|| CHR(123) || CHR(123) || 'p' || 'n' || CHR(125) || CHR(125)
```

**参考实现：**

- `scripts/ai_material_intel_schema_v2_postgresql.sql`
- `scripts/ai_entity_parse_postgresql.sql`

完整示例：

```sql
UPDATE public.ai_prompt_template
SET user_prompt_template =
    convert_from(decode('e8af...efbc9a', 'hex'), 'UTF8')  -- 「…原文：」
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.customer' AND version = 1;
```

### 方案 B：整段 user_prompt 经 hex 写入

适用于**不含** `{{` 占位符的短 Prompt，或占位符也拆出去用方案 A 拼接。

**参考实现：** `scripts/ai_module_postgresql.sql`（`material.spec.lookup` 的 user_prompt）

```sql
user_prompt_template = convert_from(
    decode('e8af...', 'hex'),
    'UTF8'
)
```

若 Prompt 末尾需要 `{{pn}}`，hex 只写到「型号：」为止，再用 **方案 A** 拼接占位符，**不要把 `7b7b...`（即 `{{` 的 UTF-8 hex）写进 decode 字符串**——部分工具仍可能扫描 decode 内容。

### 方案 C：PostgreSQL 美元引号 `$$...$$`

在 **psql** 或 EF Migration 中常用；DBeaver 对 `$$` 内 `{{` 的行为因版本而异，**手工 scripts 不推荐依赖**，优先 A / B。

### 方案 D：客户端关闭绑定参数（兜底，非规范）

DBeaver：**窗口 → 首选项 → 编辑器 → SQL 编辑器 → 取消勾选「绑定参数」相关选项**（名称因版本略有差异）。  
仅作个人临时手段；**提交到仓库的脚本仍须符合本规范**，避免同事环境不一致。

---

## 4. 脚本文件头与注释规范

每个 `scripts/*.sql` 文件首行起建议包含：

```sql
-- 增量：<简要说明>
-- DBeaver-safe / Navicat-safe：<本脚本采用的占位符写法，如「CHR 拼接 raw_text」>
```

**注释中禁止出现：**

- `{{任意名称}}`
- 易被误读的 `:paramName`（非 `::类型`）

可写：「占位符经 CHR 拼接 raw_text」「勿写双花括号字面量」。

---

## 5. 幂等与已有库

增量脚本应 **可重复执行**：

- `INSERT ... ON CONFLICT ... DO NOTHING` 或 `DO UPDATE`
- 对已部署环境，必要时追加 **UPDATE** 修正 Prompt / 权限（见 `ai_entity_parse_postgresql.sql` 文末）

---

## 6. 提交前自检清单

在 PR / 合并前对新增或修改的 `scripts/*.sql` 执行：

```bash
# 1. 脚本内不得出现双花括号（含注释）
rg '\{\{' scripts/你的脚本.sql

# 2. 可选：排查可疑单冒号绑定（排除 :: 类型转换需人工看）
rg ':[a-zA-Z_][a-zA-Z0-9_]*' scripts/你的脚本.sql
```

- [ ] 文件头注明 DBeaver-safe / Navicat-safe
- [ ] 无 `{{...}}` 字面量（含注释、字符串、hex 注释说明）
- [ ] AI Prompt 占位符使用 **CHR 拼接** 或 **hex + CHR**
- [ ] 在 DBeaver **整脚本执行一次**，确认无绑定参数弹窗
- [ ] 幂等（重复执行不报错、或仅 UPDATE 固定行数）

---

## 7. 参考脚本索引

| 脚本 | 要点 |
|------|------|
| `scripts/ai_module_postgresql.sql` | 全文件 DBeaver-safe；user_prompt 纯 hex |
| `scripts/ai_material_intel_lookup_postgresql.sql` | 注释规范；hex user_prompt |
| `scripts/ai_material_intel_schema_v2_postgresql.sql` | **CHR 拼接 `{{pn}}`** 标准范例 |
| `scripts/ai_entity_parse_postgresql.sql` | **CHR 拼接 `{{raw_text}}`**；文末 UPDATE 已有库 |

---

## 8. 与 EF Migration 的区别

| 维度 | `scripts/*.sql` | `CRM.Infrastructure/Migrations/*.cs` |
|------|-----------------|-------------------------------------|
| 执行方式 | DBeaver 手工 | `dotnet ef database update` |
| `{{pn}}` 写法 | **禁止**字面量；用 CHR / hex | C# 原始字符串中写 `{{{{pn}}}}`（转义为 SQL 中的 `{{pn}}`） |
| 美元引号 `$$` | 不推荐依赖 | Migration 内常用 |

Migration 示例见 `20260805100000_MaterialIntelLookupSchemaV2.cs`。

---

## 9. 相关文档

- `document/System/AI模块架构与实现.md` — AI 模块与 Prompt 模板
- `document/System/AI物料情报查询-设计与实现.md` §7.3 — 业务侧执行注意（指向本文）

---

**版本：** 2026-06-03  
**维护：** 新增 AI / Prompt / 权限类 SQL 时必读本规范。
