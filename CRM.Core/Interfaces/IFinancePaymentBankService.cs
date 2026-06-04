namespace CRM.Core.Interfaces
{
    public class FinancePaymentBankDto
    {
        public string Id { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? EBankName { get; set; }
        public int CurrencyType { get; set; }
        public int SortOrder { get; set; }
        public bool IsDisabled { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public DateTime? ModifyTimeUtc { get; set; }
    }

    public interface IFinancePaymentBankService
    {
        /// <summary>业务下拉用：仅未禁用的银行，按排序与名称。</summary>
        Task<IReadOnlyList<FinancePaymentBankDto>> ListEnabledAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FinancePaymentBankDto>> ListAsync(CancellationToken cancellationToken = default);
        Task<FinancePaymentBankDto> CreateAsync(
            string bankName,
            string? shortName,
            string? eBankName,
            int currencyType,
            int? sortOrder,
            CancellationToken cancellationToken = default);
        Task<FinancePaymentBankDto?> UpdateAsync(
            string id,
            string bankName,
            string? shortName,
            string? eBankName,
            int currencyType,
            int sortOrder,
            bool isDisabled,
            CancellationToken cancellationToken = default);
    }
}
