上传文档管理系统 实施方案

### 总体思路（按“可复用独立模块”重构）

在原有方案基础上，强化**模块化与可复用**：把“上传文档管理”做成一个**独立的后端库 + 可选前端子模块**，对业务层只暴露少量清晰接口（`IDocumentService` + HTTP API），业务代码不直接操作存储细节。下面的方案分为**后端模块设计、前端模块设计、在本项目中的集成路径**三部分。

---

### 一、后端独立模块设计

#### 1.1 模块边界与项目结构

建议新增独立类库（或后续抽取到单独 Git 仓库）：

- `CRM.Document`（或 `FrontCRM.DocumentModule`）：
  - 只依赖：
    - `CRM.Core`（共用 `BaseEntity`、`IUnitOfWork` 等）或定义一层最小接口（更彻底解耦）。
  - 提供：
    - 实体：`UploadDocument`
    - 配置：`DocumentModuleOptions`
    - 服务接口：`IDocumentService`、`IFileStorageService`
    - DTO：`DocumentDto`、`UploadResultDto` 等
    - 扩展方法：`AddDocumentModule(this IServiceCollection, IConfiguration)`
    - 控制器：可选两种模式
      - A. 直接在模块里定义 `DocumentsController`，通过 Endpoint Routing 添加。
      - B. 只提供 `MapDocumentEndpoints` 扩展，由宿主 API 项目决定是否映射。

在当前项目中，第一步可以先**按目录模拟模块**，后续再抽成独立 `.csproj`：

- `CRM.Core/Document/*`（接口、实体、DTO）
- `CRM.Infrastructure/Document/*`（实现、文件存储）
- `CRM.API/Controllers/DocumentsController.cs`（控制器）
- 完成后，再抽到 `CRM.Document` 独立项目，只改引用和命名空间。

#### 1.2 通用实体与 DbContext 集成

**实体：`UploadDocument`**

放到 `CRM.Core.Document` 命名空间下（未来移动到 Document 模块）：

- 字段（支持所有业务场景）：
  - `Id`：GUID（继承 `BaseGuidEntity`）
  - `BizType`：string（枚举，如 `"Customer"`, `"SalesOrder"`）
  - `BizId`：string（业务主键或业务编号）
  - `OriginalFileName`、`StoredFileName`
  - `RelativePath`：相对 `RootPath` 的路径（`UploadFile/...`）
  - `FileSize`（long）、`FileExtension`、`MimeType`
  - `Remark`
  - `IsDeleted`、`DeleteTime`、`DeleteUserId`
  - `CreateTime`、`CreateUserId`（来自 `BaseEntity`）

**DbContext 扩展**

- 在 `ApplicationDbContext` 中增加：
  - `public DbSet<UploadDocument> UploadDocuments { get; set; } = null!;`
- `OnModelCreating` 中配置：
  - 主键、长度约束（文件名 255 / 500，备注 256）
  - 索引：
    - `HasIndex(e => new { e.BizType, e.BizId })`
    - `HasIndex(e => e.CreateTime)`

> 模块化角度：未来若提取为独立 dbContext，可通过 `ModelBuilder.ApplyConfiguration` 或单独 `IEntityTypeConfiguration` 来复用。

#### 1.3 模块配置与序号生成

**DocumentModuleOptions**（独立配置对象）：

- `RootPath`：`D:\FrontCRM_Uploads` / Linux 路径
- `MaxFileSizeMb`：50
- `MaxFilesPerUpload`：5
- `AllowedExtensions`：白名单列表（`[".pdf",".jpg",".jpeg",".png",".docx",".xlsx",".zip"]`）
- `Thumbnail`：
  - `Enable`：bool
  - `MaxWidth`，`MaxHeight`，`Quality`

在宿主项目（`CRM.API`）`appsettings.json` 中配置 `"DocumentModule": { ... }`，模块通过 `IOptions<DocumentModuleOptions>` 获取。

**流水号生成**

