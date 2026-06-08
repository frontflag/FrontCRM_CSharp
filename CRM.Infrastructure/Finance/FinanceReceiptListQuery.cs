using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>收款单主表列表：EF 侧 <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class FinanceReceiptListQuery : IFinanceReceiptListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinanceReceiptListQuery(ApplicationDbContext db, IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceipt>> GetPagedAsync(
        FinanceReceiptQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceReceipts.AsNoTracking();
        q = await _dataPermissionService.ApplyFinanceReceiptListDataScopeAsync(
            request.CurrentUserId,
            q,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.FinanceReceiptCode.ToLower().Contains(k) ||
                (r.CustomerName != null && r.CustomerName.ToLower().Contains(k)));
        }

        if (request.Status.HasValue)
            q = q.Where(r => r.Status == request.Status.Value);

        if (request.StartDate.HasValue)
            q = q.Where(r => r.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(r => r.CreateTime <= request.EndDate.Value.AddDays(1));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var receiptIds = items.Select(r => r.Id).ToList();
            var purposeMap = await _db.FinanceReceiptItems.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(i => receiptIds.Contains(i.FinanceReceiptId))
                .GroupBy(i => i.FinanceReceiptId)
                .Select(g => new { ReceiptId = g.Key, Purpose = g.Max(i => i.ReceiptPurpose) })
                .ToDictionaryAsync(x => x.ReceiptId, x => x.Purpose, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var receipt in items)
            {
                if (purposeMap.TryGetValue(receipt.Id, out var purpose))
                    receipt.ReceiptPurpose = purpose;
            }
        }

        return new PagedResult<FinanceReceipt>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
