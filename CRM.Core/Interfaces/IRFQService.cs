using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 需求(RFQ)服务接口
    /// </summary>
    public interface IRFQService
    {
        Task<RFQ> CreateAsync(CreateRFQRequest request);
        Task<RFQ?> GetByIdAsync(string id);
        Task<IEnumerable<RFQ>> GetAllAsync();
        Task<IEnumerable<RFQ>> GetByCustomerIdAsync(string customerId);
        Task<RFQ> UpdateAsync(string id, UpdateRFQRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateRFQRequest
    {
        public string RFQCode { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string SalesUserId { get; set; } = string.Empty;
        public DateTime RFQDate { get; set; }
        public List<CreateRFQItemRequest> Items { get; set; } = new();
    }

    public class CreateRFQItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal? TargetPrice { get; set; }
        public DateTime? RequestDate { get; set; }
    }

    public class UpdateRFQRequest
    {
        public string? Remark { get; set; }
    }
}
