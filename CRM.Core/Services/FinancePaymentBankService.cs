using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    public class FinancePaymentBankService : IFinancePaymentBankService
    {
        private readonly IRepository<FinancePaymentBank> _repo;
        private readonly IUnitOfWork _unitOfWork;

        public FinancePaymentBankService(IRepository<FinancePaymentBank> repo, IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<FinancePaymentBankDto>> ListEnabledAsync(CancellationToken cancellationToken = default)
        {
            var rows = (await _repo.GetAllAsync()).Where(r => !r.IsDisabled).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            return rows
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.BankName, StringComparer.Ordinal)
                .Select(Map)
                .ToList();
        }

        public async Task<IReadOnlyList<FinancePaymentBankDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            var rows = (await _repo.GetAllAsync()).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            return rows
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.BankName, StringComparer.Ordinal)
                .Select(Map)
                .ToList();
        }

        public async Task<FinancePaymentBankDto> CreateAsync(
            string bankName,
            string? shortName,
            string? eBankName,
            int currencyType,
            int? sortOrder,
            CancellationToken cancellationToken = default)
        {
            var name = (bankName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("银行名称不能为空");
            if (name.Length > 200)
                throw new ArgumentException("银行名称过长");
            ValidateCurrencyType(currencyType);

            var all = (await _repo.GetAllAsync()).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            var order = sortOrder ?? (all.Count == 0 ? 0 : all.Max(x => x.SortOrder) + 1);

            var entity = new FinancePaymentBank
            {
                Id = Guid.NewGuid().ToString(),
                BankName = name,
                ShortName = NormalizeShortName(shortName),
                EBankName = NormalizeEBankName(eBankName),
                CurrencyType = currencyType,
                SortOrder = order,
                IsDisabled = false
            };
            await _repo.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<FinancePaymentBankDto?> UpdateAsync(
            string id,
            string bankName,
            string? shortName,
            string? eBankName,
            int currencyType,
            int sortOrder,
            bool isDisabled,
            CancellationToken cancellationToken = default)
        {
            var name = (bankName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("银行名称不能为空");
            if (name.Length > 200)
                throw new ArgumentException("银行名称过长");
            ValidateCurrencyType(currencyType);

            var entity = await _repo.GetByIdAsync(id);
            cancellationToken.ThrowIfCancellationRequested();
            if (entity == null)
                return null;

            entity.BankName = name;
            entity.ShortName = NormalizeShortName(shortName);
            entity.EBankName = NormalizeEBankName(eBankName);
            entity.CurrencyType = currencyType;
            entity.SortOrder = sortOrder;
            entity.IsDisabled = isDisabled;
            await _repo.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return Map(entity);
        }

        private static FinancePaymentBankDto Map(FinancePaymentBank r) =>
            new()
            {
                Id = r.Id,
                BankName = r.BankName,
                ShortName = r.ShortName,
                EBankName = r.EBankName,
                CurrencyType = r.CurrencyType,
                SortOrder = r.SortOrder,
                IsDisabled = r.IsDisabled,
                CreateTimeUtc = r.CreateTime,
                ModifyTimeUtc = r.ModifyTime
            };

        private static void ValidateCurrencyType(int currencyType)
        {
            if (!FinancePaymentBankCurrencyType.IsValid(currencyType))
                throw new ArgumentException("币别类型无效，仅支持 10（人民币银行）或 20（外币银行）");
        }

        private static string? NormalizeShortName(string? shortName)
        {
            var s = (shortName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s)) return null;
            if (s.Length > 100) throw new ArgumentException("银行简称过长");
            return s;
        }

        private static string? NormalizeEBankName(string? eBankName)
        {
            var s = (eBankName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s)) return null;
            if (s.Length > 200) throw new ArgumentException("银行英文名称过长");
            return s;
        }
    }
}
