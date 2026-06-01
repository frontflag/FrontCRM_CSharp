using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>解析删除/操作日志中的操作人展示名。</summary>
public static class OperationLogActorResolver
{
    public static async Task<(string? UserId, string UserName)> ResolveAsync(
        IUserService? userService,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = ActingUserIdNormalizer.Normalize(actingUserId);
        if (string.IsNullOrEmpty(id))
            return (null, "系统");

        if (userService == null)
            return (id, id);

        var user = await userService.GetByIdAsync(id);
        var name = string.IsNullOrWhiteSpace(user?.UserName) ? id : user!.UserName!.Trim();
        return (id, name);
    }
}
