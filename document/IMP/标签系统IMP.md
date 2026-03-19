标签系统 实现方案

### 总体思路

按照 PRD，把“标签系统”做成**可复用的基础能力模块**（类似文档模块），对所有业务实体开放统一接口：  
- 后端：标签定义 + 标签关联 + 用户偏好 三张表 + 服务层 + API。  
- 前端：通用标签选择器组件 + 标签展示组件 + 标签筛选组件 + 各业务页面轻量集成。  
- 分阶段实现：先打通「客户/供应商」一条完整链路，再扩展其它实体与高级功能。

---

### 一、后端设计方案

#### 1. 数据表设计（EF 实体）

1. **标签定义表 `TagDefinition`**
   - 主键：`Id` (GUID, `BaseGuidEntity`)
   - 基本字段：
     - `Name`：标签名称（必填，<=50）
     - `Code`：编码（系统标签必填，唯一；用户标签可空或自动生成）
     - `Color`：颜色（如 `#FF6B6B`）
     - `Icon`：可选图标标识
     - `Type`：`short`（1=系统标签，2=用户自定义）
     - `Category`：分类（如“优先级/客户类型/跟进状态”）
     - `Scope`：适用范围（实体编码集合，JSON 字符串或逗号分隔：`CUSTOMER,CONTACT,...`）
     - `Status`：`short`（1=启用，0=停用）
     - `SortOrder`：排序权重（int）
     - `UsageCount`：使用次数（long）
   - 权限相关：
     - `OwnerUserId`：创建者（系统标签为 null 或 0）
     - `Visibility`：`short`（1=仅自己，2=团队共享，3=全局公开）
   - 通用字段：`CreateTime/CreateUserId/ModifyTime/ModifyUserId`（继承）

2. **标签关联表 `TagRelation`**
   - 主键：`Id` (GUID)
   - 字段：
     - `TagId`：引用 `TagDefinition`
     - `EntityType`：实体编码（`CUSTOMER/CONTACT/SALES_ORDER/STOCK_IN...`）
     - `EntityId`：业务记录 ID（字符串 GUID）
     - `AppliedByUserId`：打标签人
     - `AppliedTime`：打标签时间
     - `Source`：标签来源（`Manual/Batch/System`）
   - 索引：
     - `(EntityType, EntityId)`
     - `TagId`
     - `(EntityType, TagId)`（筛选优化）

3. **用户标签偏好表 `UserTagPreference`**
   - 复合主键：`UserId + TagId`
   - 字段：
     - `UserId`：long
     - `TagId`：GUID
     - `UseCount`：使用次数
     - `LastUsedTime`
     - `IsFavorite`：是否收藏
   - 索引：`UserId`、`(UserId, IsFavorite)`。

> 集成方式：在 `ApplicationDbContext` 新增 `DbSet<TagDefinition> Tags; DbSet<TagRelation> TagRelations; DbSet<UserTagPreference> UserTagPreferences;`，并配置约束与索引。

---

#### 2. 服务层设计

接口建议在 `CRM.Core.Interfaces` / `CRM.Core.Tag` 下：

1. **标签管理服务 `ITagService`**
   - `Task<PagedResult<TagDefinition>> SearchTagsAsync(TagSearchRequest req)`  
   - `Task<TagDefinition> CreateTagAsync(CreateTagRequest req)`（区分系统 / 用户标签）  
   - `Task<TagDefinition> UpdateTagAsync(string id, UpdateTagRequest req)`  
   - `Task DisableTagAsync(string id)` / `Task EnableTagAsync(string id)`  
   - `Task DeleteTagAsync(string id)`（仅未被使用的可物理删）  
   - `Task<TagDefinition?> GetByIdAsync(string id)`

   DTO 大致：
   - `TagSearchRequest`：`Type`、`Category`、`EntityType`、`Keyword`、`Status`、分页。
   - `CreateTagRequest`：`Name/Code/Color/Icon/Type/Category/Scope/Visibility`。
   - `UpdateTagRequest`：同上但部分可空；系统标签限制修改字段。

