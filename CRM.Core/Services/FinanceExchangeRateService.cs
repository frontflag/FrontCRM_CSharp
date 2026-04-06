using System.Globalization;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinanceExchangeRateService : IFinanceExchangeRateService
    {
        private readonly IRepository<FinanceExchangeRateSetting> _settingRepo;
        private readonly IRepository<FinanceExchangeRateChangeLog> _logRepo;
        private readonly IUnitOfWork _unitOfWork;

        public FinanceExchangeRateService(
            IRepository<FinanceExchangeRateSetting> settingRepo,
            IRepository<FinanceExchangeRateChangeLog> logRepo,
            IUnitOfWork unitOfWork)
        {
            _settingRepo = settingRepo;
            _logRepo = logRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinanceExchangeRateDto> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            var row = await _settingRepo.GetByIdAsync(FinanceExchangeRateSetting.SingletonId);
            if (row == null)
            {
                return new FinanceExchangeRateDto
                {
                    UsdToCny = 6.9194m,
                    UsdToHkd = 7.8367m,
                    UsdToEur = 0.8725m
                };
            }

            return new FinanceExchangeRateDto
            {
                UsdToCny = row.UsdToCny,
                UsdToHkd = row.UsdToHkd,
                UsdToEur = row.UsdToEur,
                ModifyTimeUtc = row.ModifyTime ?? row.CreateTime,
                ModifyUserId = row.EditorUserId,
                ModifyUserName = row.EditorUserName
            };
        }

        public async Task<FinanceExchangeRateDto> UpdateAsync(
            decimal usdToCny,
            decimal usdToHkd,
            decimal usdToEur,
            string? userId,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            if (usdToCny <= 0 || usdToHkd <= 0 || usdToEur <= 0)
                throw new ArgumentException("汇率必须大于 0");

            var row = await _settingRepo.GetByIdAsync(FinanceExchangeRateSetting.SingletonId);
            if (row == null)
            {
                row = new FinanceExchangeRateSetting
                {
                    Id = FinanceExchangeRateSetting.SingletonId,
                    UsdToCny = usdToCny,
                    UsdToHkd = usdToHkd,
                    UsdToEur = usdToEur,
                    EditorUserId = userId,
                    EditorUserName = userName
                };
                await _settingRepo.AddAsync(row);
            }
            else
            {
                row.UsdToCny = usdToCny;
                row.UsdToHkd = usdToHkd;
                row.UsdToEur = usdToEur;
                row.EditorUserId = userId;
                row.EditorUserName = userName;
                await _settingRepo.UpdateAsync(row);
            }

            var summary = BuildSummary(usdToCny, usdToHkd, usdToEur);
            var log = new FinanceExchangeRateChangeLog
            {
                Id = Guid.NewGuid().ToString(),
                UsdToCny = usdToCny,
                UsdToHkd = usdToHkd,
                UsdToEur = usdToEur,
                ChangeUserId = userId,
                ChangeUserName = userName,
                ChangeSummary = summary
            };
            await _logRepo.AddAsync(log);

            await _unitOfWork.SaveChangesAsync();

            return await GetCurrentAsync(cancellationToken);
        }

        public async Task<(IReadOnlyList<FinanceExchangeRateChangeLogDto> Items, int TotalCount)> GetChangeLogPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var all = (await _logRepo.GetAllAsync()).ToList();
            var total = all.Count;
            var items = all
                .OrderByDescending(x => x.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FinanceExchangeRateChangeLogDto
                {
                    Id = x.Id,
                    ChangeTimeUtc = PostgreSqlDateTime.ToUtc(x.CreateTime),
                    ChangeUserId = x.ChangeUserId,
                    ChangeUserName = x.ChangeUserName,
                    UsdToCny = x.UsdToCny,
                    UsdToHkd = x.UsdToHkd,
                    UsdToEur = x.UsdToEur,
                    ChangeSummary = x.ChangeSummary
                })
                .ToList();

            return (items, total);
        }

        private static string BuildSummary(decimal cny, decimal hkd, decimal eur)
        {
            var inv = CultureInfo.InvariantCulture;
            return string.Format(inv,
                "人民币={0}; 港币={1}; 欧元={2}",
                cny.ToString("0.####", inv),
                hkd.ToString("0.####", inv),
                eur.ToString("0.####", inv));
        }
    }
}
