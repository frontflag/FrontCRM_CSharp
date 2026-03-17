using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces
{
    /// <summary>需求(RFQ)服务接口</summary>
    public interface IRFQService
    {
        Task<RFQ> CreateAsync(CreateRFQRequest request);
        Task<RFQ?> GetByIdAsync(string id);
        Task<PagedResult<RFQListItem>> GetPagedAsync(RFQQueryRequest request);
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
        public short AssignMethod { get; set; } = 1;
        public string? Industry { get; set; }
        public string? Product { get; set; }
        public short TargetType { get; set; } = 1;
        public short Importance { get; set; } = 5;
        public bool IsLastInquiry { get; set; } = false;
        public string? ProjectBackground { get; set; }
        public string? Competitor { get; set; }
        public string? Remark { get; set; }
        public DateTime RfqDate { get; set; } = DateTime.UtcNow;
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
        public DateTime? RfqDate { get; set; }
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
        public DateTime RfqDate { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
