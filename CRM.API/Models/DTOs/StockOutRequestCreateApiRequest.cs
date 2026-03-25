namespace CRM.API.Models.DTOs
{
    /// <summary>
    /// POST /api/v1/stock-out/request 请求体（由 API 层反序列化，避免与 Core 服务模型混用导致 JSON 绑定异常）
    /// </summary>
    public class StockOutRequestCreateApiRequest
    {
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        /// <summary>销售订单明细主键（sellorderitem）</summary>
        public string SalesOrderItemId { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string RequestUserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Remark { get; set; }
    }
}
