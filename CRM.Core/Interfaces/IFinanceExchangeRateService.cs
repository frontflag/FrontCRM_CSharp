namespace CRM.Core.Interfaces
{
    public class FinanceExchangeRateDto
    {
        public decimal UsdToCny { get; set; }
        public decimal UsdToHkd { get; set; }
        public decimal UsdToEur { get; set; }
        public DateTime? ModifyTimeUtc { get; set; }
        public string? ModifyUserId { get; set; }
        public string? ModifyUserName { get; set; }
    }

    public class FinanceExchangeRateChangeLogDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime ChangeTimeUtc { get; set; }
        public string? ChangeUserId { get; set; }
        public string? ChangeUserName { get; set; }
        public decimal UsdToCny { get; set; }
        public decimal UsdToHkd { get; set; }
        public decimal UsdToEur { get; set; }
        public string? ChangeSummary { get; set; }
    }

    public interface IFinanceExchangeRateService
    {
        Task<FinanceExchangeRateDto> GetCurrentAsync(CancellationToken cancellationToken = default);
        Task<FinanceExchangeRateDto> UpdateAsync(
            decimal usdToCny,
            decimal usdToHkd,
            decimal usdToEur,
            string? userId,
            string? userName,
            CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<FinanceExchangeRateChangeLogDto> Items, int TotalCount)> GetChangeLogPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
