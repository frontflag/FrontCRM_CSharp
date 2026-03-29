using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces
{
    /// <summary>需求(RFQ)服务接口</summary>
    public interface IRFQService
    {
        Task<RFQ> CreateAsync(CreateRFQRequest request);
        Task<RFQ?> GetByIdAsync(string id);
        Task<PagedResult<RFQListItem>> GetPagedAsync(RFQQueryRequest request);
        /// <summary>需求明细分页（联主表、客户、业务员，含数据权限）</summary>
        Task<PagedResult<RFQItemListItem>> GetPagedItemsAsync(RFQItemQueryRequest request);
        Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateRFQRequest
    {
        public string? CustomerId { get; set; }
        public string? ContactId { get; set; }
        public string? ContactEmail { get; set; }
        public string? SalesUserId { get; set; }
        public short RfqType { get; set; } = 1;
        public short QuoteMethod { get; set; } = 1;
        public short AssignMethod { get; set; } = 2;
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
        public int LineNo { get; set; } = 1;
        public string? CustomerMpn { get; set; }
        public string Mpn { get; set; } = string.Empty;
        public string CustomerBrand { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
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
    }

    public class RFQListItem
    {
        public string Id { get; set; } = string.Empty;
        public string RfqCode { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public short Status { get; set; }
        public short RfqType { get; set; }
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public short Importance { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>需求明细列表查询条件（对应 GET /rfqs/items）</summary>
    public class RFQItemQueryRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        /// <summary>按主表创建时间（需求创建）筛选，含当日</summary>
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CustomerKeyword { get; set; }
        public string? MaterialModel { get; set; }
        /// <summary>按主表业务员用户 ID 精确筛选（与前端下拉一致）</summary>
        public string? SalesUserId { get; set; }
        public string? SalesUserKeyword { get; set; }
        /// <summary>为 true 时仅返回至少存在一条关联报价单（quote.rfq_item_id）的需求明细</summary>
        public bool? HasQuotesOnly { get; set; }
        public string? CurrentUserId { get; set; }
    }

    /// <summary>需求明细列表行（主表扩展字段供前端展示）</summary>
    public class RFQItemListItem
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public string? RfqCode { get; set; }
        /// <summary>主表创建时间（需求创建）</summary>
        public DateTime RfqCreateTime { get; set; }
        public int LineNo { get; set; }
        public string Mpn { get; set; } = string.Empty;
        public string? CustomerMpn { get; set; }
        public string Brand { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public short Status { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }

        /// <summary>轮询分配的询价采购员1</summary>
        public string? AssignedPurchaserUserId1 { get; set; }
        public string? AssignedPurchaserUserId2 { get; set; }
        public string? AssignedPurchaserName1 { get; set; }
        public string? AssignedPurchaserName2 { get; set; }
    }
}
