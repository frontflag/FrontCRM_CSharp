# scripts — PostgreSQL 增量脚本

运维 / DBA 在 DBeaver、Navicat 等客户端**手工执行**的 SQL 放此目录。

## 编写规范（必读）

**[PostgreSQL 增量脚本编写规范](../document/PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md)**

要点：

- 脚本内（**含注释**）禁止 `{{占位符}}` 明文 → 用 `CHR(123)||CHR(123)||'name'||CHR(125)||CHR(125)` 拼接
- 中文 Prompt 优先 `convert_from(decode('...','hex'),'UTF8')`
- 提交前 `rg '\{\{' scripts/你的文件.sql` 应为空
- 在 DBeaver 整文件试跑，确认无「绑定参数」弹窗

## 命名

`{主题}_{postgresql|pg}.sql`，例如 `ai_entity_parse_postgresql.sql`。
