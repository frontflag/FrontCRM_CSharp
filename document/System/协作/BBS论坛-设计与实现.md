# BBS 论坛 — 设计与实现

> **状态：** 已实现（MVP + 板块管理 + 投票帖 P1）；须跑 SQL / SchemaEnsure 并重启 API  
> **EBS 对照（只读）：** [document/EBS/辅助功能/BBS.md](../../EBS/辅助功能/BBS.md)  
> **实施方案：** [BBS论坛-实施方案](../../实现方案/BBS论坛-实施方案.md)  
> **关联：** [系统公告](../系统/系统公告-设计与实现.md)、[系统通知](../系统/系统通知-设计与实现.md)（本模块不替代二者）  
> **QA：** [BBS论坛-测试对照说明](../../QA/协作/BBS论坛-测试对照说明.md)  
> **系统帖规范：** [BBS系统自动生成帖子-规范](./BBS系统自动生成帖子-规范.md)  
> **帮助：** [论坛](../../../help/pages/论坛_MENU_BBS.md)  
> **入口：** 侧栏「我的」→「论坛」`/bbs`；详情 `/bbs/:id`；发帖 `/bbs/create`；发起投票 `/bbs/create?kind=poll`  
> **DDL：** `scripts/ensure_bbs_postgresql.sql`（启动幂等 `BbsSchemaEnsure`）  
> **权限：** 读/写 = 登录即可；板块版主见 `bbs_board_moderator`；全局版主 = `bbs.moderate` 或 SYS_ADMIN / SYS_MANAGER；**仅 SYS_ADMIN / SYS_MANAGER 可设置板块版主 / 新建板块 / 排序**

---

## 1. 功能需求总览

### 1.1 已实现能力

| 域 | 需求要点 |
|----|----------|
| **基础帖** | 发帖、看帖、回复；关闭后不可回复；软删除主题/回复 |
| **板块** | 内置六类 + 自定义板块（type≥100）；侧栏统计（帖数/浏览）；固定区与可调序区分隔线 |
| **板块管理** | 超管：分类旁设置新建板块、拖放排序；各板块设置改名称/版主/删空板块 |
| **固定板块** | 「全部 / 置顶」筛选；「系统更新 / 操作说明」顺序固定、不设版主、仅 SYS_ADMIN 发帖 |
| **置顶 / 热帖** | 置顶区与普通列表分离；回复数 >50 标热帖 |
| **匿名** | 发帖/回复可匿名；非版主见 `*****`，版主/管理员见 `姓名【匿名】` |
| **正文与媒体** | Markdown / 富文本；主题可附图/视频（限额见 §4.3） |
| **赞踩** | 主题赞/踩，每用户每帖一条可切换/取消 |
| **投票帖** | 独立 `kind=poll`；≥2 选项；单选/多选；可选截止；一人一票不可改；与回复分离 |
| **投票可见性** | 投后 / 作者 / 版主可看统计；**截止后全员**可看；不公示谁选了哪项 |
| **系统说明帖** | 各环境启动幂等写入《论坛如何使用》；正文范围与锁定见 §5.1 |

### 1.2 明确不做（本版）

| 项 | 说明 |
|----|------|
| @提及 / 铃铛桥接 | 不做；强制弹窗仍走系统公告 |
| 微信端 | 不做 |
| EBS 历史迁移 | 空库起步 |
| 回复附媒体 / 转码 | 媒体仅主题；原样存 |
| 改票 | 提交后不可改（P1） |
| 公示选民名单 | 选票实名落库防重，界面不展示「谁选了哪项」 |

### 1.3 产品定稿（冻结）

| 项 | 口径 |
|----|------|
| 菜单名 | **论坛** |
| 入口 | 侧栏「我的」下，登录可见 |
| 默认可读 / 可写 | 全员登录即可读、发帖、回复 |
| 投票与回复 | **方案 A**：投票与回复分离；一人一票；回复不限次数 |
| 选项数 | 最少 **2**，最多 20 |
| 截止时间 | **选填**；不填则主题打开期间可投 |
| 截止后未投票 | **可看**统计结果 |
| 投前看结果 | **作者 / 版主可看**；普通用户须先投（截止前） |
| 多选上限 | 选填；不填 = 可选全部；填则 `2 ≤ max ≤ 选项数` |
| 谁能发投票 | 与普通发帖相同（系统更新/操作说明仍仅超管） |
| 投票帖匿名/媒体 | 与普通帖相同；选票实名落库 |

