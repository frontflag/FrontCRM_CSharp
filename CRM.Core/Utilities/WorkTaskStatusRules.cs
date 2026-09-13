using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public static class WorkTaskStatusRules
{
    public static bool IsTerminal(short status) =>
        status is WorkTaskStatuses.Completed or WorkTaskStatuses.Cancelled;

    public static bool CanStart(short status) => status == WorkTaskStatuses.Pending;

    public static bool CanComplete(short status) =>
        status is WorkTaskStatuses.Pending or WorkTaskStatuses.InProgress;

    public static bool CanCancel(short status) =>
        status is WorkTaskStatuses.Pending or WorkTaskStatuses.InProgress;

    public static bool CanEditCore(short status) => !IsTerminal(status);

    public static bool CountsAsOrangeDot(short status, bool isDeleted) =>
        !isDeleted && status != WorkTaskStatuses.Cancelled;
}
