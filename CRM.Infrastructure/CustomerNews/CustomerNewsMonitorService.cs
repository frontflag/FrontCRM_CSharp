using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.CustomerNews;

public sealed class CustomerNewsMonitorService : ICustomerNewsMonitorService
{
    readonly ApplicationDbContext _db;
    readonly IAiOrchestrator _orchestrator;
    readonly IRbacService _rbac;
    readonly IDataPermissionService _dataPermission;
    readonly ILogger<CustomerNewsMonitorService> _logger;

    public CustomerNewsMonitorService(
        ApplicationDbContext db,
        IAiOrchestrator orchestrator,
        IRbacService rbac,
        IDataPermissionService dataPermission,
        ILogger<CustomerNewsMonitorService> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _rbac = rbac;
        _dataPermission = dataPermission;
        _logger = logger;
    }

    public async Task<CustomerNewsListDto> ListAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAccessAsync(customerId, userId, cancellationToken);
        List<CustomerNewsListItemDto> items;
        try
        {
            items = await _db.CustomerNewsBriefings.AsNoTracking()
                .Where(x => x.CustomerId == ctx.Customer.Id && x.Status == CustomerNewsCodes.StatusSuccess)
                .OrderByDescending(x => x.GeneratedAt)
                .Select(x => new CustomerNewsListItemDto
                {
                    Id = x.Id,
                    BriefingDate = x.BriefingDate,
                    PeriodStart = x.PeriodStart,
                    PeriodEnd = x.PeriodEnd,
                    GeneratedAt = x.GeneratedAt
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取客户新闻列表失败，仍返回抓取权限 customerId={CustomerId}", ctx.Customer.Id);
            items = new List<CustomerNewsListItemDto>();
        }

        return new CustomerNewsListDto
        {
            CanFetch = ctx.CanFetch,
            CanDelete = ctx.CanDelete,
            Items = items
        };
    }

    public async Task<CustomerNewsDetailDto?> GetByIdAsync(
        string customerId,
        string briefingId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAccessAsync(customerId, userId, cancellationToken);
        var id = (briefingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            return null;

        var row = await _db.CustomerNewsBriefings.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id
                     && x.CustomerId == ctx.Customer.Id
                     && x.Status == CustomerNewsCodes.StatusSuccess,
                cancellationToken);
        return row == null ? null : ToDetail(row);
    }

    public async Task<CustomerNewsLatestRunDto> GetLatestRunAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAccessAsync(customerId, userId, cancellationToken);
        var row = await _db.CustomerNewsBriefings.AsNoTracking()
            .Where(x => x.CustomerId == ctx.Customer.Id)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (row == null)
            return new CustomerNewsLatestRunDto();

        return new CustomerNewsLatestRunDto
        {
            Id = row.Id,
            Status = row.Status,
            Message = row.ErrorMessage,
            GeneratedAt = row.GeneratedAt
        };
    }

    public async Task<CustomerNewsRunResultDto> RunAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAccessAsync(customerId, userId, cancellationToken);
        if (!ctx.CanFetch)
            throw new UnauthorizedAccessException("无权抓取该客户新闻动态");

        var today = CommissionShanghai.Today();
        var lastSuccess = await _db.CustomerNewsBriefings.AsNoTracking()
            .Where(x => x.CustomerId == ctx.Customer.Id && x.Status == CustomerNewsCodes.StatusSuccess)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var (start, end) = CustomerNewsWindow.ForFetch(today, lastSuccess?.PeriodEnd);
        var isFirst = lastSuccess == null;
        var now = DateTime.UtcNow;
        var row = new CustomerNewsBriefing
        {
            Id = Guid.NewGuid().ToString("D"),
            CustomerId = ctx.Customer.Id,
            BriefingDate = today,
            PeriodStart = start,
            PeriodEnd = end,
            Status = CustomerNewsCodes.StatusFailed,
            RequestedBy = userId,
            GeneratedAt = now,
            CreateTime = now,
            ModifyTime = now
        };

        try
        {
            var input = BuildInput(ctx.Customer, start, end, isFirst, lastSuccess?.PeriodEnd);
            var invoke = await _orchestrator.InvokeSystemAsync(
                new AiInvokeRequestDto
                {
                    ScenarioCode = AiScenarioCodes.CustomerNewsMonitor,
                    TriggerType = AiInvocationTriggerType.Manual,
                    ForceRefresh = true,
                    BizType = "CUSTOMER",
                    BizId = ctx.Customer.Id,
                    Input = input
                },
                userId,
                cancellationToken);

            var markdown = CustomerNewsMarkdownSanitizer.SanitizeForPersist(invoke.Content);
            if (string.IsNullOrWhiteSpace(markdown))
            {
                _logger.LogWarning(
                    "客户新闻简报被丢弃（无「## 核心摘要」或检索循环）customerId={CustomerId} invocationId={InvocationId} rawLen={RawLen} preview={Preview}",
                    ctx.Customer.Id,
                    invoke.InvocationId,
                    invoke.Content?.Length ?? 0,
                    Truncate(invoke.Content, 240));
                throw new InvalidOperationException("模型未生成有效简报，请重新抓取");
            }

            row.Markdown = markdown;
            row.Status = CustomerNewsCodes.StatusSuccess;
            row.ErrorMessage = null;
            row.InvocationId = invoke.InvocationId;
            row.GeneratedAt = DateTime.UtcNow;
            row.ModifyTime = row.GeneratedAt;
            _db.CustomerNewsBriefings.Add(row);
            await _db.SaveChangesAsync(cancellationToken);

            return new CustomerNewsRunResultDto
            {
                Ran = true,
                Success = true,
                Message = "已生成",
                AcceptedAt = row.GeneratedAt,
                Latest = ToDetail(row)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Customer news monitor failed customerId={CustomerId}", ctx.Customer.Id);
            row.ErrorMessage = Truncate(ex.Message, 500);
            row.GeneratedAt = DateTime.UtcNow;
            row.ModifyTime = row.GeneratedAt;
            try
            {
                _db.CustomerNewsBriefings.Add(row);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to persist customer news failure row");
            }

            return new CustomerNewsRunResultDto
            {
                Ran = true,
                Success = false,
                Message = ex.Message,
                AcceptedAt = row.GeneratedAt
            };
        }
    }

