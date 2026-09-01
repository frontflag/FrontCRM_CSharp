using CRM.Core.Models.Quote;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 报价服务接口
    /// </summary>
    public interface IQuoteService
    {
        Task<Quote> CreateAsync(CreateQuoteRequest request, string? actingUserId = null);
        Task<Quote?> GetByIdAsync(string id);
        Task<IEnumerable<Quote>> GetAllAsync();
        /// <summary>报价主表列表（数据库分页 + 当前页明细与展示字段填充）。</summary>
        Task<PagedResult<Quote>> GetPagedAsync(QuoteQueryRequest request);
        Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request, string? actingUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 删除人）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);

        /// <summary>报价主表及明细字段变更日志（<c>log_change_fldval</c>）。</summary>
        Task<IReadOnlyList<QuoteFieldChangeLogDto>> GetFieldChangeLogsAsync(string quoteId);

        /// <summary>指定需求明细上已软删的报价（含删除人/时间；按行挂「已删报价」）。</summary>
        Task<IReadOnlyList<QuoteDeletedOnRfqItemDto>> GetDeletedQuotesByRfqItemIdsAsync(
            IReadOnlyCollection<string> rfqItemIds);
    }

    /// <summary>需求明细行上曾被删除的报价。</summary>
    public class QuoteDeletedOnRfqItemDto
    {
        public string QuoteId { get; set; } = string.Empty;
        public string? QuoteCode { get; set; }
        public string? RfqItemId { get; set; }
        public int LineNo { get; set; }
        public string? Mpn { get; set; }
        public string? Brand { get; set; }
        /// <summary>生产日期/DC（多行，与报价明细对齐，换行拼接）</summary>
        public string? DateCodeText { get; set; }
        /// <summary>交期（多行，换行拼接）</summary>
        public string? LeadTimeText { get; set; }
        /// <summary>报价数量（多行，换行拼接）</summary>
        public string? QuantityText { get; set; }
        public DateTime? QuoteCreatedAt { get; set; }
        public string? VendorName { get; set; }
        /// <summary>供应商等级展示文案（S/A/B/C，多供应商去重后顿号拼接）</summary>
        public string? VendorLevel { get; set; }
        public string? UnitPriceText { get; set; }
        public string? CurrencyText { get; set; }
        public string? PurchaseUserName { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        public string? DeletedByUserName { get; set; }
    }

    /// <summary>报价字段变更日志行。</summary>
    public class QuoteFieldChangeLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string QuoteId { get; set; } = string.Empty;
        public string? QuoteCode { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldLabel { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTime ChangedAt { get; set; }
        /// <summary>主表为「主表」；明细为 <c>{QuoteCode}#N</c>。</summary>
        public string? ObjectLabel { get; set; }
    }

    /// <summary>
    /// 创建报价单请求 DTO
    /// </summary>
    public class CreateQuoteRequest
    {
        public string QuoteCode { get; set; } = string.Empty;
        public string? RFQId { get; set; }
        /// <summary>需求明细行 ID；创建报价时用于绑定与回写明细状态。禁止仅用 RFQId+Mpn 在后端推断（同单可有多行相同 PN）。</summary>
        public string? RFQItemId { get; set; }
        public string? Mpn { get; set; }
        public string? CustomerId { get; set; }
        public string? SalesUserId { get; set; }
        public string? PurchaseUserId { get; set; }
        public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
        public short Status { get; set; } = 0;
        public string? Remark { get; set; }
        public List<CreateQuoteItemRequest> Items { get; set; } = new();
    }

    /// <summary>
    /// 创建报价明细行请求 DTO
    /// </summary>
    public class CreateQuoteItemRequest
    {
        /// <summary>已有报价明细行 Id；编辑保存时传入以增量更新，省略则新建。</summary>
        public string? Id { get; set; }

        // 供应商信息
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorCode { get; set; }
        /// <summary>供应商等级（1=S 2=A 3=B 4=C）；有值且与主数据不同时，保存报价回写 vendorinfo.Level。</summary>
        public short? VendorLevel { get; set; }
        public string? ContactId { get; set; }
        public string? ContactName { get; set; }

        // 价格类型
        public string? PriceType { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // 物料信息
        public string? Mpn { get; set; }
        public string? Brand { get; set; }
        public string? BrandOrigin { get; set; }

        // 时效信息
        public string? DateCode { get; set; }
        public string? LeadTime { get; set; }

        // 涂标 / 产地
        public short LabelType { get; set; } = 2;
        public short WaferOrigin { get; set; } = 2;
        public short PackageOrigin { get; set; } = 2;
        public bool FreeShipping { get; set; } = false;

        // 价格信息
        public short Currency { get; set; } = 1;
        public decimal Quantity { get; set; } = 0;
        public decimal UnitPrice { get; set; } = 0;
        public decimal? ConvertedPrice { get; set; }

        // 库存 / 订购信息
        public int MinPackageQty { get; set; } = 0;
        public string? MinPackageUnit { get; set; }
        public int StockQty { get; set; } = 0;
        public int Moq { get; set; } = 0;

        public string? Remark { get; set; }
        public short Status { get; set; } = 0;
    }

    /// <summary>
    /// 更新报价单请求 DTO
    /// </summary>
    public class UpdateQuoteRequest
    {
        public string? Mpn { get; set; }
        public string? CustomerId { get; set; }
        public string? SalesUserId { get; set; }
        public string? PurchaseUserId { get; set; }
        public DateTime? QuoteDate { get; set; }
        public short? Status { get; set; }
        public string? Remark { get; set; }
        public List<CreateQuoteItemRequest>? Items { get; set; }
    }
}
