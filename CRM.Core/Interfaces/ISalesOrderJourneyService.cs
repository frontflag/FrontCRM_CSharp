namespace CRM.Core.Interfaces
{
    public interface ISalesOrderJourneyService
    {
        Task<SalesOrderJourneyResponseDto> GetJourneyAsync(string sellOrderId, string? currentUserId = null);
    }

    public class SalesOrderJourneyResponseDto
    {
        public IReadOnlyList<SalesOrderJourneyNodeDto> Nodes { get; set; } = Array.Empty<SalesOrderJourneyNodeDto>();
        public IReadOnlyList<SalesOrderJourneyEdgeDto> Edges { get; set; } = Array.Empty<SalesOrderJourneyEdgeDto>();
    }

    public class SalesOrderJourneyNodeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? StatusText { get; set; }
        public string? CreateDate { get; set; }
        public string? CreatorName { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class SalesOrderJourneyEdgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
    }
}

