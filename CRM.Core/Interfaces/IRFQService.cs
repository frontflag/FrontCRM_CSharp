using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces
{
    /// <summary>需求(RFQ)服务接口</summary>
    public interface IRFQService
    {
        /// <param name="actingUserId">当前登录用户 ID（写入 create_by_user_id，供列表「创建人」展示）</param>
        Task<RFQ> CreateAsync(CreateRFQRequest request, string? actingUserId = null);
        /// <param name="viewerUserId">当前查看者用户 ID；传入且不具备 customer.info.read 时，响应中脱敏客户相关字段。</param>
        Task<RFQ?> GetByIdAsync(string id, string? viewerUserId = null);
        Task<RFQListPagedResult> GetPagedAsync(RFQQueryRequest request);
        /// <summary>需求明细分页（联主表、客户、业务员，含数据权限）</summary>
        Task<PagedResult<RFQItemListItem>> GetPagedItemsAsync(RFQItemQueryRequest request);
        /// <summary>按明细 ID 获取单条需求明细（含数据权限与客户字段脱敏）。</summary>
        /// <param name="viewerUserId">当前查看者；无权限时抛 <see cref="UnauthorizedAccessException"/>。</param>
        Task<RFQItem?> GetItemByIdAsync(string itemId, string? viewerUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request, string? actingUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 删除人）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);

        /// <summary>手动分配询价采购员。指定 <see cref="AssignPurchaserRequest.RfqItemId"/> 时仅更新该明细；否则更新需求下全部未软删明细。</summary>
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task<RFQ> AssignPurchaserAsync(string rfqId, AssignPurchaserRequest request, string? actingUserId = null);

        /// <summary>标记需求明细为查无报价（status 0→5，不创建报价单、不回写主单）。</summary>
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task<RFQItem> MarkNoQuoteAsync(string itemId, string? actingUserId = null);

        /// <summary>关闭需求：写入关闭记录并将主单置为终态。</summary>
        Task<RfqCloseRecordListItem> CloseRfqAsync(string rfqId, CloseRfqRequest request, string? actingUserId = null);

        /// <summary>需求关闭记录列表（按关闭时间倒序）。</summary>
        Task<IReadOnlyList<RfqCloseRecordListItem>> GetCloseRecordsAsync(string rfqId);

        /// <summary>需求主表及明细字段变更日志（<c>log_change_fldval</c>）。</summary>
        Task<IReadOnlyList<RfqFieldChangeLogDto>> GetFieldChangeLogsAsync(string rfqId);
    }

    /// <summary>需求字段变更日志行。</summary>
    public class RfqFieldChangeLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public string? RfqCode { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldLabel { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTime ChangedAt { get; set; }
        /// <summary>主表为「主表」；明细为 <c>{RfqCode}-L{LineNo}</c>。</summary>
        public string? ObjectLabel { get; set; }
    }

    /// <summary>关闭需求请求</summary>
    public class CloseRfqRequest
    {
        public short CloseType { get; set; }
        public string CloseReason { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }

    /// <summary>需求关闭记录（API 列表项，字段与前端 RFQCloseRecord / 表格列对齐）</summary>
    public class RfqCloseRecordListItem
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public short CloseType { get; set; }
        public string CloseReason { get; set; } = string.Empty;
        /// <summary>与 closeReason 相同，供关闭记录 Tab 列 reason 使用</summary>
        public string Reason { get; set; } = string.Empty;
        public string? ClosedBy { get; set; }
        public string? ClosedByName { get; set; }
        /// <summary>与 closedByName 相同，供关闭记录 Tab 列 operatorName 使用</summary>
        public string? OperatorName { get; set; }
        public DateTime ClosedAt { get; set; }
        /// <summary>与 closedAt 相同，供关闭记录 Tab 列 createdAt 使用</summary>
        public DateTime CreatedAt { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>分配采购员请求（与前端 purchaserId / remark 对齐）</summary>
    public class AssignPurchaserRequest
    {
        public string PurchaserId { get; set; } = string.Empty;
        public string? Remark { get; set; }
        /// <summary>可选：仅更新该需求明细；省略则批量更新全部未软删明细。</summary>
        public string? RfqItemId { get; set; }
    }

    public class CreateRFQRequest
    {
        public string? CustomerId { get; set; }
        public string? ContactId { get; set; }
        public string? ContactEmail { get; set; }
        public string? SalesUserId { get; set; }
        public short RfqType { get; set; } = 1;
        public short QuoteMethod { get; set; } = 1;
        public short AssignMethod { get; set; } = 5;
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public short TargetType { get; set; } = 1;
        public short Importance { get; set; } = 5;
        public bool IsLastInquiry { get; set; } = false;
        public string? ProjectBackground { get; set; }
        public string? Competitor { get; set; }
        public string? Remark { get; set; }
        public List<CreateRFQItemRequest> Items { get; set; } = new();
    }

    public class CreateRFQItemRequest
    {
        /// <summary>已有明细行 Id；编辑保存时传入以增量更新，省略则新建。</summary>
        public string? Id { get; set; }

        public int LineNo { get; set; } = 1;
        public string? CustomerMpn { get; set; }
        public string Mpn { get; set; } = string.Empty;
    public string CustomerBrand { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public long? BrandId { get; set; }
        public decimal? TargetPrice { get; set; }
        public short PriceCurrency { get; set; } = 1;
        public decimal Quantity { get; set; } = 1;
        public string? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? MinPackageQty { get; set; }
        public decimal? Moq { get; set; }
        public string? Alternatives { get; set; }
        public string? Remark { get; set; }
    }

    public class UpdateRFQRequest
    {
        public string? CustomerId { get; set; }
        public string? ContactId { get; set; }
        public string? ContactEmail { get; set; }
        public string? SalesUserId { get; set; }
        public short? RfqType { get; set; }
        public short? QuoteMethod { get; set; }
        public short? AssignMethod { get; set; }
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public short? TargetType { get; set; }
        public short? Importance { get; set; }
        public bool? IsLastInquiry { get; set; }
        public string? ProjectBackground { get; set; }
        public string? Competitor { get; set; }
        public string? Remark { get; set; }
        public List<CreateRFQItemRequest>? Items { get; set; }
    }

    /// <summary>需求主列表 HTTP 分页结果（含全量筛选维度统计卡片数据）。</summary>
    public sealed class RFQListPagedResult : PagedResult<RFQListItem>
    {
        public RfqMainListAggregates? Aggregates { get; set; }
    }

    public class RFQQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Keyword { get; set; }
        public short? Status { get; set; }
        public string? CustomerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CurrentUserId { get; set; }

        /// <summary>按标签 OR 筛选（AND 与其他条件组合）。</summary>
        public List<string>? TagIds { get; set; }

        /// <summary>按业务员登录账号模糊筛选（匹配 user.UserName）。</summary>
        public string? SalesUserName { get; set; }

        /// <summary>按创建人登录账号模糊筛选（匹配 user.UserName → rfq.CreateByUserId）。</summary>
        public string? CreateUserName { get; set; }
    }

    public class RFQListItem
    {
        public string Id { get; set; } = string.Empty;
        public string RfqCode { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public short Status { get; set; }
        public short RfqType { get; set; }
        /// <summary>目标类型：1比价 2独家 3紧急 4常规</summary>
        public short TargetType { get; set; }
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public short Importance { get; set; }
        public int ItemCount { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }

        /// <summary>业务员用户 ID</summary>
        public string? SalesUserId { get; set; }
        /// <summary>业务员登录账号（列表组装，序列化为 salesUserName）</summary>
        public string? SalesUserName { get; set; }
        /// <summary>创建人用户 ID</summary>
        public string? CreateByUserId { get; set; }
        /// <summary>创建人登录账号（列表组装；前端 createUserName）</summary>
        public string? CreateUserName { get; set; }

        /// <summary>标签（仅对有查看权限的用户返回）</summary>
        public List<EntityTagDto>? Tags { get; set; }
    }

    /// <summary>需求明细列表查询条件（对应 GET /rfqs/items）</summary>
    public class RFQItemQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>按主表创建时间（需求创建）筛选，含当日</summary>
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>按明细行创建时间筛选（含起点、不含终点；UTC）</summary>
        public DateTime? ItemCreateStartUtc { get; set; }
        public DateTime? ItemCreateEndExclusiveUtc { get; set; }
        /// <summary>明细下存在报价创建时间落在此窗口（含起点、不含终点；UTC）</summary>
        public DateTime? QuoteCreateStartUtc { get; set; }
        public DateTime? QuoteCreateEndExclusiveUtc { get; set; }
        /// <summary>左栏业务快捷检索码，见 <c>RfqItemListQuickFilterCodes</c></summary>
        public string? QuickFilter { get; set; }
        public string? CustomerKeyword { get; set; }
        public string? MaterialModel { get; set; }
        /// <summary>按主表业务员用户 ID 精确筛选（与前端下拉一致）</summary>
        public string? SalesUserId { get; set; }
        public string? SalesUserKeyword { get; set; }
        /// <summary>按明细分配的询价采购员用户 ID 精确筛选（匹配 AssignedPurchaserUserId1 或 AssignedPurchaserUserId2）</summary>
        public string? PurchaserUserId { get; set; }
        /// <summary>为 true 时仅返回至少存在一条关联报价单（quote.rfq_item_id）的需求明细</summary>
        public bool? HasQuotesOnly { get; set; }
        /// <summary>明细状态（与列表「明细状态」列一致：0 待报价、1 已报价、5 查无报价等；1 含「待报价但有报价记录」）</summary>
        public short? Status { get; set; }
        /// <summary>需求编号（主表 rfq_code，模糊匹配）</summary>
        public string? RfqCode { get; set; }
        public string? CurrentUserId { get; set; }

        /// <summary>
        /// 分析数据集：<c>listFilter</c>（默认，跟列表筛选）或 <c>reportScope</c>（报表范围，排除主单已取消）。
        /// </summary>
        public string? AnalyticsDataset { get; set; }
        /// <summary>报表透镜：company / department / personal（仅 reportScope）。</summary>
        public string? AnalyticsViewLevel { get; set; }
        /// <summary>报表部门透镜主键（仅 department）。</summary>
        public string? AnalyticsDepartmentId { get; set; }

        /// <summary>具备 customer.info.read 等时由服务层置为 true；否则物料型号筛选不包含客户物料号。</summary>
        public bool CanViewCustomerInList { get; set; } = true;

        /// <summary>
        /// 为 true 时仅返回当前用户可报价的明细（系统管理员 / 采购总监 / 分配报价员 / 保护期后采购池），
        /// 与前端 <c>canQuoteRfqItem</c> 对齐。
        /// </summary>
        public bool QuotableByMeOnly { get; set; }

        /// <summary>
        /// 若指定且该明细落在当前筛选结果中，则将页码调整为包含该明细的页（报价桌面深链定位）。
        /// </summary>
        public string? PreferItemId { get; set; }
    }

    /// <summary>需求明细列表行（主表扩展字段供前端展示）</summary>
    public class RFQItemListItem
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public string? RfqCode { get; set; }
        /// <summary>主表创建时间（需求创建）</summary>
        public DateTime RfqCreateTime { get; set; }
        /// <summary>明细行创建时间</summary>
        public DateTime ItemCreateTime { get; set; }
        public int LineNo { get; set; }
        public string Mpn { get; set; } = string.Empty;
        public string? CustomerMpn { get; set; }
        /// <summary>客户指定品牌（脱敏时清空）</summary>
        public string? CustomerBrand { get; set; }
        public string Brand { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        /// <summary>目标价币别（1=RMB 2=USD 3=EUR 4=HKD …，与明细 PriceCurrency 一致）</summary>
        public short PriceCurrency { get; set; } = 1;
        public short Status { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        /// <summary>业务员登录账号（明细分页列表）</summary>
        public string? SalesUserName { get; set; }
        /// <summary>创建人用户 ID（主表 create_by_user_id）</summary>
        public string? CreateByUserId { get; set; }
        /// <summary>创建人登录账号（列表组装；前端 createUserName）</summary>
        public string? CreateUserName { get; set; }

        /// <summary>轮询分配的询价采购员1</summary>
        public string? AssignedPurchaserUserId1 { get; set; }
        public string? AssignedPurchaserUserId2 { get; set; }
        /// <summary>采购员登录账号（列表展示）</summary>
        public string? AssignedPurchaserName1 { get; set; }
        /// <summary>采购员登录账号（列表展示）</summary>
        public string? AssignedPurchaserName2 { get; set; }

        /// <summary>需求明细备注（rfqitem.remark）</summary>
        public string? Remark { get; set; }
    }
}
