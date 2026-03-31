namespace CRM.API.Models.DTOs
{
    /// <summary>
    /// 销售订单旅程（交易路线图）聚合返回 DTO
    /// </summary>
    public class SalesOrderJourneyResponseDto
    {
        public IReadOnlyList<SalesOrderJourneyNodeDto> Nodes { get; set; } = Array.Empty<SalesOrderJourneyNodeDto>();
        public IReadOnlyList<SalesOrderJourneyEdgeDto> Edges { get; set; } = Array.Empty<SalesOrderJourneyEdgeDto>();
    }

    public class SalesOrderJourneyNodeDto
    {
        /// <summary>全局唯一节点ID（建议带业务前缀）</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>业务类型：RFQ/QUOTE/SALES_ORDER/PR/PO/...（用于前端映射样式）</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>节点标题（展示用，如“销售订单 SOxxxx”）</summary>
        public string Title { get; set; } = string.Empty;

        public string? StatusText { get; set; }
        public string? CreateDate { get; set; }
        public string? CreatorName { get; set; }

        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }

        /// <summary>当前节点（“您在此”）</summary>
        public bool IsCurrent { get; set; }
    }

    public class SalesOrderJourneyEdgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
    }
}

