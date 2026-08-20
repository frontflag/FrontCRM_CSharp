using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class FinanceFreightForwarderPayableListQuery : IFinanceFreightForwarderPayableListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinanceFreightForwarderPayableListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    public async Task<PagedResult<FinanceFreightForwarderPayableListItem>> GetPagedAsync(
        FinanceFreightForwarderPayableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceReceipts.AsNoTracking()
            .Where(r => r.IsFreightForwarderPayment)
            .Where(r => r.Status == FinanceReceiptStatusCode.Confirmed
                        || r.Status == FinanceReceiptStatusCode.LegacyApproved);

        q = await _dataPermissionService.ApplyFinanceReceiptListDataScopeAsync(
            request.CurrentUserId, q, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.FinanceReceiptCode.ToLower().Contains(k)
                || (r.CustomerName != null && r.CustomerName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            q = q.Where(r => r.CustomerId == request.CustomerId.Trim());

        if (!string.IsNullOrWhiteSpace(request.FreightForwarderCompanyId))
            q = q.Where(r => r.FreightForwarderCompanyId == request.FreightForwarderCompanyId.Trim());

        var receipts = await q
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync(cancellationToken);

        if (receipts.Count == 0)
        {
            return new PagedResult<FinanceFreightForwarderPayableListItem>
            {
                Items = Array.Empty<FinanceFreightForwarderPayableListItem>(),
                TotalCount = 0,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var paidMap = await _db.FinanceFreightForwarderPayments.AsNoTracking()
            .Where(p => receiptIds.Contains(p.FinanceReceiptId))
            .GroupBy(p => p.FinanceReceiptId)
            .Select(g => new { ReceiptId = g.Key, Paid = g.Sum(x => x.PaymentAmount) })
            .ToDictionaryAsync(x => x.ReceiptId, x => x.Paid, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var companyIds = receipts
            .Select(r => r.FreightForwarderCompanyId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var companyMap = companyIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.FreightForwarderCompanies.AsNoTracking()
                .Where(c => companyIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Cname, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var items = receipts.Select(r =>
        {
            paidMap.TryGetValue(r.Id, out var paid);
            var pending = FinanceFreightForwarderPayableStatusHelper.PendingAmount(r.ReceiptAmount, paid);
            var status = FinanceFreightForwarderPayableStatusHelper.Compute(r.ReceiptAmount, paid);
            string? companyName = null;
            if (!string.IsNullOrWhiteSpace(r.FreightForwarderCompanyId)
                && companyMap.TryGetValue(r.FreightForwarderCompanyId, out var cn))
                companyName = cn;

            return new FinanceFreightForwarderPayableListItem
            {
                ReceiptId = r.Id,
                FinanceReceiptCode = r.FinanceReceiptCode,
                ReceiptStatus = r.Status,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                FreightForwarderCompanyId = r.FreightForwarderCompanyId,
                FreightForwarderCompanyName = companyName,
                ReceiptAmount = r.ReceiptAmount,
                PaidAmount = paid,
                PendingAmount = pending,
                ReceiptCurrency = r.ReceiptCurrency,
                PayableStatus = status,
                ReceiptDate = r.ReceiptDate,
                CreateTime = r.CreateTime
            };
        }).ToList();

        if (request.PayableStatus.HasValue)
            items = items.Where(i => i.PayableStatus == request.PayableStatus.Value).ToList();

        var total = items.Count;
        var pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var nameMap = await FinanceCustomerDisplayEnrichment.LoadMapAsync(
            _db, pageItems.Select(x => x.CustomerId), cancellationToken);
        foreach (var row in pageItems)
        {
            if (!nameMap.TryGetValue(row.CustomerId, out var names)) continue;
            if (!string.IsNullOrWhiteSpace(names.Zh)) row.CustomerName = names.Zh;
            row.CustomerEnglishName = names.En;
        }

        return new PagedResult<FinanceFreightForwarderPayableListItem>
        {
            Items = pageItems,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
