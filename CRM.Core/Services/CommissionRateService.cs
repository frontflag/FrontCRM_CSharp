using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class CommissionRateService : ICommissionRateService
{
    private readonly IRepository<CommissionRate> _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CommissionRateService(IRepository<CommissionRate> repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CommissionRateDto>> ListAsync(
        short roleType,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionLadder.IsRoleType(roleType))
            throw new ArgumentOutOfRangeException(nameof(roleType), "类型须为业务员或采购员");

        await EnsureRowsAsync(roleType, cancellationToken);
        var rows = (await _repo.FindAsync(x => x.RoleType == roleType))
            .OrderBy(x => x.UserLevel)
            .ToList();
        return rows.Select(ToDto).ToList();
    }

    public async Task<CommissionRateDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var row = await _repo.GetByIdAsync(id.Trim());
        _ = cancellationToken;
        return row == null ? null : ToDto(row);
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

        var row = await _repo.GetByIdAsync(id.Trim());
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
        row.UpdatedBy = string.IsNullOrWhiteSpace(operatorUserId) ? null : operatorUserId.Trim();
        await _repo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return ToDto(row);
    }

    async Task EnsureRowsAsync(short roleType, CancellationToken cancellationToken)
    {
        var existing = (await _repo.FindAsync(x => x.RoleType == roleType)).ToList();
        var have = existing.Select(x => x.UserLevel).ToHashSet();
        var now = DateTime.UtcNow;
        var added = false;
        for (short level = UserLevelCode.Min; level <= UserLevelCode.Max; level++)
        {
            if (have.Contains(level)) continue;
            await _repo.AddAsync(new CommissionRate
            {
                Id = Guid.NewGuid().ToString(),
                RoleType = roleType,
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

    static CommissionRateDto ToDto(CommissionRate row)
    {
        var slots = row.GetSlots();
        return new CommissionRateDto
        {
            Id = row.Id,
            RoleType = row.RoleType,
            UserLevel = row.UserLevel,
            Thresholds = slots.Select(s => s.Threshold).ToArray(),
            RatePoints = slots.Select(s => s.RatePoints).ToArray(),
            Remark = row.Remark
        };
    }
}
