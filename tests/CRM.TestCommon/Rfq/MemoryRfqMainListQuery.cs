using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;

namespace CRM.TestCommon.Rfq;

/// <summary>内存仓储测试用：与 <c>RfqMainListQuery</c> 筛选顺序对齐（数据权限仍走 <see cref="IDataPermissionService.FilterRFQsAsync"/>）。</summary>
public sealed class MemoryRfqMainListQuery : IRfqMainListQuery
{
    private const int MaxPageSize = 2000;

    private readonly IRepository<RFQ> _rfqRepo;
    private readonly IRepository<CustomerInfo>? _customerRepo;
    private readonly IUserService _userService;
    private readonly IDataPermissionService _dataPermission;

    public MemoryRfqMainListQuery(
        IRepository<RFQ> rfqRepo,
        IRepository<CustomerInfo>? customerRepo,
        IUserService userService,
        IDataPermissionService dataPermission)
    {
        _rfqRepo = rfqRepo;
        _customerRepo = customerRepo;
        _userService = userService;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<RfqMainListQueryPage> GetPagedWithAggregatesAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var all = (await _rfqRepo.GetAllAsync()).ToList();
        var query = all.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.RfqCode.ToLower().Contains(kw) ||
                (r.Industry != null && r.Industry.ToLower().Contains(kw)) ||
                (r.Product != null && r.Product.ToLower().Contains(kw)) ||
                (r.Remark != null && r.Remark.ToLower().Contains(kw)));
        }

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            query = query.Where(r => r.CustomerId == request.CustomerId);

        if (request.StartDate.HasValue)
            query = query.Where(r => r.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(r => r.CreateTime <= request.EndDate.Value.AddDays(1));

        var filteredEntities = query.OrderByDescending(r => r.CreateTime).ToList();

        var customerIds = filteredEntities.Where(r => r.CustomerId != null).Select(r => r.CustomerId!).Distinct().ToList();
        var customers = new Dictionary<string, string>();
        if (customerIds.Count > 0 && _customerRepo != null)
        {
            var allCustomers = await _customerRepo.GetAllAsync();
            customers = allCustomers
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionary(c => c.Id, c => c.OfficialName ?? c.NickName ?? "");
        }

        var users = (await _userService.GetAllAsync())
            .ToDictionary(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

        var listItems = filteredEntities.Select(r =>
        {
            users.TryGetValue(r.SalesUserId ?? string.Empty, out var salesUser);
            users.TryGetValue(r.CreateByUserId ?? string.Empty, out var createUser);
            return new RFQListItem
            {
                Id = r.Id,
                RfqCode = r.RfqCode,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerId != null && customers.ContainsKey(r.CustomerId) ? customers[r.CustomerId] : null,
                Status = r.Status,
                RfqType = r.RfqType,
                TargetType = r.TargetType,
                Industry = r.Industry,
                Product = r.Product,
                Importance = r.Importance,
                ItemCount = r.ItemCount,
                Remark = r.Remark,
                CreateTime = r.CreateTime,
                SalesUserId = r.SalesUserId,
                SalesUserName = EntityLookupService.FormatUserLoginName(salesUser),
                CreateByUserId = r.CreateByUserId,
                CreateUserName = EntityLookupService.FormatUserLoginName(createUser)
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            listItems = (await _dataPermission.FilterRFQsAsync(request.CurrentUserId, listItems)).ToList();

        var idOrder = listItems.Select(x => x.Id).ToList();
        var entityDict = filteredEntities.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        var orderedRfqs = idOrder
            .Select(id => entityDict.TryGetValue(id, out var e) ? e : null)
            .Where(e => e != null)
            .Cast<RFQ>()
            .ToList();

        var total = orderedRfqs.Count;
        var pending = orderedRfqs.Count(r => r.Status == 0);
        var processing = orderedRfqs.Count(r => r.Status == 1 || r.Status == 2);
        var quoted = orderedRfqs.Count(r => r.Status == 3 || r.Status == 4 || r.Status == 5);

        var pageSlice = orderedRfqs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new RfqMainListQueryPage
        {
            Items = pageSlice,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize,
            Aggregates = new RfqMainListAggregates
            {
                Total = total,
                Pending = pending,
                Processing = processing,
                Quoted = quoted
            }
        };
    }
}