- 模式 1（复用现有机制）：在 `SysSerialNumber` 中增加一个 `ModuleCode = "UploadDocument"`，使用已有的 `SerialNumberService`。
- 模式 2（模块自带）：模块内部仅依赖一个简单的 `INumberGenerator` 接口，宿主可注入实现（比如基于数据库/Redis）。当前项目可以适配到现有 `ISerialNumberService`。

命名规则实现（模块内）：

- `FileNameGenerator.Generate(bizType, bizId, originalName)`
  - 查询当天的序列号 N（原子自增）
  - 拼接 `yyMMddNNNNNN_{bizId}_{originalName}`，并对 `bizId`/`originalName` 做非法字符过滤。

#### 1.4 文件存储服务（可复用）

**接口 `IFileStorageService`**（模块暴露）：

- `Task<StoredFileResult> SaveAsync(DocumentSaveContext ctx)`
  - `ctx` 包含：
    - `Stream FileStream`
    - `string OriginalFileName`
    - `string BizType`
    - `string BizId`
    - `string ContentType`
    - `string? UploadUserId`
- `Task<Stream> OpenReadAsync(string relativePath)`
- `Task<bool> ExistsAsync(string relativePath)`
- `Task DeleteAsync(string relativePath)`（可选）

内部实现：

- 路径：`RootPath/UploadFile/{BizType}/{BizId}/StoredFileName`
- 校验：
  - 扩展名白名单
  - 长度（<= MaxSize）
  - 文件头（基于 magic bytes 检测图片/PDF/Office；拒绝 exe/脚本）
- 对图片生成缩略图：`{StoredFileNameWithoutExt}_thumb{ext}` 并记录路径。

**返回结果 `StoredFileResult`**：

- `StoredFileName`
- `RelativePath`
- `FileSize`
- `IsImage`
- `ThumbnailRelativePath`

#### 1.5 文档业务服务（模块的主要 API）

**接口 `IDocumentService`**（跨项目调用入口）：

- 上传：
  - `Task<IReadOnlyList<UploadDocument>> UploadAsync(DocumentUploadRequest request)`
    - `BizType`, `BizId`, `List<DocumentUploadFile> Files`, `string? Remark`, `string UploadUserId`
- 查询：
  - `Task<IReadOnlyList<UploadDocument>> GetByBizAsync(string bizType, string bizId)`
  - `Task<PagedResult<UploadDocument>> SearchAsync(DocumentSearchRequest req)`（系统管理）
- 删除（软删）：
  - `Task SoftDeleteAsync(string documentId, string userId)`
- 单个获取：
  - `Task<UploadDocument?> GetByIdAsync(string id)`

宿主业务（客户、订单等）只依赖 `IDocumentService`，不关注存储细节。

#### 1.6 HTTP API 封装（可选内置 / 可选关闭）

模块提供**默认 Controller**（可放在模块中，也可以是 `Minimal API`）：

- `POST /api/v1/documents/upload`（multipart/form-data）
- `GET /api/v1/documents?bizType=...&bizId=...`
- `GET /api/v1/documents/admin?...`（管理查询）
- `GET /api/v1/documents/{id}/download`
- `GET /api/v1/documents/{id}/preview`
- `DELETE /api/v1/documents/{id}`

**模块化要求**：

- 提供扩展方法：
  - `services.AddDocumentModule(configuration)`  
    - 注册 `IFileStorageService`、`IDocumentService`、相关配置。
  - `endpoints.MapDocumentModuleEndpoints()`  
    - 可选：宿主项目可以控制是否添加默认 API 路由。

---

### 二、前端独立组件设计（Vue 模块化）

#### 2.1 API 封装

在前端创建独立文档 API 模块（未来可抽到共享库）：

- `src/api/document.ts`（与后端 API 对齐）：
  - `uploadDocuments(bizType, bizId, files, remark?)`
  - `getDocuments(bizType, bizId)`
  - `searchDocumentsAdmin(filters)`
  - `deleteDocument(id)`
  - `getPreviewUrl(id)` / `getDownloadUrl(id)`