### 1.4 与系统公告 / 通知边界

| 能力 | 归属 |
|------|------|
| 运维强制必看弹窗 | **系统公告** |
| 点对点铃铛 | **系统通知** |
| 内部交流、板块、投票 | **论坛** |

论坛帖**不**写入 `sys_announcement` / `sys_user_notice`。

---

## 2. 对照 EBS

| 点 | EBS | FrontCRM |
|----|-----|----------|
| 表 | `aux_bbs_*` | 本库 `bbs_*` |
| 投票 | 用户+投票项可多投项 | 投票帖：用户+主题唯一；选项独立表 |
| 点赞 | 无服务端防重 | `bbs_reaction` 唯一索引 |
| @ / 微信 | 有 | 不做 |
| 匿名 | 双字段 | 统一 `anonymous` |
| ViewCount | 先读后写 | 原子 `view_count + 1` |

---

## 3. 数据模型

### 3.1 `bbs_subject`

| 列 | 说明 |
|----|------|
| id / title / content | 主键、标题≤200、正文 |
| type | 板块：1～6 内置；≥100 自定义 |
| status | 1 Open / 2 Close |
| is_top / is_hot / anonymous / **is_system** | bool；`is_system` 为系统启动写入的说明帖 |
| view_count / reply_count / like_count / dislike_count | int |
| last_reply_time | timestamptz |
| **kind** | 0 普通帖 / **1 投票帖** |
| **vote_mode** | 0 无；1 单选；2 多选 |
| **vote_max_choices** | 多选上限；空=不限 |
| **vote_deadline** | UTC 截止；空=不限（关帖仍停投） |
| **vote_count** | 已投票人数（每人计 1） |
| 审计 / is_deleted | 常规 |

### 3.2 `bbs_reply` / `bbs_reaction`

- 回复：纯文本；关帖不可回。  
- 赞踩：`target_type`+`target_id`+`user_id` 唯一；主题赞踩已实现。

### 3.3 板块 `bbs_board_moderator`

主键 `subject_type`；`user_id`（版主可空）、`display_name`、`sort_order`、`is_deleted`、审计。

### 3.4 投票表

| 表 | 说明 |
|----|------|
| `bbs_poll_option` | 选项文案、sort_order、软删 |
| `bbs_poll_vote` | 一人一帖一行；`UNIQUE(subject_id, user_id)` |
| `bbs_poll_vote_item` | 一次投票选中的选项（多选多行） |

### 3.5 主题媒体

复用 `upload_document`，`bizType = BBS_SUBJECT`。图 ≤50×5MB；视频 ≤1×50MB；仅主题。

---

## 4. API（`/api/v1/bbs`）

### 4.1 主题 / 回复 / 媒体 / 赞踩

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | `/subjects/top`、`/subjects`、`/subjects/{id}` | 登录（详情浏览 +1） |
| POST / PUT / DELETE | `/subjects`、`/subjects/{id}` | 登录；编辑=创建人或全局版主 |
| POST | `/subjects/{id}/close|open|top|untop` | 版主或创建人（置顶需版主） |
| GET/POST | `/subjects/{id}/replies`；DELETE `/replies/{id}` | 登录；删=本人或版主 |
| GET/POST/DELETE | 媒体相关 | 作者或版主 |
| POST | `/subjects/{id}/reaction` | 登录 |

### 4.2 板块

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | `/board-moderators` | 登录 |
| POST | `/board-moderators` | 超管；新建自定义板块 |
| PUT | `/board-moderators/sort` | 超管；`{ orderedTypes }` |
| PUT | `/board-moderators/{type}` | 超管；名称/版主 |
| DELETE | `/board-moderators/{type}` | 超管；无帖可软删 |

