# BBS 论坛 — 设计与实现

> **状态：** MVP（已实现代码；须跑 SQL / SchemaEnsure 并重启 API）  
> **EBS 对照（只读）：** [document/EBS/辅助功能/BBS.md](../../EBS/辅助功能/BBS.md)  
> **关联：** [系统公告](../系统/系统公告-设计与实现.md)、[系统通知](../系统/系统通知-设计与实现.md)（本模块不替代二者）  
> **QA：** [BBS论坛-测试对照说明](../../QA/协作/BBS论坛-测试对照说明.md)  
> **帮助：** [论坛](../../../help/pages/论坛_MENU_BBS.md)  
> **入口：** 侧栏「我的」→「论坛」`/bbs`；详情 `/bbs/:id`；发帖 `/bbs/create`  
> **DDL：** `scripts/ensure_bbs_postgresql.sql`（启动幂等 SchemaEnsure）  
> **权限：** 读/写 = 登录即可；板块版主见 `bbs_board_moderator`（置顶/关帖/删帖，限本板块）；全局版主 = `bbs.moderate` 或 SYS_ADMIN / SYS_MANAGER；**仅 SYS_ADMIN / SYS_MANAGER 可设置板块版主**

---

## 1. 目标与非目标

### 1.1 目标（MVP）

1. 公司内部论坛：发帖、看帖、回复。  
2. 主题类型（MVP）：**公司通告**、**行业快讯**、**交流分享**、**操作说明**、**优化建议**、**系统更新**。  
3. 置顶区与普通列表分离；详情浏览计数 +1（原子更新）。  
4. 关闭主题不可回复；软删除主题/回复。  
5. 匿名发帖/回复：非版主显示 `*****`，版主/管理员显示 `姓名【匿名】`。  
6. 版主：置顶/取消置顶、关闭/打开、删除他人帖与回复（板块版主仅本类型；全局版主全板块）。  
7. 正文支持 **Markdown** 与 **富文本**（字号/字体/颜色等）；主题可附图片/视频（见 §4.3）。
8. **板块版主**：每个主题类型可指定 1 名账号；侧栏板块名下显示版主账号；超管可设置/更换/清除。  
9. **系统更新 / 操作说明**：不设版主；**仅 SYS_ADMIN 可发帖**；其他角色可浏览与回复。

### 1.2 非目标（MVP 明确不做）

| 项 | 说明 |
|----|------|
| 投票 | **已实现（P1）**：投票帖（kind=1）独立选项；一人一票不可改；投后/作者版主可见统计；截止后全员可见 |
| 点赞 | 一期；须服务端防重 |
| @提及 | 不做；桥接系统通知亦不做 |
| IsNotify / 铃铛角标 | 不做；强制弹窗仍走系统公告 |
| 微信端 / WX API | 不做 |
| EBS `aux_bbs_*` 历史迁移 | **空库起步** |
| 回复附媒体 / 转码 | 不做；媒体仅主题发帖/编辑；原样存不转码 |

### 1.3 产品定稿（已冻结）

| 项 | 口径 |
|----|------|
| 菜单名 | **论坛** |
| 入口 | 侧栏「我的」下，登录可见 |
| 默认可读 | **全员**（登录即可） |
| 默认可写 | **全员**（登录即可发帖/回复）；版主另赋 |
| 投票规则（一期） | **投票帖**：一人一票（多选一次提交多项）；不可改票；与普通回复分离 |
| 历史数据 | 空库起步 |
| @与通知 | MVP 完全不做 |
| 微信 | 不做 |

---

## 2. 与系统公告 / 系统通知的边界

| 能力 | 归属 |
|------|------|
| 运维强制必看弹窗 | **系统公告** |
| 点对点铃铛消息 | **系统通知** |
| 员工内部交流（通告/快讯/分享/说明/建议/更新）与后续投票 | **论坛（本模块）** |

论坛帖子**不**写入 `sys_announcement` / `sys_user_notice`（MVP）。

---

## 3. 对照 EBS 的差异