该模块只依赖 `apiClient` 和后端路径，不依赖具体业务页面。

#### 2.2 通用 UI 组件

**1）`DocumentUploadPanel.vue`**

- Props：
  - `bizType: string`
  - `bizId: string`
- 行为：
  - 拖拽 + 文件选择
  - 数量、大小限制
  - 备注输入
  - 显示上传进度
- Emits：
  - `uploaded(documents: UploadDocumentDto[])`（供父组件刷新列表）

**2）`DocumentListPanel.vue`**

- Props：
  - `bizType: string`
  - `bizId: string`
  - 可选 `viewMode: 'grid'|'list'`
- 行为：
  - 拉取 `getDocuments`
  - 展示缩略图/文件名/上传时间/上传人/备注
  - “预览”/“下载”/“删除”

**3）`DocumentPreviewDialog.vue`**

- 内部逻辑：
  - 图片 → `el-image` + viewer
  - PDF → `iframe` 加载 `/preview` 地址
  - 其他 → 下载提示

> 模块化：这三个组件不依赖具体“客户/订单”页面，只吃 `bizType`+`bizId`，后续任何项目只要有这两个字段即可集成。

#### 2.3 集成方式（对业务透明）

在任意详情页中集成方式统一：

```vue
<DocumentUploadPanel bizType="SalesOrder" :bizId="orderId" @uploaded="refreshDocuments" />
<DocumentListPanel bizType="SalesOrder" :bizId="orderId" />
```

对其他项目，只要复用这套前端组件和同样的后端 API，即可无缝添加文档功能。

---

### 三、在当前项目中的落地顺序（按模块化实现）

1. **封装后端模块（最小闭环）**
   - 在 `CRM.Core` / `CRM.Infrastructure` 里按上面结构实现 `UploadDocument` + `IFileStorageService` + `IDocumentService`。
   - 在 `CRM.API` 里通过 `AddDocumentModule` + `MapDocumentModuleEndpoints` 暴露 API。
   - 完成文档上传/查询/下载/删除接口，不依赖具体业务。

2. **实现前端 API + 通用组件**
   - 完成 `api/document.ts` + 三个 Vue 组件。
   - 在一个测试页面（例如 `/documents/demo`）验证完整上传/预览链路。

3. **业务集成（按场景逐步接入）**
   - 客户联系人详情页 → 加入文档模块（名片/身份证）。
   - 销售订单详情页 → 加入文档模块（合同/报价单）。
   - 入库单详情页 → 加入文档模块（质检照片）。

4. **抽离为真正独立模块（可被其他项目引用）**
   - 把相关代码迁移到 `CRM.Document` 独立 `.csproj` 和（可选）前端 npm 包或本地共享包；当前项目只通过 NuGet/本地引用使用：
     - `services.AddDocumentModule(Configuration.GetSection("DocumentModule"));`
     - Vue 端通过引用共享组件/库来使用。

---

如果你同意这个“独立模块”方案，我可以下一步从**后端 Document 模块骨架**开始：创建 `UploadDocument` 实体、`IDocumentService`/`IFileStorageService`、`DocumentModuleOptions` 和 `AddDocumentModule` 扩展，然后再逐步加 API 和前端组件，每完成一段继续给你进度汇报。




### 开发预估时间

### 独立模块方案开发时间预估（以你当前项目为基准）

#### 一、后端模块

- **1. 文档元数据表 + 实体 + DbContext 集成**  
  - 设计字段、实体 `UploadDocument`、`DbSet`、`OnModelCreating`、迁移脚本  
  - **预估：1.0–1.5 小时**

- **2. 流水号/文件命名与路径规则实现**  
  - 复用/扩展 `SysSerialNumber` 或封装 `INumberGenerator`，生成 `yyMMdd+6位序号`，组合 `BizId + 原始文件名`  
  - **预估：0.5–1.0 小时**

