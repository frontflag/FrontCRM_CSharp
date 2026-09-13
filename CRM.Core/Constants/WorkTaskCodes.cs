namespace CRM.Core.Constants;

public static class WorkTaskPermissionCodes
{
    public const string Read = "work-task.read";
    public const string Write = "work-task.write";
}

public static class WorkTaskObjectTypes
{
    public const string Customer = "CUSTOMER";
}

public static class WorkTaskStatuses
{
    public const short Pending = 10;
    public const short InProgress = 20;
    public const short Completed = 30;
    public const short Cancelled = 90;
}

public static class WorkTaskPriorities
{
    /// <summary>P0 最紧急。</summary>
    public const short P0 = 0;
    public const short P1 = 1;
    public const short P2 = 2;
    public const short P3 = 3;
}
