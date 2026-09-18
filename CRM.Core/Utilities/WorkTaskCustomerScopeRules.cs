using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public static class WorkTaskCustomerScopeRules
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 50;

    public static bool IncludeInCustomerList(bool isDeleted, short status, bool includeCancelled)
    {
        if (isDeleted) return false;
        if (includeCancelled) return true;
        return status != WorkTaskStatuses.Cancelled;
    }

    public static bool CanWrite(
        string userId,
        string createByUserId,
        string assigneeUserId,
        bool hasWritePermission)
    {
        if (!hasWritePermission) return false;
        var uid = (userId ?? string.Empty).Trim();
        if (uid.Length == 0) return false;
        return string.Equals(uid, createByUserId?.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(uid, assigneeUserId?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static int ClampPage(int page) => page < 1 ? 1 : page;

    public static int ClampPageSize(int pageSize)
    {
        if (pageSize <= 0) return DefaultPageSize;
        return Math.Min(pageSize, MaxPageSize);
    }
}
