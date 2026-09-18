using CRM.Core.Models.Work;

namespace CRM.Core.Interfaces;

public interface IWorkCalendarService
{
    Task<WorkCalendarMonthDto> GetMonthAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<WorkCalendarDayDto> GetDayAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> CreateTaskAsync(
        string userId,
        WorkTaskCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> GetTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> PatchTaskAsync(
        string userId,
        string taskId,
        WorkTaskPatchRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> StartTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> CompleteTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailDto> CancelTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task SoftDeleteTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkTaskAssigneeOptionDto>> ListAssigneesAsync(
        CancellationToken cancellationToken = default);

    Task<CustomerWorkTaskMonthDto> GetCustomerMonthAsync(
        string userId,
        string customerId,
        int year,
        int month,
        bool includeCancelled,
        CancellationToken cancellationToken = default);

    Task<PagedCustomerWorkTasksDto> ListCustomerTasksAsync(
        string userId,
        string customerId,
        int page,
        int pageSize,
        DateOnly? startDate,
        bool includeCancelled,
        CancellationToken cancellationToken = default);

    Task<CustomerWorkTaskItemDto> GetCustomerTaskAsync(
        string userId,
        string customerId,
        string taskId,
        CancellationToken cancellationToken = default);
}

public sealed class WorkCalendarMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<WorkCalendarDayCountDto> Days { get; set; } = new();
}

public sealed class WorkCalendarDayCountDto
{
    public string Date { get; set; } = string.Empty;
    public int RfqCount { get; set; }
    public int SoCount { get; set; }
    public int TaskCount { get; set; }
}

public sealed class WorkCalendarDayDto
{
    public string Date { get; set; } = string.Empty;
    public List<WorkCalendarDocItemDto> Rfqs { get; set; } = new();
    public List<WorkCalendarDocItemDto> SalesOrders { get; set; } = new();
    public List<WorkTaskListItemDto> Tasks { get; set; } = new();
}

public sealed class WorkCalendarDocItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
}

public class WorkTaskListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public short Status { get; set; }
    public short Priority { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string ObjectId { get; set; } = string.Empty;
}

public class WorkTaskDetailDto : WorkTaskListItemDto
{
    public string ObjectType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string AssigneeUserId { get; set; } = string.Empty;
    public string? AssigneeUserName { get; set; }
    public string CreateByUserId { get; set; } = string.Empty;
    public string? ContactHistoryId { get; set; }
}

public sealed class CustomerWorkTaskItemDto : WorkTaskDetailDto
{
    public bool CanWrite { get; set; }
}

public sealed class CustomerWorkTaskMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<CustomerWorkTaskDayCountDto> Days { get; set; } = new();
}

public sealed class CustomerWorkTaskDayCountDto
{
    public string Date { get; set; } = string.Empty;
    public int TaskCount { get; set; }
}

public sealed class PagedCustomerWorkTasksDto
{
    public List<CustomerWorkTaskItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class WorkTaskCreateRequest
{
    public string ObjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateOnly StartDate { get; set; }
    public short Priority { get; set; } = 2;
    public string? AssigneeUserId { get; set; }
}

public sealed class WorkTaskPatchRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateOnly? StartDate { get; set; }
    public short? Priority { get; set; }
    public string? AssigneeUserId { get; set; }
    public short? Status { get; set; }
}

public sealed class WorkTaskAssigneeOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
}