2. **标签应用服务 `ITagApplyService`**
   - 单条 / 批量打标签：
     - `Task AddTagsToEntityAsync(AddTagsToEntityRequest req)`
       - `EntityType`, `EntityIds`, `TagIds`, `AppliedByUserId`, `Source`
   - 单条 / 批量移除：
     - `Task RemoveTagsFromEntityAsync(RemoveTagsFromEntityRequest req)`
   - 查询记录标签：
     - `Task<IReadOnlyList<TagDefinition>> GetTagsForEntityAsync(string entityType, string entityId)`
     - `Task<IReadOnlyList<TagRelation>> GetRelationsForEntityAsync(...)`（内部用）
   - 常用 / 最近使用：
     - `Task<IReadOnlyList<TagDefinition>> GetUserCommonTagsAsync(long userId, string? entityType, int topN = 10)`
     - `Task<IReadOnlyList<TagDefinition>> GetUserRecentTagsAsync(long userId, string? entityType, TimeSpan? range = null)`

   请求 DTO：
   - `AddTagsToEntityRequest`：`EntityType`, `List<string> EntityIds`, `List<string> TagIds`, `AppliedByUserId`, `Source`.
   - `RemoveTagsFromEntityRequest`：同结构。

3. **标签筛选服务 `ITagFilterService`**
   - `Task<IReadOnlyList<string>> QueryEntityIdsByTagsAsync(TagFilterRequest req)`  
     - `EntityType`、`IncludeTagIds`（且/或）、`ExcludeTagIds`，分页或只返回 ID 集合。  
   - 由业务服务（如 `CustomerService`、`VendorService`）在原有查询上组合标签过滤结果。

   `TagFilterRequest`：
   - `EntityType`
   - `IncludeTagIds: List<string>`
   - `IncludeLogic: 'AND' | 'OR'`
   - `ExcludeTagIds: List<string>`
   - `PageNumber/PageSize`（可选）

> 内部实现：对 TagRelation 按 `EntityType`、`TagId` 做分组，使用 LINQ 或原生 SQL 计算满足 AND/OR 的 `EntityId` 集合。

---

#### 3. 接口（API）设计

Controller 建议新建 `TagsController` 与 `TagRelationsController` 或合并为 `TagsController` 中的分组路由。

1. **标签管理 API（后台 / 配置）**
   - `GET /api/v1/tags`  
     - 查询：`type/category/entityType/status/keyword/pageNumber/pageSize`
   - `POST /api/v1/tags`  
     - 创建标签（按当前用户 + type 判断系统 or 用户标签，系统标签入口可封在“系统设置”UI 下）。
   - `PUT /api/v1/tags/{id}`  
   - `PATCH /api/v1/tags/{id}/enable` / `PATCH /api/v1/tags/{id}/disable`  
   - `DELETE /api/v1/tags/{id}`

2. **标签应用 API**
   - `POST /api/v1/tags/apply`  
     - Body: `AddTagsToEntityRequest`（支持 EntityIds 多个）。
   - `POST /api/v1/tags/remove`  
   - `GET /api/v1/tags/entities/{entityType}/{entityId}`  
     - 返回该记录所有标签（含颜色、类型等）。
   - `GET /api/v1/tags/user/common` / `GET /api/v1/tags/user/recent`  
     - Query: `entityType?`，使用当前登录用户 ID。

3. **标签筛选 API**
   - `POST /api/v1/tags/filter`  
     - Body: `TagFilterRequest`  
     - 返回 `{ entityIds: string[] }`（由前端或业务服务再去查询具体数据）。

> 集成方式：比如客户列表查询接口多加一个 `tagIds`、`tagLogic` 参数，服务层先调用 `ITagFilterService` 拿到符合条件 `customerIds`，然后在 EF 查询中再 `where id in (ids)`。

---

### 二、前端开发方案

#### 1. API 模块

新建 `src/api/tag.ts`：

