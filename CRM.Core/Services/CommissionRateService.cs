using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class CommissionRateService : ICommissionRateService
{
    public const string SeedSalesVersionId = "c2000000-0000-4000-8000-000000000001";
    public const string SeedPurchaseVersionId = "c2000000-0000-4000-8000-000000000002";

    private readonly IRepository<CommissionRate> _rateRepo;
    private readonly IRepository<CommissionRateVersion> _versionRepo;
    private readonly IRepository<CommissionCalcSetting> _settingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CommissionRateService(
        IRepository<CommissionRate> rateRepo,
        IRepository<CommissionRateVersion> versionRepo,
        IRepository<CommissionCalcSetting> settingRepo,
        IUnitOfWork unitOfWork)
    {
        _rateRepo = rateRepo;
        _versionRepo = versionRepo;
        _settingRepo = settingRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommissionRateSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureVersionsAsync((short)CommissionRoleType.Sales, cancellationToken);
        await EnsureVersionsAsync((short)CommissionRoleType.Purchase, cancellationToken);
        var setting = await EnsureSettingAsync(cancellationToken);
        var sales = await ListVersionsAsync((short)CommissionRoleType.Sales, cancellationToken);
        var purchase = await ListVersionsAsync((short)CommissionRoleType.Purchase, cancellationToken);
        return new CommissionRateSettingsDto
        {
            SalesVersions = sales.ToList(),
            PurchaseVersions = purchase.ToList(),
            SalesActiveVersionId = sales.FirstOrDefault(v => v.IsActive)?.Id,
            PurchaseActiveVersionId = purchase.FirstOrDefault(v => v.IsActive)?.Id,
            ReceiptWriteoffDelayDays = setting.ReceiptWriteoffDelayDays
        };
    }

    public async Task<IReadOnlyList<CommissionRateVersionDto>> ListVersionsAsync(
        short roleType,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionLadder.IsRoleType(roleType))
            throw new ArgumentOutOfRangeException(nameof(roleType), "类型须为业务员或采购员");

        await EnsureVersionsAsync(roleType, cancellationToken);
        var rows = (await _versionRepo.FindAsync(x => x.RoleType == roleType))
            .OrderByDescending(x => x.VersionNo)
            .ToList();
        return rows.Select(ToVersionDto).ToList();
    }

    public async Task<CommissionRateVersionDto> CreateVersionAsync(
        short roleType,
        string? remark,
        string? copyFromVersionId,
        string? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionLadder.IsRoleType(roleType))
            throw new ArgumentOutOfRangeException(nameof(roleType), "类型须为业务员或采购员");

        await EnsureVersionsAsync(roleType, cancellationToken);
        var remarkText = NormalizeRemark(remark);
        if (string.IsNullOrEmpty(remarkText))
            throw new InvalidOperationException("版本说明不能为空");

        var existing = (await _versionRepo.FindAsync(x => x.RoleType == roleType)).ToList();
        CommissionRateVersion? source = null;
        if (!string.IsNullOrWhiteSpace(copyFromVersionId))
        {
            source = existing.FirstOrDefault(x => x.Id == copyFromVersionId.Trim());
            if (source == null)
                throw new InvalidOperationException("复制来源版本不存在或不属于该类型");
        }
        else
        {
            source = existing.FirstOrDefault(x => x.Status == CommissionRateVersionStatus.Active)
                     ?? existing.OrderByDescending(x => x.VersionNo).FirstOrDefault();
        }

        if (source == null)
            throw new InvalidOperationException("没有可复制的版本");

        var now = DateTime.UtcNow;
        var nextNo = existing.Count == 0 ? 1 : existing.Max(x => x.VersionNo) + 1;
        var version = new CommissionRateVersion
        {
            Id = Guid.NewGuid().ToString(),
            RoleType = roleType,
            VersionNo = nextNo,
            Remark = remarkText,
            Status = CommissionRateVersionStatus.Draft,
            CreateTime = now,
            ModifyTime = now,
            CreatedBy = TrimToNull(operatorUserId),
            UpdatedBy = TrimToNull(operatorUserId)
        };
        await _versionRepo.AddAsync(version);
        // 须先落版本头：库有 FK_commission_rate_version，未配置关系时同批插入会先写档位行。
        await _unitOfWork.SaveChangesAsync();

        var sourceRates = (await _rateRepo.FindAsync(x => x.VersionId == source.Id)).ToList();
        foreach (var src in sourceRates)
        {
            var row = new CommissionRate
            {
                Id = Guid.NewGuid().ToString(),
                VersionId = version.Id,
                UserLevel = src.UserLevel,
                Remark = src.Remark,
                CreateTime = now,
                ModifyTime = now,
                CreatedBy = TrimToNull(operatorUserId),
                UpdatedBy = TrimToNull(operatorUserId)
            };
            row.SetSlots(src.GetSlots());
            await _rateRepo.AddAsync(row);
        }

        await _unitOfWork.SaveChangesAsync();
        await EnsureRowsAsync(version.Id, cancellationToken);
        return ToVersionDto(version);
    }

    public async Task<CommissionRateVersionDto> UpdateVersionAsync(
        string id,
        string? remark,
        string? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new KeyNotFoundException("版本不存在");

        var row = await _versionRepo.GetByIdAsync(id.Trim());
        if (row == null)
            throw new KeyNotFoundException("版本不存在");

        var remarkText = NormalizeRemark(remark);
        if (string.IsNullOrEmpty(remarkText))
            throw new InvalidOperationException("版本说明不能为空");

        row.Remark = remarkText;
        row.ModifyTime = DateTime.UtcNow;
        row.UpdatedBy = TrimToNull(operatorUserId);
        await _versionRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        _ = cancellationToken;
        return ToVersionDto(row);
    }

    public async Task SetActiveVersionsAsync(
        string salesVersionId,
        string purchaseVersionId,
        int receiptWriteoffDelayDays,
        string? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionCalcSettingCode.IsDelayDays(receiptWriteoffDelayDays))
            throw new InvalidOperationException(
                $"收款核销延期提成天数须为 {CommissionCalcSettingCode.DelayDaysMin}～{CommissionCalcSettingCode.DelayDaysMax} 的整数");

        await EnsureVersionsAsync((short)CommissionRoleType.Sales, cancellationToken);
        await EnsureVersionsAsync((short)CommissionRoleType.Purchase, cancellationToken);
        var setting = await EnsureSettingAsync(cancellationToken);
        await ActivateAsync((short)CommissionRoleType.Sales, salesVersionId, operatorUserId);
        await ActivateAsync((short)CommissionRoleType.Purchase, purchaseVersionId, operatorUserId);
        setting.ReceiptWriteoffDelayDays = receiptWriteoffDelayDays;
        setting.ModifyTime = DateTime.UtcNow;
        setting.UpdatedBy = TrimToNull(operatorUserId);
        await _settingRepo.UpdateAsync(setting);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CommissionRateDto>> ListAsync(
        short roleType,
        string? versionId = null,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionLadder.IsRoleType(roleType))
            throw new ArgumentOutOfRangeException(nameof(roleType), "类型须为业务员或采购员");

        await EnsureVersionsAsync(roleType, cancellationToken);
        var version = await ResolveVersionAsync(roleType, versionId);
        await EnsureRowsAsync(version.Id, cancellationToken);
        var rows = (await _rateRepo.FindAsync(x => x.VersionId == version.Id))
            .OrderBy(x => x.UserLevel)
            .ToList();
        return rows.Select(r => ToDto(r, version.RoleType)).ToList();
    }

    public async Task<CommissionRateDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var row = await _rateRepo.GetByIdAsync(id.Trim());
        _ = cancellationToken;
        if (row == null) return null;
        var version = await _versionRepo.GetByIdAsync(row.VersionId);
        return ToDto(row, version?.RoleType ?? 0);
    }

    public async Task<CommissionRateDto> UpdateAsync(
        string id,
        IReadOnlyList<CommissionLadderWriteDto> ladders,
        string? remark,
        string? operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new KeyNotFoundException("记录不存在");

        var row = await _rateRepo.GetByIdAsync(id.Trim());
        if (row == null)
            throw new KeyNotFoundException("记录不存在");

        var slots = ToSlots(ladders);
        var error = CommissionLadderRules.Validate(slots);
        if (error != null)
            throw new InvalidOperationException(error);

        if (remark != null && remark.Length > 200)
            throw new InvalidOperationException("备注最长 200 字");

        row.SetSlots(slots);
        row.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        row.ModifyTime = DateTime.UtcNow;
        row.UpdatedBy = TrimToNull(operatorUserId);
        await _rateRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        var version = await _versionRepo.GetByIdAsync(row.VersionId);
        _ = cancellationToken;
        return ToDto(row, version?.RoleType ?? 0);
    }

    async Task ActivateAsync(short roleType, string versionId, string? operatorUserId)
    {
        if (string.IsNullOrWhiteSpace(versionId))
            throw new InvalidOperationException(roleType == (short)CommissionRoleType.Sales
                ? "请选择业务提成系数版本"
                : "请选择采购提成系数版本");

        var versions = (await _versionRepo.FindAsync(x => x.RoleType == roleType)).ToList();
        var target = versions.FirstOrDefault(x => x.Id == versionId.Trim());
        if (target == null)
            throw new InvalidOperationException("选用的版本不存在或不属于该类型");
        if (target.Status == CommissionRateVersionStatus.Disabled)
            throw new InvalidOperationException("停用版本不能设为生效");

        var now = DateTime.UtcNow;
        var op = TrimToNull(operatorUserId);
        foreach (var row in versions)
        {
            var next = row.Id == target.Id
                ? CommissionRateVersionStatus.Active
                : row.Status == CommissionRateVersionStatus.Active
                    ? CommissionRateVersionStatus.Draft
                    : row.Status;
            if (row.Status == next) continue;
            row.Status = next;
            row.ModifyTime = now;
            row.UpdatedBy = op;
            await _versionRepo.UpdateAsync(row);
        }
    }

    async Task<CommissionRateVersion> ResolveVersionAsync(short roleType, string? versionId)
    {
        var versions = (await _versionRepo.FindAsync(x => x.RoleType == roleType)).ToList();
        if (versions.Count == 0)
            throw new InvalidOperationException("尚未配置提成系数版本");

        if (!string.IsNullOrWhiteSpace(versionId))
        {
            var hit = versions.FirstOrDefault(x => x.Id == versionId.Trim());
            if (hit == null)
                throw new InvalidOperationException("版本不存在或不属于该类型");
            return hit;
        }

        return versions.FirstOrDefault(x => x.Status == CommissionRateVersionStatus.Active)
               ?? versions.OrderByDescending(x => x.VersionNo).First();
    }

    async Task EnsureVersionsAsync(short roleType, CancellationToken cancellationToken)
    {
        var existing = (await _versionRepo.FindAsync(x => x.RoleType == roleType)).ToList();
        if (existing.Count > 0)
        {
            if (!existing.Any(x => x.Status == CommissionRateVersionStatus.Active))
            {
                var first = existing.OrderBy(x => x.VersionNo).First();
                first.Status = CommissionRateVersionStatus.Active;
                first.ModifyTime = DateTime.UtcNow;
                await _versionRepo.UpdateAsync(first);
                await _unitOfWork.SaveChangesAsync();
            }

            await EnsureRowsAsync(
                existing.First(x => x.Status == CommissionRateVersionStatus.Active).Id,
                cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        var seedId = roleType == (short)CommissionRoleType.Sales ? SeedSalesVersionId : SeedPurchaseVersionId;
        var version = new CommissionRateVersion
        {
            Id = seedId,
            RoleType = roleType,
            VersionNo = 1,
            Remark = "初始版本",
            Status = CommissionRateVersionStatus.Active,
            CreateTime = now,
            ModifyTime = now
        };
        await _versionRepo.AddAsync(version);
        await _unitOfWork.SaveChangesAsync();
        await EnsureRowsAsync(version.Id, cancellationToken);
    }

    async Task<CommissionCalcSetting> EnsureSettingAsync(CancellationToken cancellationToken)
    {
        var existing = (await _settingRepo.GetAllAsync()).OrderBy(x => x.CreateTime).FirstOrDefault();
        if (existing != null)
        {
            _ = cancellationToken;
            return existing;
        }

        var now = DateTime.UtcNow;
        var row = new CommissionCalcSetting
        {
            Id = CommissionCalcSettingCode.SeedId,
            ReceiptWriteoffDelayDays = 0,
            CreateTime = now,
            ModifyTime = now
        };
        await _settingRepo.AddAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    async Task EnsureRowsAsync(string versionId, CancellationToken cancellationToken)
    {
        var existing = (await _rateRepo.FindAsync(x => x.VersionId == versionId)).ToList();
        var have = existing.Select(x => x.UserLevel).ToHashSet();
        var now = DateTime.UtcNow;
        var added = false;
        for (short level = UserLevelCode.Min; level <= UserLevelCode.Max; level++)
        {
            if (have.Contains(level)) continue;
            await _rateRepo.AddAsync(new CommissionRate
            {
                Id = Guid.NewGuid().ToString(),
                VersionId = versionId,
                UserLevel = level,
                CreateTime = now,
                ModifyTime = now
            });
            added = true;
        }

        if (added)
            await _unitOfWork.SaveChangesAsync();
        _ = cancellationToken;
    }

    static CommissionLadderSlot[] ToSlots(IReadOnlyList<CommissionLadderWriteDto>? ladders)
    {
        var slots = new CommissionLadderSlot[CommissionLadder.Count];
        for (var i = 0; i < CommissionLadder.Count; i++)
        {
            if (ladders != null && i < ladders.Count)
                slots[i] = new CommissionLadderSlot(ladders[i].ThresholdAmount, ladders[i].RatePoints);
        }

        return CommissionLadderRules.Normalize(slots);
    }

    static CommissionRateDto ToDto(CommissionRate row, short roleType)
    {
        var slots = row.GetSlots();
        return new CommissionRateDto
        {
            Id = row.Id,
            VersionId = row.VersionId,
            RoleType = roleType,
            UserLevel = row.UserLevel,
            Thresholds = slots.Select(s => s.Threshold).ToArray(),
            RatePoints = slots.Select(s => s.RatePoints).ToArray(),
            Remark = row.Remark
        };
    }

    static CommissionRateVersionDto ToVersionDto(CommissionRateVersion row) =>
        new()
        {
            Id = row.Id,
            RoleType = row.RoleType,
            VersionNo = row.VersionNo,
            Remark = row.Remark,
            Status = row.Status,
            IsActive = row.IsActive
        };

    static string? NormalizeRemark(string? remark)
    {
        if (string.IsNullOrWhiteSpace(remark)) return null;
        var text = remark.Trim();
        if (text.Length > 200)
            throw new InvalidOperationException("版本说明最长 200 字");
        return text;
    }

    static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
