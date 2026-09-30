# BBS 论坛 — 设计与实现

> **状态：** MVP（已实现代码；须跑 SQL / SchemaEnsure 并重启 API）  
> **EBS 对照（只读）：** [document/EBS/辅助功能/BBS.md](../../EBS/辅助功能/BBS.md)  
> **关联：** [系统公告](../系统/系统公告-设计与实现.md)、[系统通知](../系统/系统通知-设计与实现.md)（本模块不替代二者）  
> **QA：** [BBS论坛-测试对照说明](../../QA/协作/BBS论坛-测试对照说明.md)  
> **帮助：** [论坛](../../../help/pages/论坛_MENU_BBS.md)  
> **入口：** 侧栏「我的」→「论坛」`/bbs`；详情 `/bbs/:id`；发帖 `/bbs/create`  
> **DDL：** `scripts/ensure_bbs_postgresql.sql`（启动幂等 SchemaEnsure）  
> **权限：** 读/写 = 登录即可；版主 `bbs.moderate` 或 SYS_ADMIN / SYS_MANAGER

---

## 1. 目标与非目标

### 1.1 目标（MVP）

1. 公司内部论坛：发帖、看帖、回复。  
2. 主题类型（MVP）：**公司通告**、**行业快讯**、**交流分享**、**操作说明**、**优化建议**、**系统更新**。  
3. 置顶区与普通列表分离；详情浏览计数 +1（原子更新）。  
4. 关闭主题不可回复；软删除主题/回复。  
5. 匿名发帖/回复：非版主显示 `*****`，版主/管理员显示 `姓名【匿名】`。  
6. 版主：置顶/取消置顶、关闭/打开、删除他人帖与回复。  
7. 正文 Markdown（与系统公告一致思路）；无附件。

### 1.2 非目标（MVP 明确不做）

| 项 | 说明 |
|----|------|
| 投票 | 一期；规则定稿为**一主题一票** |
| 点赞 | 一期；须服务端防重 |
| @提及 | 不做；桥接系统通知亦不做 |
| IsNotify / 铃铛角标 | 不做；强制弹窗仍走系统公告 |
| 微信端 / WX API | 不做 |
| EBS `aux_bbs_*` 历史迁移 | **空库起步** |
| 富文本编辑器 / 附件 | 不做；Markdown 纯文本框 |

### 1.3 产品定稿（已冻结）

| 项 | 口径 |
|----|------|
| 菜单名 | **论坛** |
| 入口 | 侧栏「我的」下，登录可见 |
| 默认可读 | **全员**（登录即可） |
| 默认可写 | **全员**（登录即可发帖/回复）；版主另赋 |
| 投票规则（一期） | **一主题一票**（与 EBS「可多投项」不同） |
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
| content | Markdown |
| anonymous | bool |
| create_time / create_by / modify_time / modify_by | 审计 |
| is_deleted | 软删 |

一期再加：`bbs_vote_item` / `bbs_vote_record`（ux: user+subject）/ `bbs_thumbs_up`（ux: user+reply）。

---

## 5. API（`/api/v1/bbs`）

| 方法 | 路径 | 权限 |
|------|------|------|
| GET | `/subjects/top` | 登录 |
| GET | `/subjects` | 登录（分页；排除置顶） |
| GET | `/subjects/{id}` | 登录（浏览 +1） |
| POST | `/subjects` | 登录 |
| PUT | `/subjects/{id}` | 创建人或版主 |
| POST | `/subjects/{id}/close` `/open` | 版主或创建人 |
| POST | `/subjects/{id}/top` `/untop` | 版主 |
| DELETE | `/subjects/{id}` | 创建人或版主 |
| GET | `/subjects/{id}/replies` | 登录 |
| POST | `/subjects/{id}/replies` | 登录（主题须 Open） |
| DELETE | `/replies/{id}` | 创建人或版主 |

版主判定：`IsSysAdmin || IsSysManager || PermissionCodes 含 bbs.moderate`。

---

## 6. 业务规则（MVP）

1. 置顶帖仅出现在 top 接口，普通列表 `is_top = false`。  
2. 详情每次打开 `view_count + 1`（原子）。  
3. 回复成功：`reply_count + 1`，`last_reply_time = now`；若 `reply_count > 50` 则 `is_hot = true`。  
4. `status = Close` 禁止回复。  
5. 删除均为软删；列表/详情排除已删。  
6. 匿名展示规则见 §1.1。  
7. MVP 发帖类型仅允许上述六类（1–6）。

---

## 7. 前端

| 页 | 路由 | 说明 |
|----|------|------|
| 列表 | `/bbs` | 置顶区 + 分页列表；筛选类型/状态/关键词；发帖按钮 |
| 发帖 | `/bbs/create` | 标题、类型、匿名、Markdown 正文 |
| 详情 | `/bbs/:id` | 正文、操作条、回复流、回复框 |

菜单：`layout.menu.bbs` →「论坛」；帮助注册 `MENU_BBS`。

---

## 8. 分期

| 期 | 内容 |
|----|------|
| **MVP** | 本文 §1.1 |
| **一期** | 投票（一主题一票）+ 点赞防重 + 列表热帖筛选 |
| **二期** | @ + 系统通知推送；可选论坛「通知类」帖与铃铛策略 |

---

## 9. 实现清单

| 层 | 路径 |
|----|------|
| 常量/DTO/接口 | `CRM.Core/Constants/BbsCodes.cs`、`Models/Bbs/*`、`Interfaces/IBbsService.cs` |
| 服务/Schema | `CRM.Infrastructure/Bbs/*` |
| DbContext | `BbsSubjects` / `BbsReplies` |
| API | `CRM.API/Controllers/BbsController.cs` |
| 前端 | `CRM.Web/src/views/Bbs/*`、`api/bbs.ts` |
| SQL | `scripts/ensure_bbs_postgresql.sql` |

---

## 10. 修订

| 日期 | 说明 |
|------|------|
| 2026-09-30 | 定稿产品决策；开写 MVP |
