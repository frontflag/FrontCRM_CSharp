using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Work;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.WorkCalendar;

public sealed class WorkCalendarService : IWorkCalendarService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerService _customers;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRbacService _rbac;

    public WorkCalendarService(
        ApplicationDbContext db,
        IUnitOfWork unitOfWork,
        ICustomerService customers,
        IDataPermissionService dataPermission,
        IRbacService rbac)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _customers = customers;
        _dataPermission = dataPermission;
        _rbac = rbac;
    }

    public async Task<WorkCalendarMonthDto> GetMonthAsync(
        string userId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > 2100 || month is < 1 or > 12)
            throw new ArgumentException("年月无效。");

        var uid = userId.Trim();
        var (startUtc, endUtc) = CompanyCalendarDate.MonthUtcRange(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var perms = await _rbac.GetUserPermissionSummaryAsync(uid);
        var counts = new Dictionary<string, WorkCalendarDayCountDto>(StringComparer.Ordinal);

        if (HasApiPermission(perms, "rfq.read"))
        {
            var assignedAts = await _db.RFQs.AsNoTracking()
                .Where(r => r.SalesUserId == uid
                    && r.AssignedAt != null
                    && r.AssignedAt >= startUtc
                    && r.AssignedAt < endUtc)
                .Select(r => new { r.AssignedAt, r.Status })
                .ToListAsync(cancellationToken);
            foreach (var row in assignedAts)
            {
                if (row.AssignedAt == null || !WorkCalendarDotRules.CountsAsRfqAssignedDot(row.Status))
                    continue;
                AddCount(counts, CompanyCalendarDate.ToCompanyDate(row.AssignedAt.Value), rfq: 1);
            }
        }

        if (HasApiPermission(perms, "sales-order.read"))
        {
            var approvedAts = await _db.SellOrders.AsNoTracking()
                .Where(s => s.SalesUserId == uid
                    && s.ApprovedAt != null
                    && s.ApprovedAt >= startUtc
                    && s.ApprovedAt < endUtc)
                .Select(s => new { s.ApprovedAt, s.Status })
                .ToListAsync(cancellationToken);
            foreach (var row in approvedAts)
            {
                if (row.ApprovedAt == null || !WorkCalendarDotRules.CountsAsSalesOrderApprovedDot(row.Status))
                    continue;
                AddCount(counts, CompanyCalendarDate.ToCompanyDate(row.ApprovedAt.Value), so: 1);
            }
        }

        if (HasApiPermission(perms, WorkTaskPermissionCodes.Read))
        {
            var starts = await _db.WorkTasks.AsNoTracking()
                .Where(t => t.AssigneeUserId == uid
                    && t.StartDate >= monthStart
                    && t.StartDate < monthEnd
                    && t.Status != WorkTaskStatuses.Cancelled)
                .Select(t => t.StartDate)
                .ToListAsync(cancellationToken);
            foreach (var d in starts)
                AddCount(counts, d, task: 1);
        }

        return new WorkCalendarMonthDto
        {
            Year = year,
            Month = month,
            Days = counts.Values.OrderBy(x => x.Date).ToList()
        };
    }

    public async Task<WorkCalendarDayDto> GetDayAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        var (startUtc, endUtc) = CompanyCalendarDate.DayUtcRange(date);
        var dateText = date.ToString("yyyy-MM-dd");
        var perms = await _rbac.GetUserPermissionSummaryAsync(uid);
        var dto = new WorkCalendarDayDto { Date = dateText };

        if (HasApiPermission(perms, "rfq.read"))
        {
            var rows = await _db.RFQs.AsNoTracking()
                .Where(r => r.SalesUserId == uid
                    && r.AssignedAt != null
                    && r.AssignedAt >= startUtc
                    && r.AssignedAt < endUtc)
                .Select(r => new { r.Id, r.RfqCode, r.CustomerId, r.Status, r.AssignedAt })
                .ToListAsync(cancellationToken);
            var customerNames = await LoadCustomerNamesAsync(
                rows.Select(r => r.CustomerId).ToList(),
                cancellationToken);
            foreach (var row in rows)
            {
                if (!WorkCalendarDotRules.CountsAsRfqAssignedDot(row.Status))
                    continue;
                dto.Rfqs.Add(new WorkCalendarDocItemDto
                {
                    Id = row.Id,
                    Code = row.RfqCode,
                    CustomerName = LookupName(customerNames, row.CustomerId)
                });
            }
        }

        if (HasApiPermission(perms, "sales-order.read"))
        {
            var rows = await _db.SellOrders.AsNoTracking()
                .Where(s => s.SalesUserId == uid
                    && s.ApprovedAt != null
                    && s.ApprovedAt >= startUtc
                    && s.ApprovedAt < endUtc)
                .Select(s => new { s.Id, s.SellOrderCode, s.CustomerName, s.CustomerId, s.Status })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!WorkCalendarDotRules.CountsAsSalesOrderApprovedDot(row.Status))
                    continue;
                dto.SalesOrders.Add(new WorkCalendarDocItemDto
                {
                    Id = row.Id,
                    Code = row.SellOrderCode,
                    CustomerName = string.IsNullOrWhiteSpace(row.CustomerName) ? null : row.CustomerName.Trim()
                });
            }
        }

        if (HasApiPermission(perms, WorkTaskPermissionCodes.Read))
        {
            var tasks = await _db.WorkTasks.AsNoTracking()
                .Where(t => t.AssigneeUserId == uid
                    && t.StartDate == date
                    && t.Status != WorkTaskStatuses.Cancelled)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreateTime)
                .ToListAsync(cancellationToken);
            var names = await LoadCustomerNamesAsync(
                tasks.Where(t => t.ObjectType == WorkTaskObjectTypes.Customer).Select(t => t.ObjectId).ToList(),
                cancellationToken);
            dto.Tasks.AddRange(tasks.Select(t => ToListItem(t, names)));
        }

        return dto;
    }

    public async Task<WorkTaskDetailDto> CreateTaskAsync(
        string userId,
        WorkTaskCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var title = RequireTitle(request.Title);
        var customer = await RequireVisibleCustomerAsync(uid, request.ObjectId);
        var assigneeId = string.IsNullOrWhiteSpace(request.AssigneeUserId)
            ? uid
            : request.AssigneeUserId.Trim();
        await EnsureActiveUserAsync(assigneeId);

        var task = new WorkTask
        {
            Id = Guid.NewGuid().ToString(),
            CreateTime = DateTime.UtcNow,
            CreateByUserId = uid,
            Status = WorkTaskStatuses.Pending,
            ObjectType = WorkTaskObjectTypes.Customer,
            ObjectId = customer.Id,
            Title = title,
            Content = EmptyToNull(request.Content),
            StartDate = request.StartDate,
            Priority = NormalizePriority(request.Priority),
            AssigneeUserId = assigneeId
        };
        _db.WorkTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        await AppendLogAsync(task, "status", "状态", null, "待开始", uid);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task<WorkTaskDetailDto> GetTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await RequireVisibleTaskAsync(userId, taskId, cancellationToken);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task<WorkTaskDetailDto> PatchTaskAsync(
        string userId,
        string taskId,
        WorkTaskPatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var task = await RequireOwnedTaskAsync(uid, taskId, cancellationToken);
        if (request.StartDate != null || request.AssigneeUserId != null)
        {
            if (!WorkTaskStatusRules.CanEditCore(task.Status))
                throw new InvalidOperationException("已完成或已取消的任务不能改开始日或执行人。");
        }

        if (request.Title != null)
        {
            var next = RequireTitle(request.Title);
            if (!string.Equals(task.Title, next, StringComparison.Ordinal))
            {
                await AppendLogAsync(task, "title", "标题", task.Title, next, uid);
                task.Title = next;
            }
        }

        if (request.Content != null)
        {
            var next = EmptyToNull(request.Content);
            if (!string.Equals(task.Content, next, StringComparison.Ordinal))
            {
                await AppendLogAsync(task, "content", "内容", task.Content, next, uid);
                task.Content = next;
            }
        }

        if (request.StartDate != null && request.StartDate.Value != task.StartDate)
        {
            await AppendLogAsync(task, "startDate", "开始日期", task.StartDate.ToString("yyyy-MM-dd"), request.StartDate.Value.ToString("yyyy-MM-dd"), uid);
            task.StartDate = request.StartDate.Value;
        }

        if (request.Priority != null)
        {
            var next = NormalizePriority(request.Priority.Value);
            if (next != task.Priority)
            {
                await AppendLogAsync(task, "priority", "紧急程度", FormatPriority(task.Priority), FormatPriority(next), uid);
                task.Priority = next;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AssigneeUserId))
        {
            var next = request.AssigneeUserId.Trim();
            if (!string.Equals(next, task.AssigneeUserId, StringComparison.OrdinalIgnoreCase))
            {
                await EnsureActiveUserAsync(next);
                await AppendLogAsync(task, "assignee", "执行人", task.AssigneeUserId, next, uid);
                task.AssigneeUserId = next;
            }
        }

        if (request.Status == WorkTaskStatuses.InProgress)
            await ApplyStartLogged(task, uid);

        task.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task<WorkTaskDetailDto> StartTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var task = await RequireOwnedTaskAsync(uid, taskId, cancellationToken);
        await ApplyStartLogged(task, uid);
        task.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task<WorkTaskDetailDto> CompleteTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var task = await RequireOwnedTaskAsync(uid, taskId, cancellationToken);
        if (task.Status == WorkTaskStatuses.Completed)
            return await ToDetailAsync(task, cancellationToken);
        if (!WorkTaskStatusRules.CanComplete(task.Status))
            throw new InvalidOperationException("当前状态不能完成。");

        var oldStatus = FormatStatus(task.Status);
        task.Status = WorkTaskStatuses.Completed;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedByUserId = uid;
        task.ModifyTime = DateTime.UtcNow;

        if (task.ObjectType == WorkTaskObjectTypes.Customer && string.IsNullOrWhiteSpace(task.ContactHistoryId))
        {
            await RequireVisibleCustomerAsync(uid, task.ObjectId);
            var history = await _customers.AddContactHistoryAsync(task.ObjectId, new AddContactHistoryRequest
            {
                Type = "other",
                Subject = task.Title,
                Content = task.Content,
                Time = task.CompletedAt,
                NextFollowUpTime = null,
                Result = "任务完成"
            });
            history.OperatorId = uid;
            _db.CustomerContactHistories.Update(history);
            task.ContactHistoryId = history.Id;
        }

        await AppendLogAsync(task, "status", "状态", oldStatus, "完成", uid);
        await _db.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task<WorkTaskDetailDto> CancelTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var task = await RequireOwnedTaskAsync(uid, taskId, cancellationToken);
        if (!WorkTaskStatusRules.CanCancel(task.Status))
            throw new InvalidOperationException("当前状态不能取消。");
        var oldStatus = FormatStatus(task.Status);
        task.Status = WorkTaskStatuses.Cancelled;
        task.ModifyTime = DateTime.UtcNow;
        await AppendLogAsync(task, "status", "状态", oldStatus, "取消", uid);
        await _db.SaveChangesAsync(cancellationToken);
        return await ToDetailAsync(task, cancellationToken);
    }

    public async Task SoftDeleteTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var uid = userId.Trim();
        await EnsureWriteAsync(uid);
        var task = await RequireOwnedTaskAsync(uid, taskId, cancellationToken);
        task.IsDeleted = true;
        task.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkTaskAssigneeOptionDto>> ListAssigneesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Status == UserAccountStatus.Active)
            .OrderBy(u => u.UserName)
            .Select(u => new WorkTaskAssigneeOptionDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName
            })
            .ToListAsync(cancellationToken);
    }

    private async Task ApplyStartLogged(WorkTask task, string uid)
    {
        if (task.Status == WorkTaskStatuses.InProgress)
            return;
        if (!WorkTaskStatusRules.CanStart(task.Status))
            throw new InvalidOperationException("当前状态不能开始。");
        var old = FormatStatus(task.Status);
        task.Status = WorkTaskStatuses.InProgress;
        await AppendLogAsync(task, "status", "状态", old, "进行中", uid);
    }

    private async Task<WorkTask> RequireVisibleTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken)
    {
        var uid = userId.Trim();
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId.Trim(), cancellationToken)
            ?? throw new KeyNotFoundException("任务不存在。");
        if (!string.Equals(task.AssigneeUserId, uid, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(task.CreateByUserId, uid, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("不能查看他人任务。");
        return task;
    }

    private async Task<WorkTask> RequireOwnedTaskAsync(
        string userId,
        string taskId,
        CancellationToken cancellationToken)
    {
        var uid = userId.Trim();
        var task = await _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId.Trim(), cancellationToken)
            ?? throw new KeyNotFoundException("任务不存在。");
        if (!string.Equals(task.AssigneeUserId, uid, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(task.CreateByUserId, uid, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("不能修改他人任务。");
        return task;
    }

    private async Task<CustomerInfo> RequireVisibleCustomerAsync(string userId, string? customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("请选择客户。");
        var customer = await _customers.GetCustomerByIdAsync(customerId.Trim())
            ?? throw new KeyNotFoundException("客户不存在。");
        if (!await _dataPermission.CanAccessCustomerAsync(userId, customer))
            throw new UnauthorizedAccessException("无权使用该客户。");
        return customer;
    }

    private async Task EnsureActiveUserAsync(string userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ArgumentException("执行人不存在。");
        if (!user.IsActive || user.Status != UserAccountStatus.Active)
            throw new ArgumentException("执行人未启用。");
    }

    private async Task EnsureWriteAsync(string userId)
    {
        var perms = await _rbac.GetUserPermissionSummaryAsync(userId);
        if (!HasApiPermission(perms, WorkTaskPermissionCodes.Write))
            throw new UnauthorizedAccessException("无任务维护权限。");
    }

    private async Task<Dictionary<string, string>> LoadCustomerNamesAsync(
        IReadOnlyList<string?> ids,
        CancellationToken cancellationToken)
    {
        var keys = ids
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (keys.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return await _db.Customers.AsNoTracking()
            .Where(c => keys.Contains(c.Id))
            .Select(c => new { c.Id, c.OfficialName })
            .ToDictionaryAsync(
                c => c.Id,
                c => (c.OfficialName ?? string.Empty).Trim(),
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
    }

    private async Task<WorkTaskDetailDto> ToDetailAsync(WorkTask task, CancellationToken cancellationToken)
    {
        var names = await LoadCustomerNamesAsync(new[] { task.ObjectId }, cancellationToken);
        var assignee = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == task.AssigneeUserId, cancellationToken);
        var item = ToListItem(task, names);
        return new WorkTaskDetailDto
        {
            Id = item.Id,
            Title = item.Title,
            Status = item.Status,
            Priority = item.Priority,
            StartDate = item.StartDate,
            CustomerName = item.CustomerName,
            ObjectId = item.ObjectId,
            ObjectType = task.ObjectType,
            Content = task.Content,
            AssigneeUserId = task.AssigneeUserId,
            AssigneeUserName = assignee == null
                ? null
                : string.IsNullOrWhiteSpace(assignee.RealName) ? assignee.UserName : assignee.RealName,
            CreateByUserId = task.CreateByUserId,
            ContactHistoryId = task.ContactHistoryId
        };
    }

    private static WorkTaskListItemDto ToListItem(WorkTask task, IReadOnlyDictionary<string, string> names) =>
        new()
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            Priority = task.Priority,
            StartDate = task.StartDate.ToString("yyyy-MM-dd"),
            CustomerName = LookupName(names, task.ObjectId),
            ObjectId = task.ObjectId
        };

    private async Task AppendLogAsync(
        WorkTask task,
        string field,
        string label,
        string? oldValue,
        string? newValue,
        string actingUserId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actingUserId);
        var userName = string.IsNullOrWhiteSpace(user?.UserName) ? actingUserId : user!.UserName;
        await FieldChangeLogAppender.AppendIfChangedAsync(
            _unitOfWork,
            BusinessLogTypes.WorkTask,
            task.Id,
            task.Title,
            field,
            label,
            oldValue,
            newValue,
            actingUserId,
            userName);
    }

    private static void AddCount(
        Dictionary<string, WorkCalendarDayCountDto> map,
        DateOnly date,
        int rfq = 0,
        int so = 0,
        int task = 0)
    {
        var key = date.ToString("yyyy-MM-dd");
        if (!map.TryGetValue(key, out var row))
        {
            row = new WorkCalendarDayCountDto { Date = key };
            map[key] = row;
        }

        row.RfqCount += rfq;
        row.SoCount += so;
        row.TaskCount += task;
    }

    private static string? LookupName(IReadOnlyDictionary<string, string> names, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return names.TryGetValue(id.Trim(), out var n) && !string.IsNullOrWhiteSpace(n) ? n : null;
    }

    private static string RequireTitle(string? title)
    {
        var t = (title ?? string.Empty).Trim();
        if (t.Length == 0)
            throw new ArgumentException("请填写标题。");
        if (t.Length > 200)
            throw new ArgumentException("标题最长 200 字。");
        return t;
    }

    private static string? EmptyToNull(string? value)
    {
        var t = value?.Trim();
        if (string.IsNullOrEmpty(t))
            return null;
        if (t.Length > 2000)
            throw new ArgumentException("内容最长 2000 字。");
        return t;
    }

    private static short NormalizePriority(short priority) =>
        priority is >= 0 and <= 3 ? priority : WorkTaskPriorities.P2;

    private static string FormatStatus(short status) => status switch
    {
        WorkTaskStatuses.Pending => "待开始",
        WorkTaskStatuses.InProgress => "进行中",
        WorkTaskStatuses.Completed => "完成",
        WorkTaskStatuses.Cancelled => "取消",
        _ => status.ToString()
    };

    private static string FormatPriority(short priority) => $"P{priority}";

    private static bool HasApiPermission(UserPermissionSummaryDto summary, string code)
    {
        if (summary.IsSysAdmin)
            return true;
        return summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
    }
}
