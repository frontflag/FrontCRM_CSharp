using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.System;

namespace CRM.Core.Services;

public sealed class UserLevelService : IUserLevelService
{
    private readonly IUserService _users;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<UserLevelHistory> _historyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UserLevelService(
        IUserService users,
        IRepository<User> userRepo,
        IRepository<UserLevelHistory> historyRepo,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _userRepo = userRepo;
        _historyRepo = historyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserLevelChangeResult> ChangeAsync(
        string targetUserId,
        short newLevel,
        string? remark,
        string operatorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            throw new ArgumentException("用户 Id 不能为空", nameof(targetUserId));
        if (!UserLevelCode.IsValid(newLevel))
            throw new ArgumentOutOfRangeException(nameof(newLevel), $"等级须为 {UserLevelCode.Min}～{UserLevelCode.Max}");

        var user = await _users.GetByIdForAdminAsync(targetUserId.Trim());
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        var trimmedRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        if (trimmedRemark != null && trimmedRemark.Length > 200)
            trimmedRemark = trimmedRemark[..200];

        var current = user.Level < UserLevelCode.Min ? UserLevelCode.Default : user.Level;
        user.LevelRemark = trimmedRemark;

        if (current == newLevel)
        {
            user.ModifyTime = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return new UserLevelChangeResult
            {
                UserId = user.Id,
                Level = current,
                LevelChangedAt = user.LevelChangedAt,
                LevelRemark = user.LevelRemark,
                LevelChanged = false
            };
        }

        var now = DateTime.UtcNow;
        var operatorName = await ResolveOperatorNameAsync(operatorUserId);

        await _historyRepo.AddAsync(new UserLevelHistory
        {
            UserId = user.Id,
            UserName = user.UserName,
            OldLevel = current,
            NewLevel = newLevel,
            Remark = trimmedRemark,
            ChangeTime = now,
            OperatorUserId = string.IsNullOrWhiteSpace(operatorUserId) ? null : operatorUserId.Trim(),
            OperatorUserName = operatorName
        });

        user.Level = newLevel;
        user.LevelChangedAt = now;
        user.ModifyTime = now;
        await _userRepo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserLevelChangeResult
        {
            UserId = user.Id,
            Level = newLevel,
            LevelChangedAt = now,
            LevelRemark = user.LevelRemark,
            LevelChanged = true
        };
    }

    public async Task<IReadOnlyList<UserLevelHistory>> GetHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<UserLevelHistory>();

        var rows = await _historyRepo.FindAsNoTrackingAsync(x => x.UserId == userId.Trim());
        return rows.OrderByDescending(x => x.ChangeTime).ThenByDescending(x => x.CreateTime).ToList();
    }

    private async Task<string?> ResolveOperatorNameAsync(string operatorUserId)
    {
        if (string.IsNullOrWhiteSpace(operatorUserId)) return null;
        var op = await _users.GetByIdForAdminAsync(operatorUserId.Trim());
        if (op == null) return operatorUserId.Trim();
        if (!string.IsNullOrWhiteSpace(op.RealName)) return op.RealName.Trim();
        return op.UserName;
    }
}