| 点 | EBS | FrontCRM |
|----|-----|----------|
| 表 | `aux_bbs_*`（Auxiliary 库） | `bbs_subject` / `bbs_reply`（本库） |
| 投票防重 | 用户+投票项（可多投项） | 一期：用户+主题唯一 |
| 点赞 | 无服务端防重 | 一期补唯一索引 |
| @ | 仅展示 | MVP 不做 |
| 匿名 | `Anonymous` / `IsHideName` 双字段 | 统一 `anonymous` |
| Update 权限 | 服务端未校验 | 创建人或版主 |
| ViewCount | 先读后写 | `UPDATE … view_count = view_count + 1` |
| WX | 有 | 无 |

---

## 4. 数据模型（MVP）

### 4.1 `bbs_subject`

| 列 | 说明 |
|----|------|
| id | varchar(36) PK |
| title | 标题，≤200 |
| content | Markdown 正文 |
| type | 1=公司通告，2=行业快讯，3=交流分享，4=操作说明，5=优化建议，6=系统更新 |
| status | 1=打开 Open，2=关闭 Close |
| is_top / is_hot / anonymous | bool |
| view_count / reply_count | int |
| last_reply_time | timestamptz nullable |
| create_time / create_by / modify_time / modify_by | 审计 |
| is_deleted | 软删 |

### 4.2 `bbs_reply`

| 列 | 说明 |
|----|------|
| id | varchar(36) PK |
| subject_id | 主题 |
| content | 纯文本 |
| anonymous | bool |
| create_time / create_by / modify_time / modify_by | 审计 |
| is_deleted | 软删 |

一期再加：`bbs_vote_item` / `bbs_vote_record`（ux: user+subject）/ `bbs_thumbs_up`（ux: user+reply）。

### 4.3 主题媒体（`upload_document`，`bizType = BBS_SUBJECT`）

复用文档模块；不单独建媒体表。正文内嵌 Markdown 图片 / `<video>`，`src` 指向 `/api/v1/documents/{id}/preview`。

| 项 | 口径 |
|----|------|
| 范围 | **仅主题**发帖/编辑；回复不附媒体 |
| 格式 | 图 jpg/png/webp/gif；视频 mp4/webm；**不转码**，原样存 |
| 限额 | 图 ≤50 张且单张 ≤5MB；视频 ≤1 个且 ≤50MB |
| 时序 | 新建：本地暂存；选文件或点「插入」写入光标处占位；发帖区左右实时预览；发布拿 id 后上传并把占位换成正式 preview 路径 |
| 删除 | 仅作者或版主（通用 documents 删除对 `BBS_SUBJECT` 拒绝） |
| 匿名 | 展示匿名；`upload_user_id` 仍记实际上传人（审计） |

---

## 5. API（`/api/v1/bbs`）

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | `/subjects/top` | 登录 |
| GET | `/subjects` | 登录（分页；排除置顶） |
| GET | `/subjects/{id}` | 登录（浏览 +1） |
| POST | `/subjects` | 登录 |
| PUT | `/subjects/{id}` | 创建人或**全局**版主（板块版主不可改他人正文） |
| POST | `/subjects/{id}/close` `/open` | 版主（本板块或全局）或创建人 |
| POST | `/subjects/{id}/top` `/untop` | 版主（本板块或全局） |
| DELETE | `/subjects/{id}` | 创建人或版主（本板块或全局） |
| GET | `/subjects/{id}/replies` | 登录 |
| POST | `/subjects/{id}/replies` | 登录（主题须 Open） |
| DELETE | `/replies/{id}` | 创建人或版主（本板块或全局） |
| GET | `/subjects/{id}/media` | 登录 |
| POST | `/subjects/{id}/media` | 作者或版主（multipart；体 ≤60MB） |
| DELETE | `/media/{documentId}` | 作者或版主 |
| GET | `/board-moderators` | 登录（侧栏展示；含内置 + 自定义板块） |
| POST | `/board-moderators` | **仅** SYS_ADMIN / SYS_MANAGER；新建自定义板块（type≥100） |
| PUT | `/board-moderators/sort` | **仅** SYS_ADMIN / SYS_MANAGER；body `{ orderedTypes }` 拖放排序 |
| PUT | `/board-moderators/{type}` | **仅** SYS_ADMIN / SYS_MANAGER；body `{ userId, displayName, sortOrder? }` |
| DELETE | `/board-moderators/{type}` | **仅** SYS_ADMIN / SYS_MANAGER；无帖时软删板块 |

