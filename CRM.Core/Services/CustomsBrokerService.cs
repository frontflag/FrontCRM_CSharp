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

    public async Task<CustomsBroker> CreateAsync(CustomsBrokerWriteFields fields, string? actingUserId)
    {
        var row = new CustomsBroker
        {
            Id = Guid.NewGuid().ToString(),
            BrokerCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.CustomsBroker),
            Status = CustomsBrokerStatusCodes.Active,
            IsDeleted = false,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        ApplyWriteFields(row, fields);
        await _repo.AddAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<CustomsBroker> UpdateAsync(string id, CustomsBrokerWriteFields fields, string? actingUserId)
    {
        var row = await _repo.GetByIdAsync(id);
        if (row == null)
            throw new InvalidOperationException("报关公司不存在");

        ApplyWriteFields(row, fields);
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

    private static void EnsureAgencyRate(decimal agencyRate) =>
        CustomsAgencyRateRules.EnsureValid(agencyRate);

    private static void EnsureRegionType(short regionType)
    {
        if (regionType != CustomsBrokerServiceRegion.Shenzhen && regionType != CustomsBrokerServiceRegion.HongKong)
            throw new ArgumentException("服务方向无效，应为 10（深圳）或 20（香港）。", nameof(regionType));
    }

    private static void ApplyWriteFields(CustomsBroker row, CustomsBrokerWriteFields fields)
    {
        if (string.IsNullOrWhiteSpace(fields.Cname))
            throw new ArgumentException("公司中文名不能为空", nameof(fields.Cname));
        if (string.IsNullOrWhiteSpace(fields.ContactName))
            throw new ArgumentException("请填写装箱单收货人联系人。", nameof(fields.ContactName));
        if (string.IsNullOrWhiteSpace(fields.Tel))
            throw new ArgumentException("请填写装箱单收货人电话。", nameof(fields.Tel));
        if (string.IsNullOrWhiteSpace(fields.Address))
            throw new ArgumentException("请填写装箱单收货人地址。", nameof(fields.Address));
        EnsureRegionType(fields.RegionType);
        EnsureAgencyRate(fields.AgencyRate);

        var email = string.IsNullOrWhiteSpace(fields.Email) ? null : fields.Email.Trim();
        if (email != null && (email.Length > 200 || email.IndexOf('@') < 1 || email.EndsWith('@')))
            throw new ArgumentException("邮箱格式无效。", nameof(fields.Email));

        var contact = fields.ContactName.Trim();
        var tel = fields.Tel.Trim();
        var address = fields.Address.Trim();
        if (contact.Length > 100)
            throw new ArgumentException("联系人最长 100 字。", nameof(fields.ContactName));
        if (tel.Length > 64)
            throw new ArgumentException("电话最长 64 字。", nameof(fields.Tel));
        if (address.Length > 500)
            throw new ArgumentException("地址最长 500 字。", nameof(fields.Address));

        row.Cname = fields.Cname.Trim();
        row.Ename = string.IsNullOrWhiteSpace(fields.Ename) ? null : fields.Ename.Trim();
        row.RegionType = fields.RegionType;
        row.AgencyRate = fields.AgencyRate;
        row.Remark = string.IsNullOrWhiteSpace(fields.Remark) ? null : fields.Remark.Trim();
        row.ContactName = contact;
        row.Tel = tel;
        row.Email = email;
        row.Address = address;
    }
}
