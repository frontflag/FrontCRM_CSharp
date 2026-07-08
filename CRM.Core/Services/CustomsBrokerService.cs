using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.System;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class CustomsBrokerService : ICustomsBrokerService
{
    private readonly IRepository<CustomsBroker> _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly IUserService _userService;

    public CustomsBrokerService(
        IRepository<CustomsBroker> repo,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        ILogOperationAppendService logOperationAppend,
        IUserService userService)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _logOperationAppend = logOperationAppend;
        _userService = userService;
    }

    public async Task<IReadOnlyList<CustomsBroker>> GetActiveListAsync()
    {
        var all = await _repo.GetAllAsync();
        return all.Where(x => x.Status == CustomsBrokerStatusCodes.Active).OrderBy(x => x.BrokerCode).ToList();
    }

    public async Task<IReadOnlyList<CustomsBroker>> GetAllOrderedForAdminAsync()
    {
        var all = await _repo.GetAllAsync();
        return all.OrderBy(x => x.BrokerCode).ThenBy(x => x.Cname).ToList();
    }

    public Task<CustomsBroker?> GetByIdAsync(string id) => _repo.GetByIdAsync(id);

    public async Task<CustomsBroker> CreateAsync(string cname, string? ename, short regionType, decimal agencyRate, string? remark, string? actingUserId)
    {
        if (string.IsNullOrWhiteSpace(cname))
            throw new ArgumentException("公司中文名不能为空", nameof(cname));
        EnsureRegionType(regionType);
        EnsureAgencyRate(agencyRate);

        var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.CustomsBroker);

        var row = new CustomsBroker
        {
            Id = Guid.NewGuid().ToString(),
            BrokerCode = code,
            Cname = cname.Trim(),
            Ename = string.IsNullOrWhiteSpace(ename) ? null : ename.Trim(),
            RegionType = regionType,
            AgencyRate = agencyRate,
            Status = CustomsBrokerStatusCodes.Active,
            IsDeleted = false,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        await _repo.AddAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<CustomsBroker> UpdateAsync(string id, string cname, string? ename, short regionType, decimal agencyRate, string? remark, string? actingUserId)
    {
        var row = await _repo.GetByIdAsync(id);
        if (row == null)
            throw new InvalidOperationException("报关公司不存在");

        if (string.IsNullOrWhiteSpace(cname))
            throw new ArgumentException("公司中文名不能为空", nameof(cname));
        EnsureRegionType(regionType);
        EnsureAgencyRate(agencyRate);

        row.Cname = cname.Trim();
        row.Ename = string.IsNullOrWhiteSpace(ename) ? null : ename.Trim();
        row.RegionType = regionType;
        row.AgencyRate = agencyRate;
        row.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

        await _repo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<CustomsBroker> SetStatusAsync(string id, short status, string? actingUserId)
    {
        if (status != CustomsBrokerStatusCodes.Active && status != CustomsBrokerStatusCodes.Inactive)
            throw new ArgumentException("状态无效，应为 1（启用）或 0（停用）。", nameof(status));

        var row = await _repo.GetByIdAsync(id);
        if (row == null)
            throw new InvalidOperationException("报关公司不存在");

        row.Status = status;
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

        await _repo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task SoftDeleteAsync(string id, string? actingUserId)
    {
        var key = id.Trim();
        var row = (await _repo.FindIgnoreFiltersAsync(x => x.Id == key)).FirstOrDefault()
                  ?? throw new InvalidOperationException("报关公司不存在");
        if (row.IsDeleted)
            throw new InvalidOperationException("报关公司已删除");

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.DeletedByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

        await _repo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();

        var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
        await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
        {
            BizType = BusinessLogTypes.CustomsBroker,
            RecordId = row.Id,
            RecordCode = row.BrokerCode,
            EntityDisplayName = DeleteLogEntityNames.CustomsBroker,
            OperatorUserId = actorId,
            OperatorUserName = actorName
        });
    }

    private static void EnsureAgencyRate(decimal agencyRate)
    {
        if (agencyRate < 1m)
            throw new ArgumentException("代理费率须为 1+纯费率形式，不能小于 1。", nameof(agencyRate));
    }

    private static void EnsureRegionType(short regionType)
    {
        if (regionType != CustomsBrokerServiceRegion.Shenzhen && regionType != CustomsBrokerServiceRegion.HongKong)
            throw new ArgumentException("服务方向无效，应为 10（深圳）或 20（香港）。", nameof(regionType));
    }
}