### 4.3 投票

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/subjects` | `kind=1` + `voteMode` + `pollOptions[]` + 可选 `voteDeadline` / `voteMaxChoices` |
| POST | `/subjects/{id}/poll/vote` | `{ optionIds }`；防重、校验单多选与截止 |
| GET | `/subjects/{id}` | 详情含 `poll`：选项、`hasVoted`、`canVote`、`canSeeStats`、条件性票数/占比 |

版主判定：全局（超管 / `bbs.moderate`）或 `bbs_board_moderator` 对本 Type。

---

## 5. 业务规则摘要

1. 置顶仅 top 接口；普通列表排除置顶。  
2. 详情每次 `view_count + 1`。  
3. 回复：`reply_count` / `last_reply_time`；>50 → `is_hot`。  
4. Close：禁回复、禁投票。  
5. 软删不出现在列表/详情。  
6. 自定义板块须存在且未删除方可发帖。  
7. 侧栏：固定「系统更新 / 操作说明」→ 分隔线 → 可拖放板块。  
8. **投票**：选项 ≥2；一人一票不可改；截止不可早于当天（前端禁用 + 服务端须晚于当前时刻）；有票后不可改选项。  
9. **统计可见**：`hasVoted || 作者 || 版主 || 已截止`。  
10. 媒体限额与 documents 拦截见 §3.5。  
11. **系统说明帖**见 §5.1。指定板块列表第 1 页须带上该板块的置顶帖（含系统帖），不能只在「全部」里出现。

### 5.1 系统说明帖《论坛如何使用》（锁定）

启动时由 `BbsOpsGuidePost` 读取 `CRM.Infrastructure/Bbs/SystemPosts/*.md`，按文件头主键幂等写入。新建「操作说明」只加 Markdown 和配图。注意事项见 [BBS系统自动生成帖子-规范](./BBS系统自动生成帖子-规范.md)。

---

## 6. 前端

| 页 | 路由 | 说明 |
|----|------|------|
| 列表 | `/bbs` | 侧栏分类 +「发帖」「发起投票」；投票角标与人数 |
| 发帖/投票 | `/bbs/create`、`?kind=poll` | 投票表单：模式/截止/选项 |
| 编辑 | `/bbs/:id/edit` | 有票后选项锁定 |
| 详情 | `/bbs/:id` | 投票区 + 赞踩 + 回复。赞踩与正文隔开两行空白，不与正文混在一起 |

关键组件：`BbsCategoryAside`（板块管理/排序）、`BbsRichEditor`。

---

## 7. 分期

| 期 | 内容 | 状态 |
|----|------|------|
| **MVP** | 发帖/回复/置顶/匿名/媒体/六类板块 | ✅ |
| **板块增强** | 版主、自定义板块、拖放排序、固定区 UI | ✅ |
| **一期 P1** | 主题赞踩、**投票帖**（含截止与可见性） | ✅ |
| **二期** | @ + 系统通知；可选改票 / 导出结果 | 未做 |

---

## 8. 实现清单

| 层 | 路径 |
|----|------|
| 常量/DTO/实体 | `CRM.Core/Constants/BbsCodes.cs`、`Models/Bbs/*`、`Interfaces/IBbsService.cs` |
| 服务 / Schema | `CRM.Infrastructure/Bbs/*` |
| DbContext | `BbsSubjects` / `BbsReplies` / `BbsPoll*` / `BbsBoardModerators` |
| API | `CRM.API/Controllers/BbsController.cs`；`DocumentsController` 拦截 `BBS_SUBJECT` |
| 前端 | `CRM.Web/src/views/Bbs/*`、`components/Bbs/*`、`api/bbs.ts` |
| SQL | `scripts/ensure_bbs_postgresql.sql` |

---

## 9. 修订

| 日期 | 说明 |
|------|------|
| 2026-09-30 | 定稿产品决策；开写 MVP |
| 2026-10-01 | 主题媒体、富文本/Markdown、板块版主 |
| 2026-10-01 | 分类设置：新建板块、拖放排序 |
| 2026-10-01 | 投票帖 P1 |
| 2026-10-01 | **整理全文**：功能需求 + 实现口径汇总（本文） |
| 2026-10-03 | 系统说明帖注意事项单独立为 [BBS系统自动生成帖子-规范](./BBS系统自动生成帖子-规范.md)。操作说明正文不写管理员板块操作 |
| 2026-10-03 | 详情页赞踩与正文隔开两行空白 |
