using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class FinanceReceivableStatementQuery : IFinanceReceivableStatementQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IDisplayTimeZoneService _displayTimeZone;
    private readonly IRbacService _rbac;

    public FinanceReceivableStatementQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IDisplayTimeZoneService displayTimeZone,
        IRbacService rbac)
    {
        _db = db;
        _dataPermission = dataPermission;
        _displayTimeZone = displayTimeZone;
        _rbac = rbac;
    }

    public async Task<PagedResult<FinanceReceivableStatementListItem>> GetPagedAsync(
        FinanceReceivableStatementListQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var recv = await BuildScopedReceivablesAsync(request.CurrentUserId, cancellationToken);
        if (request.Currency.HasValue)
            recv = recv.Where(r => r.Currency == request.Currency.Value);

        var grouped = await recv
            .GroupBy(r => new { r.CustomerId, r.Currency })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Currency,
                ReceivableCount = g.Count(),
                AmountTotal = g.Sum(x => x.Amount),
                VerifiedDone = g.Sum(x => x.VerifiedDone),
                VerifiedToBe = g.Sum(x => x.VerifiedToBe),
                LatestStockOutDate = g.Max(x => x.StockOutDate ?? x.CreateTime),
                FallbackSalesUserId = g.Max(x => x.SalesUserId),
                FallbackCustomerName = g.Max(x => x.CustomerName)
            })
            .ToListAsync(cancellationToken);

        var customerIds = grouped
            .Select(g => g.CustomerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.CustomerCode,
                c.OfficialName,
                c.EnglishOfficialName,
                c.SalesUserId
            })
            .ToListAsync(cancellationToken);
        var customerMap = customers.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        var salesIds = customers
            .Select(c => c.SalesUserId)
            .Concat(grouped.Select(g => g.FallbackSalesUserId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var salesMap = await LoadSalesNamesAsync(salesIds, cancellationToken);

        var rows = new List<FinanceReceivableStatementListItem>(grouped.Count);
        foreach (var g in grouped)
        {
            customerMap.TryGetValue(g.CustomerId, out var cust);
            var salesUserId = !string.IsNullOrWhiteSpace(cust?.SalesUserId)
                ? cust.SalesUserId.Trim()
                : g.FallbackSalesUserId?.Trim();
            salesMap.TryGetValue(salesUserId ?? string.Empty, out var salesName);
            rows.Add(new FinanceReceivableStatementListItem
            {
                CustomerId = g.CustomerId,
                CustomerCode = cust?.CustomerCode,
                CustomerName = !string.IsNullOrWhiteSpace(cust?.OfficialName)
                    ? cust.OfficialName.Trim()
                    : g.FallbackCustomerName,
                CustomerEnglishName = string.IsNullOrWhiteSpace(cust?.EnglishOfficialName)
                    ? null
                    : cust.EnglishOfficialName.Trim(),
                Currency = g.Currency,
                ReceivableCount = g.ReceivableCount,
                AmountTotal = g.AmountTotal,
                VerifiedDone = g.VerifiedDone,
                VerifiedToBe = g.VerifiedToBe,
                LatestStockOutDate = g.LatestStockOutDate,
                SalesUserId = salesUserId,
                SalesUserName = salesName
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim();
            rows = rows.Where(r =>
                    ContainsIgnoreCase(r.CustomerCode, k)
                    || ContainsIgnoreCase(r.CustomerName, k)
                    || ContainsIgnoreCase(r.CustomerEnglishName, k)
                    || ContainsIgnoreCase(r.SalesUserName, k))
                .ToList();
        }

        if (request.OnlyOpen)
            rows = rows.Where(r => r.VerifiedToBe > 0m).ToList();

        rows = rows
            .OrderBy(r => r.CustomerName ?? r.CustomerCode ?? r.CustomerId, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Currency)
            .ToList();

        var total = rows.Count;
        var pageItems = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<FinanceReceivableStatementListItem>
        {
            Items = pageItems,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<FinanceReceivableStatementDetailDto?> GetDetailAsync(
        string customerId,
        short currency,
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly agingCutoff,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (periodFrom > periodTo)
            throw new ArgumentException("对账期间起日不能晚于止日。");

        var cid = (customerId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(cid))
            return null;

        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cid, cancellationToken);

        var anyReceivable = await _db.FinanceReceivables.AsNoTracking()
            .AnyAsync(r => r.CustomerId == cid && !r.IsDeleted, cancellationToken);

        var scopedAll = await BuildScopedReceivablesAsync(currentUserId, cancellationToken);
        var scopedForCustomer = await scopedAll.Where(r => r.CustomerId == cid).ToListAsync(cancellationToken);

        if (scopedForCustomer.Count == 0)
        {
            if (customer == null || anyReceivable)
                return null;
        }

        var tz = await _displayTimeZone.GetDisplayTimeZoneIdAsync(cancellationToken);
        var generatedOn = FinanceAccumulatedMonthBoundary.ToDisplayDate(DateTime.UtcNow, tz);

        var scopedForCurrency = scopedForCustomer.Where(r => r.Currency == currency).ToList();
        var receivableIds = scopedForCurrency.Select(r => r.Id).ToList();

        var writeOffs = receivableIds.Count == 0
            ? []
            : await (
                from w in _db.FinanceReceivableWriteOffs.AsNoTracking()
                where !w.IsDeleted && receivableIds.Contains(w.FinanceReceivableId)
                join fr in _db.FinanceReceipts.AsNoTracking() on w.FinanceReceiptId equals fr.Id into frJoin
                from fr in frJoin.DefaultIfEmpty()
                select new { w, ReceiptCode = fr != null ? fr.FinanceReceiptCode : null }
            ).ToListAsync(cancellationToken);

        var events = new List<FinanceReceivableStatementEvent>(scopedForCurrency.Count + writeOffs.Count);
        foreach (var r in scopedForCurrency)
        {
            var date = r.StockOutDate.HasValue
                ? DateOnly.FromDateTime(r.StockOutDate.Value)
                : FinanceAccumulatedMonthBoundary.ToDisplayDate(r.CreateTime, tz);
            var pn = string.IsNullOrWhiteSpace(r.PN) ? string.Empty : r.PN.Trim();
            var summary = string.IsNullOrEmpty(pn) ? r.StockOutCode : $"{r.StockOutCode} {pn}";
            events.Add(new FinanceReceivableStatementEvent
            {
                LineType = FinanceReceivableStatementLineTypes.Increase,
                BusinessDate = date,
                Amount = r.Amount,
                DocNo = r.ReceivableCode,
                Summary = summary,
                ReceivableId = r.Id,
                StockOutId = r.StockOutId
            });
        }

        foreach (var row in writeOffs)
        {
            var isAdvance = row.w.WriteOffSource == FinanceReceivableWriteOffSourceCode.AdvancePool;
            events.Add(new FinanceReceivableStatementEvent
            {
                LineType = FinanceReceivableStatementLineTypes.Receipt,
                BusinessDate = FinanceAccumulatedMonthBoundary.ToDisplayDate(row.w.CreateTime, tz),
                Amount = row.w.Amount,
                DocNo = isAdvance ? null : row.ReceiptCode,
                Summary = isAdvance
                    ? "预收核销"
                    : string.IsNullOrWhiteSpace(row.ReceiptCode)
                        ? "收款核销"
                        : $"收款核销 {row.ReceiptCode}",
                ReceivableId = row.w.FinanceReceivableId,
                ReceiptId = row.w.FinanceReceiptId,
                WriteOffId = row.w.Id
            });
        }

        var header = FinanceReceivableStatementCalculator.Compute(
            periodFrom,
            periodTo,
            generatedOn,
            agingCutoff,
            currency,
            events,
            out var lines);

        var customerDto = await BuildCustomerDtoAsync(
            cid,
            customer,
            scopedForCustomer.FirstOrDefault()?.CustomerName,
            scopedForCustomer.FirstOrDefault()?.SalesUserId,
            currentUserId,
            cancellationToken);

        return new FinanceReceivableStatementDetailDto
        {
            Customer = customerDto,
            Statement = header,
            Lines = lines
        };
    }

    private async Task<IQueryable<FinanceReceivable>> BuildScopedReceivablesAsync(
        string? currentUserId,
        CancellationToken cancellationToken)
    {
        var q = _db.FinanceReceivables.AsNoTracking().Where(r => !r.IsDeleted);
        return await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            currentUserId,
            q,
            _db.SellOrders.AsNoTracking(),
            cancellationToken);
    }

    private async Task<FinanceReceivableStatementCustomerDto> BuildCustomerDtoAsync(
        string customerId,
        CRM.Core.Models.Customer.CustomerInfo? customer,
        string? snapshotName,
        string? snapshotSalesUserId,
        string? currentUserId,
        CancellationToken cancellationToken)
    {
        var canViewFull = false;
        if (customer != null && !string.IsNullOrWhiteSpace(currentUserId))
        {
            var summary = await _rbac.GetUserPermissionSummaryAsync(currentUserId.Trim());
            var hasRead = summary.IsSysAdmin
                || summary.PermissionCodes.Any(c =>
                    string.Equals(c, "customer.read", StringComparison.OrdinalIgnoreCase));
            canViewFull = hasRead && await _dataPermission.CanAccessCustomerAsync(currentUserId.Trim(), customer);
        }

        string? salesUserId = !string.IsNullOrWhiteSpace(customer?.SalesUserId)
            ? customer.SalesUserId.Trim()
            : snapshotSalesUserId?.Trim();
        var salesMap = await LoadSalesNamesAsync(
            string.IsNullOrWhiteSpace(salesUserId) ? [] : [salesUserId],
            cancellationToken);
        salesMap.TryGetValue(salesUserId ?? string.Empty, out var salesName);

        if (!canViewFull)
        {
            return new FinanceReceivableStatementCustomerDto
            {
                CustomerId = customerId,
                CustomerCode = customer?.CustomerCode,
                CustomerName = snapshotName,
                SalesUserId = salesUserId,
                SalesUserName = salesName,
                CanViewFull = false
            };
        }

        var contact = await _db.CustomerContacts.AsNoTracking()
            .Where(c => c.CustomerId == customerId && !c.IsDeleted && c.IsMain)
            .OrderBy(c => c.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);

        var contactName = contact == null
            ? null
            : (!string.IsNullOrWhiteSpace(contact.CName) ? contact.CName : contact.EName);
        var phone = contact == null
            ? null
            : (!string.IsNullOrWhiteSpace(contact.Tel) ? contact.Tel : contact.Mobile);

        return new FinanceReceivableStatementCustomerDto
        {
            CustomerId = customerId,
            CustomerCode = customer!.CustomerCode,
            CustomerName = string.IsNullOrWhiteSpace(customer.OfficialName)
                ? snapshotName
                : customer.OfficialName.Trim(),
            CustomerEnglishName = string.IsNullOrWhiteSpace(customer.EnglishOfficialName)
                ? null
                : customer.EnglishOfficialName.Trim(),
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PaymentDays = customer.Payment,
            CreditLimit = customer.CreditLine,
            SalesUserId = salesUserId,
            SalesUserName = salesName,
            CanViewFull = true
        };
    }

    private async Task<Dictionary<string, string>> LoadSalesNamesAsync(
        IReadOnlyList<string> userIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (userIds.Count == 0)
            return map;

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        foreach (var u in users)
        {
            if (string.IsNullOrWhiteSpace(u.Id))
                continue;
            map[u.Id.Trim()] = EntityLookupService.FormatUserLoginName(u) ?? u.Id.Trim();
        }

        return map;
    }

    private static bool ContainsIgnoreCase(string? value, string keyword) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
