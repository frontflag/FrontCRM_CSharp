using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;

namespace CRM.TestCommon.Rfq;

/// <summary>内存仓储测试用：与旧版 RFQService 需求明细列表内存实现行为对齐。</summary>
public sealed class MemoryRfqItemListQuery : IRfqItemListQuery
{
    private const int MaxPageSize = 100;

    private readonly IRepository<RFQ> _rfqRepo;
    private readonly IRepository<RFQItem> _itemRepo;
    private readonly IRepository<CustomerInfo>? _customerRepo;
    private readonly IRepository<Quote> _quoteRepo;
    private readonly IUserService _userService;
    private readonly IDataPermissionService _dataPermission;

    public MemoryRfqItemListQuery(
        IRepository<RFQ> rfqRepo,
        IRepository<RFQItem> itemRepo,
        IRepository<CustomerInfo>? customerRepo,
        IRepository<Quote> quoteRepo,
        IUserService userService,
        IDataPermissionService dataPermission)
    {
        _rfqRepo = rfqRepo;
        _itemRepo = itemRepo;
        _customerRepo = customerRepo;
        _quoteRepo = quoteRepo;
        _userService = userService;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RFQItemListItem>> GetPagedAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var allRfqs = (await _rfqRepo.GetAllAsync()).ToDictionary(r => r.Id);

        System.Func<RFQ, RFQItem, bool>? linePredicate = null;
        if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            linePredicate = await _dataPermission.GetRfqItemLineVisibilityPredicateAsync(request.CurrentUserId!);

        var customers = new Dictionary<string, string>();
        if (_customerRepo != null)
        {
            var allCustomers = await _customerRepo.GetAllAsync();
            customers = allCustomers.ToDictionary(c => c.Id, c => c.OfficialName ?? c.NickName ?? "");
        }

        var users = (await _userService.GetAllAsync()).ToDictionary(u => u.Id, u => u);

        var allItems = (await _itemRepo.GetAllAsync()).ToList();

        var allQuotes = (await _quoteRepo.GetAllAsync()).ToList();
        var itemIdsWithQuoteHeader = new HashSet<string>(
            allQuotes
                .Where(q => !string.IsNullOrWhiteSpace(q.RFQItemId))
                .Select(q => q.RFQItemId!.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var rows = new List<RFQItemListItem>();
        foreach (var item in allItems)
        {
            if (!allRfqs.TryGetValue(item.RfqId, out var rfq))
                continue;
            if (linePredicate != null && !linePredicate(rfq, item))
                continue;

            users.TryGetValue(rfq.SalesUserId ?? "", out var salesUser);
            users.TryGetValue(item.AssignedPurchaserUserId1 ?? "", out var pu1);
            users.TryGetValue(item.AssignedPurchaserUserId2 ?? "", out var pu2);
            var customerName = rfq.CustomerId != null && customers.TryGetValue(rfq.CustomerId, out var cn)
                ? cn
                : null;

            var lineStatus = item.Status;
            if (lineStatus == 0 && itemIdsWithQuoteHeader.Contains((item.Id ?? string.Empty).Trim()))
                lineStatus = 1;

            rows.Add(new RFQItemListItem
            {
                Id = item.Id ?? string.Empty,
                RfqId = item.RfqId,
                RfqCode = rfq.RfqCode,
                RfqCreateTime = rfq.CreateTime,
                LineNo = item.LineNo,
                Mpn = item.Mpn,
                CustomerMpn = item.CustomerMpn,
                CustomerBrand = string.IsNullOrWhiteSpace(item.CustomerBrand) ? null : item.CustomerBrand.Trim(),
                Brand = item.Brand,
                Quantity = item.Quantity,
                Status = lineStatus,
                CustomerId = rfq.CustomerId,
                CustomerName = customerName,
                SalesUserId = rfq.SalesUserId,
                SalesUserName = EntityLookupService.FormatUserLoginName(salesUser),
                AssignedPurchaserUserId1 = item.AssignedPurchaserUserId1,
                AssignedPurchaserUserId2 = item.AssignedPurchaserUserId2,
                AssignedPurchaserName1 = EntityLookupService.FormatUserLoginName(pu1),
                AssignedPurchaserName2 = EntityLookupService.FormatUserLoginName(pu2),
            });
        }

        if (request.StartDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            rows = rows.Where(r => r.RfqCreateTime >= start).ToList();
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = request.EndDate.Value.Date.AddDays(1);
            rows = rows.Where(r => r.RfqCreateTime < endExclusive).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerKeyword))
        {
            var kw = request.CustomerKeyword.Trim().ToLowerInvariant();
            rows = rows.Where(r =>
                (r.CustomerName != null && r.CustomerName.ToLowerInvariant().Contains(kw)) ||
                (r.CustomerId != null && r.CustomerId.ToLowerInvariant().Contains(kw))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialModel))
        {
            var kw = request.MaterialModel.Trim().ToLowerInvariant();
            rows = rows.Where(r =>
                r.Mpn.ToLowerInvariant().Contains(kw) ||
                (request.CanViewCustomerInList && r.CustomerMpn != null && r.CustomerMpn.ToLowerInvariant().Contains(kw)))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var sid = request.SalesUserId.Trim();
            rows = rows.Where(r =>
                r.SalesUserId != null &&
                string.Equals(r.SalesUserId, sid, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(request.SalesUserKeyword))
        {
            var kw = request.SalesUserKeyword.Trim().ToLowerInvariant();
            rows = rows.Where(r =>
                (r.SalesUserName != null && r.SalesUserName.ToLowerInvariant().Contains(kw)) ||
                (r.SalesUserId != null && r.SalesUserId.ToLowerInvariant().Contains(kw)) ||
                (users.TryGetValue(r.SalesUserId ?? "", out var u) &&
                 (u.UserName.ToLowerInvariant().Contains(kw) ||
                  (u.RealName != null && u.RealName.ToLowerInvariant().Contains(kw))))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaserUserId))
        {
            var pid = request.PurchaserUserId.Trim();
            rows = rows.Where(r =>
                string.Equals(r.AssignedPurchaserUserId1, pid, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.AssignedPurchaserUserId2, pid, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (request.HasQuotesOnly == true)
        {
            var rfqItemIdsWithQuote = new HashSet<string>(
                allQuotes
                    .Where(q => !string.IsNullOrWhiteSpace(q.RFQItemId))
                    .Select(q => q.RFQItemId!.Trim()),
                StringComparer.OrdinalIgnoreCase);
            rows = rows.Where(r => rfqItemIdsWithQuote.Contains(r.Id.Trim())).ToList();
        }

        rows = rows.OrderByDescending(r => r.RfqCreateTime).ThenBy(r => r.LineNo).ToList();

        var totalCount = rows.Count;
        var pagedItems = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<RFQItemListItem>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