    public async Task DeleteAsync(
        string customerId,
        string briefingId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAccessAsync(customerId, userId, cancellationToken);
        if (!ctx.CanDelete)
            throw new UnauthorizedAccessException("仅系统管理员可删除新闻动态");

        var id = (briefingId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            throw new KeyNotFoundException("没有该次简报");

        var row = await _db.CustomerNewsBriefings
            .FirstOrDefaultAsync(
                x => x.Id == id
                     && x.CustomerId == ctx.Customer.Id
                     && x.Status == CustomerNewsCodes.StatusSuccess,
                cancellationToken)
            ?? throw new KeyNotFoundException("没有该次简报");

        _db.CustomerNewsBriefings.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "已删除客户新闻简报 customerId={CustomerId} briefingId={BriefingId} by={UserId}",
            ctx.Customer.Id,
            row.Id,
            userId);
    }

    async Task<AccessContext> LoadAccessAsync(string customerId, string userId, CancellationToken cancellationToken)
    {
        var id = (customerId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            throw new KeyNotFoundException("客户不存在");

        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("客户不存在");

        var uid = (userId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(uid))
            throw new UnauthorizedAccessException("未登录");

        if (!await _dataPermission.CanAccessCustomerAsync(uid, customer))
            throw new UnauthorizedAccessException("无权限访问该客户");

        var summary = await _rbac.GetUserPermissionSummaryAsync(uid);
        if (SaleSensitiveFieldMask521.ShouldMask(summary))
            throw new UnauthorizedAccessException("当前账号已脱敏，不可查看新闻动态");

        var isOwner = !string.IsNullOrWhiteSpace(customer.SalesUserId)
                       && string.Equals(customer.SalesUserId, uid, StringComparison.OrdinalIgnoreCase);

        var salespersonInAllow = false;
        if (!isOwner && !string.IsNullOrWhiteSpace(customer.SalesUserId))
        {
            var allow = await _dataPermission.GetAllowedUserIdsForDataScopeAsync(summary, includeChildren: true, cancellationToken);
            salespersonInAllow = allow.Contains(customer.SalesUserId);
        }

        return new AccessContext
        {
            Customer = customer,
            CanFetch = CustomerNewsMonitorAccessRules.CanFetch(summary, isOwner, salespersonInAllow),
            CanDelete = CustomerNewsMonitorAccessRules.CanDelete(summary)
        };
    }

    static Dictionary<string, string?> BuildInput(
        CustomerInfo customer,
        DateOnly start,
        DateOnly end,
        bool isFirst,
        DateOnly? previousEnd)
    {
        var nickParts = new[] { customer.NickName, customer.EnglishOfficialName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var hq = string.Join("，", new[] { customer.City, customer.Province }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["company_name"] = string.IsNullOrWhiteSpace(customer.OfficialName)
                ? customer.CustomerCode
                : customer.OfficialName.Trim(),
            ["nick_name"] = nickParts.Count == 0 ? "未维护" : string.Join("、", nickParts),
            ["stock_code"] = "未知（请按公司全称检索是否上市及证券代码，禁止当成未上市）",
            ["industry"] = string.IsNullOrWhiteSpace(customer.Industry) ? "未维护" : customer.Industry.Trim(),
            ["hq_location"] = string.IsNullOrWhiteSpace(hq) ? "未维护" : hq,
            ["related_companies"] = "未维护",
            ["actual_controller"] = "未维护",
            ["window_months"] = isFirst
                ? "首次全量，覆盖近 3 个月；须检索窗口内交易所公告、巨潮、官网与财经媒体。只按上面的起止日检索，不要把起止日理解成未来，禁止写入窗口外旧闻。"
                : "动态监测，只收录该窗口内新信息；窗口可以只有一天，不要再按近 3 个月检索，不要讨论日期是否未来。禁止写入窗口外旧闻。",
            ["start_date"] = start.ToString("yyyy-MM-dd"),
            ["end_date"] = end.ToString("yyyy-MM-dd"),
            ["monitor_mode"] = isFirst
                ? "首次全量监测"
                : $"动态监测：仅收录上次调研截至 {previousEnd:yyyy-MM-dd} 之后的新信息",
            ["change_tracking_rule"] = isFirst
                ? "首次调研，请省略「变化追踪」一节。"
                : "必须输出「变化追踪」一节，与上次调研对比。"
        };
    }

    static CustomerNewsDetailDto ToDetail(CustomerNewsBriefing row) => new()
    {
        Id = row.Id,
        BriefingDate = row.BriefingDate,
        PeriodStart = row.PeriodStart,
        PeriodEnd = row.PeriodEnd,
        GeneratedAt = row.GeneratedAt,
        Markdown = CustomerNewsMarkdownSanitizer.SanitizeForDisplay(row.Markdown ?? string.Empty)
    };

    static string Truncate(string? text, int max)
    {
        var v = text ?? string.Empty;
        return v.Length <= max ? v : v[..max];
    }

    sealed class AccessContext
    {
        public required CustomerInfo Customer { get; init; }
        public required bool CanFetch { get; init; }
        public required bool CanDelete { get; init; }
    }
}
