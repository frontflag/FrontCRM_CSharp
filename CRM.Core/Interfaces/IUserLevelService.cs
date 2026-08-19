using CRM.Core.Models.System;

namespace CRM.Core.Interfaces;

public interface IUserLevelService
{
    Task<UserLevelChangeResult> ChangeAsync(
        string targetUserId,
        short newLevel,
        string? remark,
        string operatorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserLevelHistory>> GetHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed class UserLevelChangeResult
{
    public required string UserId { get; init; }
    public required short Level { get; init; }
    public DateTime? LevelChangedAt { get; init; }
    public string? LevelRemark { get; init; }
    public bool LevelChanged { get; init; }
}