版主判定：

- **全局：** `IsSysAdmin || IsSysManager || PermissionCodes 含 bbs.moderate`
- **板块：** `bbs_board_moderator.subject_type = 主题 Type` 且 `user_id = 当前用户`
- 可管理某帖 = 全局版主 **或** 该帖 Type 的板块版主

表 `bbs_board_moderator`：主键 `subject_type`（每板块至多 1 人）；字段 `user_id` / `update_by` / `update_time`。

---

## 6. 业务规则（MVP）

1. 置顶帖仅出现在 top 接口，普通列表 `is_top = false`。  
2. 详情每次打开 `view_count + 1`（原子）。  
3. 回复成功：`reply_count + 1`，`last_reply_time = now`；若 `reply_count > 50` 则 `is_hot = true`。  
4. `status = Close` 禁止回复。  
5. 删除均为软删；列表/详情排除已删。  
6. 匿名展示规则见 §1.1。  
7. 发帖类型：内置 1–6 + 超管新建的自定义板块（type≥100，须存在且未删除）。  
8. 主题媒体限额与鉴权见 §4.3；通用 `/api/v1/documents` 不得直接上传/删除 `BBS_SUBJECT`。
9. 侧栏「分类」旁设置：可新建板块；「置顶 / 系统更新 / 操作说明」顺序固定，其余板块拖放排序。

---

## 7. 前端

| 页 | 路由 | 说明 |
|----|------|------|
| 列表 | `/bbs` | Discussions 风格：分类侧栏 + 置顶/列表；发帖按钮 |
| 发帖/编辑 | `/bbs/create`、`/bbs/:id/edit` | 富文本（字号/字体/颜色）或 Markdown；媒体本地暂存后上传 |
| 详情 | `/bbs/:id` | 正文（含鉴权 blob 预览图/视频）、操作条、纯文本回复流 |

菜单：`layout.menu.bbs` →「论坛」；帮助注册 `MENU_BBS`。

---

## 8. 分期

| 期 | 内容 |
|----|------|
| **MVP** | 本文 §1.1 + §4.3 主题媒体 |
| **一期** | 主题赞踩；**投票帖**（选项/单多选/截止/投后看结果）；列表热帖筛选 |
| **二期** | @ + 系统通知推送；可选论坛「通知类」帖与铃铛策略 |

---

## 9. 实现清单

| 层 | 路径 |
|----|------|
| 常量/DTO/接口 | `CRM.Core/Constants/BbsCodes.cs`、`Models/Bbs/*`、`Interfaces/IBbsService.cs` |
| 服务/Schema | `CRM.Infrastructure/Bbs/*` |
| DbContext | `BbsSubjects` / `BbsReplies` |
| API | `CRM.API/Controllers/BbsController.cs`（媒体）；`DocumentsController` 拦截 `BBS_SUBJECT` |
| 前端 | `CRM.Web/src/views/Bbs/*`、`api/bbs.ts`、`sanitizeAnnouncementHtml.ts` |
| SQL | `scripts/ensure_bbs_postgresql.sql` |

---

## 10. 修订

| 日期 | 说明 |
|------|------|
| 2026-09-30 | 定稿产品决策；开写 MVP |
| 2026-10-01 | 主题媒体：仅发帖/编辑、本地暂存后上传、格式与限额、作者/版主删、匿名记上传人 |
| 2026-10-01 | 发帖支持富文本（字号/字体/颜色）与 Markdown 双模式；正文 HTML/MD 自动识别渲染 |
| 2026-10-01 | 按板块设置版主（bbs_board_moderator）；侧栏展示；仅超管可设；版主权限按 Type |
| 2026-10-01 | 分类设置：新建自定义板块、拖放排序；固定「置顶/系统更新/操作说明」 |
| 2026-10-01 | 投票帖 P1：发起投票、单多选、截止、一人一票、投后/作者版主看统计、截止后公开 |
