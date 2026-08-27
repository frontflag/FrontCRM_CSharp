using CRM.Core.Constants;
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

        var q = await FinanceReceiptListFilter.BuildFilteredQueryAsync(
            _db, _dataPermissionService, request, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            var receiptIds = items.Select(r => r.Id).ToList();
            var itemAgg = await _db.FinanceReceiptItems.AsNoTracking()
                .Where(i => receiptIds.Contains(i.FinanceReceiptId))
                .GroupBy(i => i.FinanceReceiptId)
                .Select(g => new
                {
                    ReceiptId = g.Key,
                    Purpose = g.Max(i => i.ReceiptPurpose),
                    MinStatus = g.Min(i => i.VerificationStatus),
                    MaxStatus = g.Max(i => i.VerificationStatus)
                })
                .ToListAsync(cancellationToken);

            var purposeMap = itemAgg.ToDictionary(
                x => x.ReceiptId,
                x => x.Purpose,
                StringComparer.OrdinalIgnoreCase);
            var statusMap = itemAgg.ToDictionary(
                x => x.ReceiptId,
                x => FinanceReceiptListFilter.ResolveHeaderVerificationStatus(x.MinStatus, x.MaxStatus),
                StringComparer.OrdinalIgnoreCase);

            foreach (var receipt in items)
            {
                if (purposeMap.TryGetValue(receipt.Id, out var purpose))
                    receipt.ReceiptPurpose = purpose;
                receipt.VerificationStatus = statusMap.TryGetValue(receipt.Id, out var vs)
                    ? vs
                    : FinanceVerificationStatusCode.Pending;
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
