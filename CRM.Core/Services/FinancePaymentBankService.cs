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

        public async Task<FinancePaymentBankDto> CreateAsync(string bankName, int? sortOrder, CancellationToken cancellationToken = default)
        {
            var name = (bankName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("银行名称不能为空");
            if (name.Length > 200)
                throw new ArgumentException("银行名称过长");

            var all = (await _repo.GetAllAsync()).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            var order = sortOrder ?? (all.Count == 0 ? 0 : all.Max(x => x.SortOrder) + 1);

            var entity = new FinancePaymentBank
            {
                Id = Guid.NewGuid().ToString(),
                BankName = name,
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
            int sortOrder,
            bool isDisabled,
            CancellationToken cancellationToken = default)
        {
            var name = (bankName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("银行名称不能为空");
            if (name.Length > 200)
                throw new ArgumentException("银行名称过长");

            var entity = await _repo.GetByIdAsync(id);
            cancellationToken.ThrowIfCancellationRequested();
            if (entity == null)
                return null;

            entity.BankName = name;
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
                SortOrder = r.SortOrder,
                IsDisabled = r.IsDisabled,
                CreateTimeUtc = r.CreateTime,
                ModifyTimeUtc = r.ModifyTime
            };
    }
}