- **3. 文件存储服务模块（IFileStorageService + 实现）**  
  - 读取配置、落盘、目录创建、白名单校验、大小校验、简单文件头校验、防伪装 EXE、图片缩略图生成  
  - **预估：2.0–3.0 小时**

- **4. 文档业务服务模块（IDocumentService + 实现）**  
  - Upload（批量）、按业务查询、管理查询、软删除、单个获取  
  - 与存储服务 + Repository + UnitOfWork 串联  
  - **预估：1.5–2.0 小时**

- **5. 模块注册与 API 暴露（AddDocumentModule + Controllers/Endpoints）**  
  - 扩展方法、依赖注入、`DocumentsController`（上传/列表/预览/下载/删除/管理查询），按现有 `ApiResponse` 风格  
  - **预估：1.0–1.5 小时**

#### 二、前端模块

- **6. 前端文档 API 封装（`document.ts`）**  
  - 对接上传、列表、删除、预览/下载 URL  
  - **预估：0.5–1.0 小时**

- **7. 通用组件：上传面板 + 列表面板 + 预览弹窗**  
  - 拖拽/选择、进度、错误提示、列表视图/缩略图视图、图片/PDF 预览、删除/下载交互  
  - **预估：2.5–3.5 小时**

#### 三、业务集成与测试

- **8. 业务页面集成（首批 3 个页面）**  
  - 客户联系人详情、销售订单详情、入库单详情各嵌入文档面板，调整布局、联调  
  - **预估：2.0–3.0 小时**

- **9. 联调测试 & 优化**  
  - 格式/大小限制、异常场景、并发小压测、权限 & 安全点验证，必要的 bugfix  
  - **预估：2.0–3.0 小时**

---

### 汇总（可并行/重叠部分）

- **后端模块合计**：约 6–9 小时  
- **前端模块合计**：约 3–4.5 小时  
- **业务集成 + 测试**：约 4–6 小时  

考虑部分并行（前后端并行、部分复用现有基础）与你已有基础代码，**整体实现“独立模块 + 首批业务集成”的工作量大致在 10–14 小时量级**。





Summary of what was implemented:

---

## 实施总结（按 IMP 完成）

### 一、后端模块（按目录模拟独立模块）

1. **实体与 DbContext**
   - **`CRM.Core/Models/Document/UploadDocument.cs`**：元数据表，含 Id、BizType、BizId、OriginalFileName、StoredFileName、RelativePath、FileSize、FileExtension、MimeType、ThumbnailRelativePath、Remark、IsDeleted、DeleteTime、DeleteUserId、UploadUserId、CreateTime 等。
   - **`CRM.Core/Models/Document/DocumentDailySequence.cs`**：按日流水号表（TheDate, CurrentSequence），用于生成 `yyMMdd + 6 位序号` 的文件名。
   - **`ApplicationDbContext`**：已注册 `UploadDocuments`、`DocumentDailySequences`，并在 `OnModelCreating` 中配置主键、长度、索引 `(BizType, BizId)`、`CreateTime`。

2. **配置与接口**
   - **`CRM.Core/Document/DocumentModuleOptions.cs`**：RootPath、MaxFileSizeMb、MaxFilesPerUpload、AllowedExtensions、Thumbnail（Enable/MaxWidth/MaxHeight/Quality）。
   - **`CRM.Core/Document/IFileStorageService.cs`**：SaveAsync、OpenReadAsync、ExistsAsync、DeleteAsync。
   - **`CRM.Core/Document/IDocumentService.cs`**：UploadAsync、GetByBizAsync、SearchAsync、SoftDeleteAsync、GetByIdAsync；DTO：DocumentUploadRequest、DocumentUploadFile、DocumentSearchRequest、PagedResult、DocumentSaveContext、StoredFileResult。