- `getTags(params)`：管理列表。
- `createTag(data)` / `updateTag(id,data)` / `enableTag(id)` / `disableTag(id)` / `deleteTag(id)`。
- `getEntityTags(entityType, entityId)`。
- `applyTagsToEntities(payload)` / `removeTagsFromEntities(payload)`。
- `getUserCommonTags(entityType?)` / `getUserRecentTags(entityType?)`。
- `filterEntitiesByTags(payload)`（用于高级筛选）。

统一走 `apiClient`，兼容 `{ success, data }` 响应格式。

#### 2. 通用组件

1. **标签展示组件 `TagListDisplay.vue`**
   - Props：
     - `tags: TagDto[]`
     - `maxCount?: number`（列表中最多显示 N 个，多余显示 `+N`）
     - `size: 'sm' | 'md'`
   - UI：彩色圆角矩形，自动计算文本颜色（黑/白）。

2. **标签选择器组件 `TagSelector.vue`**
   - 功能：
     - 搜索 + 分类分组展示标签。
     - 多选 + 已选区域展示。
     - 支持创建新标签（只在用户标签场景打开）。
     - 显示使用次数、颜色。
   - Props：
     - `entityType: string`
     - `multiple?: boolean`（列表批量打标签时多选）
     - `allowCreate?: boolean`
   - Emits：
     - `update:modelValue`（选中 TagId 列表）
     - `confirm(selectedTags)`。

3. **打标签弹窗 `ApplyTagsDialog.vue`**
   - 内部使用 `TagSelector`，在客户/供应商列表、详情页调用：
   - Props：
     - `entityType`
     - `entityIds: string[]`
   - 点击确认后调用 `tagApi.applyTagsToEntities`。

4. **标签筛选组件 `TagFilterBar.vue`**
   - 集成到列表页的筛选区：
     - 展示已选标签（包含/排除）。
     - 支持添加 Label（包含/排除）、选择逻辑（且/或）。
   - Emits：
     - `change({ includeTagIds, excludeTagIds, logic })` → 父组件在请求参数中带上。

---

### 三、业务页面集成方案（最小闭环）

建议分两步：

1. **第一阶段：客户 / 供应商详情 + 列表**
   - 在 `CustomerDetail.vue` / `VendorDetail.vue` 标题下增加标签展示区域：
     - 使用 `getEntityTags('CUSTOMER', id)` 拉取并显示。
     - 提供“添加标签”按钮 → 弹出 `ApplyTagsDialog`，对当前单条记录打标签。
   - 在 `CustomerList.vue` / `VendorList.vue`：
     - 在列表行右侧加 `TagListDisplay`（最多 3 个标签）。
     - 顶部筛选区加入 `TagFilterBar`，将其输出参数透传给查询 API。

2. **第二阶段：销售订单 / 入库单 / 其他实体**
   - 按同样模式增量接入：
     - 销售订单：支持 “高优先级 / 大额订单 / 账期客户”等标签。
     - 入库单：标记 “质检异常 / 退货入库”等。

---

### 四、开发阶段与优先级（简版）

1. **阶段一：后端基础（P0）**
   - 实体 + DbContext + Migrations（3 张表）。
   - `ITagService` / `ITagApplyService` / `ITagFilterService` + 实现。
   - 管理与应用 API 完成，并通过 Postman/Swagger 验证。

2. **阶段二：前端基础组件（P0）**
   - `tagApi` 模块。
   - `TagListDisplay`、`TagSelector`、`ApplyTagsDialog`、`TagFilterBar`。

3. **阶段三：客户/供应商接入（P0）**
   - 在 `CustomerDetail`、`CustomerList`、`VendorDetail`、`VendorList` 集成打标签、展示、筛选。

4. **阶段四：扩展与优化（P1）**
   - 用户标签管理页（“我的标签”）。
   - 系统标签管理后台页面。
   - 常用/最近使用标签优化标签选择器的“快捷区”。
   - 标签云（可作为 Dashboard 一个统计卡片）。

---

如果你确认这个方案，我可以下一步直接在当前代码里：  
- 创建标签相关实体与 DbSet、迁移；  
- 实现后端服务与 API；  
- 然后先在「客户」模块接入一套最小可用打标签/展示/筛选闭环。