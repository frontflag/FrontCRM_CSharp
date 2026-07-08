using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class PurchaseCostParamService : IPurchaseCostParamService
{
    public const string DefaultSeedId = "00000000-0000-4000-8000-0000000000f1";

    private readonly IRepository<PurchaseCostParam> _paramRepo;
    private readonly IRepository<PurchaseCostParamChangeLog> _logRepo;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseCostParamService(
        IRepository<PurchaseCostParam> paramRepo,
        IRepository<PurchaseCostParamChangeLog> logRepo,
        IUnitOfWork unitOfWork)
    {
        _paramRepo = paramRepo;
        _logRepo = logRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseCostParamDto> GetEffectiveAsync(DateTime? asOfUtc = null, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var asOf = asOfUtc ?? DateTime.UtcNow;
        var row = await ResolveEffectiveRowAsync(asOf)
                  ?? throw new InvalidOperationException("未配置采购系数！");
        return MapDto(row, asOf);
    }

    public async Task<(IReadOnlyList<PurchaseCostParamDto> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var all = (await _paramRepo.GetAllAsync())
            .OrderByDescending(x => x.StartTime)
            .ThenByDescending(x => x.CreateTime)
            .ToList();
        var total = all.Count;
        var now = DateTime.UtcNow;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapDto(x, now))
            .ToList();
        return (items, total);
    }

    public async Task<PurchaseCostParamDto> CreateAsync(
        decimal ratio,
        DateTime startTimeUtc,
        string? remark,
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (ratio <= 0m)
            throw new ArgumentException("采购系数必须大于 0。", nameof(ratio));

        var start = startTimeUtc.Kind == DateTimeKind.Utc
            ? startTimeUtc
            : DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc);

        var row = new PurchaseCostParam
        {
            Id = Guid.NewGuid().ToString(),
            Ratio = ratio,
            StartTime = start,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            IsDeleted = false,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(userId)
        };
        await _paramRepo.AddAsync(row);

        await _logRepo.AddAsync(new PurchaseCostParamChangeLog
        {
            Id = Guid.NewGuid().ToString(),
            PurchaseCostParamId = row.Id,
            Ratio = row.Ratio,
            StartTime = row.StartTime,
            ChangeUserId = ActingUserIdNormalizer.Normalize(userId),
            ChangeUserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
            ChangeSummary = $"新增采购系数 ratio={row.Ratio:0.####} start={row.StartTime:O}",
            CreateTime = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
        return MapDto(row, DateTime.UtcNow);
    }

    public async Task SoftDeleteAsync(string id, string? userId, string? userName, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var key = id.Trim();
        var row = await _paramRepo.GetByIdAsync(key)
                  ?? throw new InvalidOperationException("采购系数配置不存在。");
        if (row.IsDeleted)
            throw new InvalidOperationException("采购系数配置已删除。");

        row.IsDeleted = true;
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(userId);
        await _paramRepo.UpdateAsync(row);

        await _logRepo.AddAsync(new PurchaseCostParamChangeLog
        {
            Id = Guid.NewGuid().ToString(),
            PurchaseCostParamId = row.Id,
            Ratio = row.Ratio,
            StartTime = row.StartTime,
            ChangeUserId = ActingUserIdNormalizer.Normalize(userId),
            ChangeUserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim(),
            ChangeSummary = $"删除采购系数 ratio={row.Ratio:0.####}",
            CreateTime = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<PurchaseCostParamChangeLogDto> Items, int TotalCount)> GetChangeLogPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

        var all = (await _logRepo.GetAllAsync()).OrderByDescending(x => x.CreateTime).ToList();
        var total = all.Count;
        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseCostParamChangeLogDto
            {
                Id = x.Id,
                PurchaseCostParamId = x.PurchaseCostParamId,
                Ratio = x.Ratio,
                StartTimeUtc = PostgreSqlDateTime.ToUtc(x.StartTime),
                ChangeTimeUtc = PostgreSqlDateTime.ToUtc(x.CreateTime),
                ChangeUserId = x.ChangeUserId,
                ChangeUserName = x.ChangeUserName,
                ChangeSummary = x.ChangeSummary
            })
            .ToList();
        return (items, total);
    }

    private async Task<PurchaseCostParam?> ResolveEffectiveRowAsync(DateTime asOfUtc)
    {
        var all = await _paramRepo.GetAllAsync();
        return all
            .Where(p => p.StartTime <= asOfUtc)
            .OrderByDescending(p => p.StartTime)
            .ThenByDescending(p => p.CreateTime)
            .FirstOrDefault();
    }

    private static PurchaseCostParamDto MapDto(PurchaseCostParam row, DateTime asOfUtc) =>
        new()
        {
            Id = row.Id,
            Ratio = row.Ratio,
            StartTimeUtc = PostgreSqlDateTime.ToUtc(row.StartTime),
            Remark = row.Remark,
            CreateTimeUtc = PostgreSqlDateTime.ToUtc(row.CreateTime),
            CreateByUserId = row.CreateByUserId,
            IsEffectiveNow = row.StartTime <= asOfUtc && !row.IsDeleted
        };
}