3. **实现**
   - **`CRM.Infrastructure/Document/FileNameGenerator.cs`**：按日原子自增序号，生成 `yyMMddNNNNNN_bizId_原始文件名`，并对 bizId/文件名做非法字符过滤。
   - **`CRM.Infrastructure/Document/FileStorageService.cs`**：扩展名白名单、大小校验、简单文件头（magic bytes）校验；路径 `RootPath/UploadFile/{BizType}/{BizId}/{StoredFileName}`；图片缩略图（ImageSharp）生成 `_thumb.jpg`。
   - **`CRM.Infrastructure/Document/DocumentService.cs`**：批量上传、按业务查询、管理分页、软删除、按 ID 获取。
   - **`CRM.Infrastructure/Document/DocumentModuleExtensions.cs`**：`AddDocumentModule(IConfiguration)` 注册配置、FileNameGenerator、IFileStorageService、IDocumentService。

4. **API**
   - **`CRM.API/Controllers/DocumentsController.cs`**：
     - `POST /api/v1/documents/upload`（multipart：bizType, bizId, remark, uploadUserId, files）
     - `GET /api/v1/documents?bizType=&bizId=`
     - `GET /api/v1/documents/admin?bizType=&bizId=&uploadUserId=&remarkKeyword=&startDate=&endDate=&pageNumber=&pageSize=`
     - `GET /api/v1/documents/{id}/download`
     - `GET /api/v1/documents/{id}/preview`
     - `DELETE /api/v1/documents/{id}?userId=`

5. **配置与迁移**
   - **`appsettings.json`**：已增加 `DocumentModule` 节点（RootPath、MaxFileSizeMb、MaxFilesPerUpload、AllowedExtensions、Thumbnail）。
   - **`CRM.Infrastructure/Migrations/20260318000000_AddDocumentTables.cs`**：创建 `document_daily_sequence`、`upload_document` 表及索引。需在目标库执行迁移（或 `dotnet ef database update`）。

---

### 二、前端模块

1. **API**
   - **`src/api/document.ts`**：uploadDocuments、getDocuments、searchDocumentsAdmin、deleteDocument、getPreviewPath、getDownloadPath、downloadDocument（认证请求后触发下载）。

2. **通用组件**
   - **`src/components/Document/DocumentUploadPanel.vue`**：拖拽/选择、数量与大小限制、备注、已选文件列表、上传与清空；Props：bizType、bizId、maxFiles、maxSizeMb、remarkAllowed；Emits：uploaded。
   - **`src/components/Document/DocumentListPanel.vue`**：按 bizType/bizId 拉取列表、网格展示、缩略图/图标、预览/下载/删除；Props：bizType、bizId、viewMode；暴露 refresh。
   - **`src/components/Document/DocumentPreviewDialog.vue`**：通过认证请求拉取 blob，图片用 img、PDF 用 iframe、其他提示下载。

3. **集成与演示**
   - **供应商详情**：`VendorDetail.vue` 的「文档」Tab 中已接入 `DocumentUploadPanel` 与 `DocumentListPanel`（bizType="Vendor", bizId=vendorId）。
   - **演示页**：`/documents/demo`（`DocumentDemo.vue`），可选业务类型与业务 ID，用于验证上传/列表/预览/下载。

4. **路由**
   - 新增路由：`/documents/demo` → `DocumentDemo`。如需在侧栏展示，可在 `AppLayout.vue` 中增加「文档演示」菜单项并指向该路由。

---

### 三、使用与后续

- **运行迁移**（未执行过时）：在解决方案目录执行  
  `dotnet ef database update --project CRM.Infrastructure --startup-project CRM.API`  
  以创建 `upload_document`、`document_daily_sequence` 表。
- **存储目录**：确保 `DocumentModule:RootPath`（默认 `Uploads`）在运行目录下存在或程序有创建权限。
- **ImageSharp 安全警告**：当前使用的 SixLabors.ImageSharp 3.1.5 有已知漏洞，建议升级到最新 3.x 或 2.x 安全版本。
- **后续可做**：在客户详情、销售订单、入库单等页面用相同方式嵌入 `DocumentUploadPanel` + `DocumentListPanel`（仅改 bizType/bizId）；需要时再增加 `GET /documents/{id}/thumbnail` 以在列表中显示缩略图。