using CRM.Core.Models.Quote;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 报价服务接口
    /// </summary>
    public interface IQuoteService
    {
        Task<Quote> CreateAsync(CreateQuoteRequest request);
        Task<Quote?> GetByIdAsync(string id);
        Task<IEnumerable<Quote>> GetAllAsync();
        Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    /// <summary>
    /// 创建报价单请求 DTO
    /// </summary>
    public class CreateQuoteRequest
    {
        public string QuoteCode { get; set; } = string.Empty;
        public string? RFQId { get; set; }
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
        // 供应商信息
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? VendorCode { get; set; }
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
