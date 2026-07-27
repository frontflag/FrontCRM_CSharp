using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed partial class FinanceReceivableListQuery : IFinanceReceivableListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceExchangeRateService _exchangeRateService;

    public FinanceReceivableListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRateService = exchangeRateService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceivable>> GetPagedAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = await BuildFilteredQueryAsync(request, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.StockOutDate ?? r.CreateTime)
            .ThenByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceReceivable>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<FinanceReceivable?> GetByIdScopedAsync(
        string id,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var rid = id.Trim();
        var q = _db.FinanceReceivables.AsNoTracking()
            .Where(r => r.Id == rid && !r.IsDeleted);
        q = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            currentUserId,
            q,
            cancellationToken);
        return await q.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerPagedAsync(
        FinanceReceivableWriteOffLedgerQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var scopedReceivables = _db.FinanceReceivables.AsNoTracking().Where(r => !r.IsDeleted);
        scopedReceivables = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            request.CurrentUserId,
            scopedReceivables,
            cancellationToken);

        var q = from w in _db.FinanceReceivableWriteOffs.AsNoTracking()
                join r in scopedReceivables on w.FinanceReceivableId equals r.Id
                join fr in _db.FinanceReceipts.AsNoTracking() on w.FinanceReceiptId equals fr.Id into frJoin
                from fr in frJoin.DefaultIfEmpty()
                select new { w, r, fr };

        if (request.SellOrderItemIds is { Count: > 0 } lineIds)
        {
            var scopedLineIds = lineIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (scopedLineIds.Count > 0)
                q = q.Where(x => x.r.SellOrderItemId != null && scopedLineIds.Contains(x.r.SellOrderItemId!));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.r.ReceivableCode != null && x.r.ReceivableCode.ToLower().Contains(k)) ||
                x.r.StockOutCode.ToLower().Contains(k) ||
                (x.r.SellOrderCode != null && x.r.SellOrderCode.ToLower().Contains(k)) ||
                (x.r.CustomerName != null && x.r.CustomerName.ToLower().Contains(k)) ||
                (x.r.PN != null && x.r.PN.ToLower().Contains(k)) ||
                (x.fr != null && x.fr.FinanceReceiptCode != null && x.fr.FinanceReceiptCode.ToLower().Contains(k)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.w.CreateTime)
            .ThenByDescending(x => x.w.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FinanceReceivableWriteOffLedgerItem
            {
                Id = x.w.Id,
                Amount = x.w.Amount,
                WriteOffSource = x.w.WriteOffSource,
                CreateTime = x.w.CreateTime,
                FinanceReceiptId = x.w.FinanceReceiptId ?? (x.fr != null ? x.fr.Id : null),
                FinanceReceiptCode = x.fr != null ? x.fr.FinanceReceiptCode : null,
                FinanceReceiptItemId = x.w.FinanceReceiptItemId,
                FinanceReceivableId = x.r.Id,
                ReceivableCode = x.r.ReceivableCode,
                StockOutId = x.r.StockOutId,
                StockOutCode = x.r.StockOutCode,
                SellOrderId = x.r.SellOrderId,
                SellOrderCode = x.r.SellOrderCode,
                CustomerId = x.r.CustomerId,
                CustomerName = x.r.CustomerName,
                PN = x.r.PN,
                Brand = x.r.Brand,
                Currency = x.r.Currency,
                OperatorUserId = x.w.OperatorUserId
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceReceivableWriteOffLedgerItem>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    private async Task<IQueryable<FinanceReceivable>> BuildFilteredQueryAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.FinanceReceivables.AsNoTracking().Where(r => !r.IsDeleted);
        q = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            request.CurrentUserId,
            q,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var cid = request.CustomerId.Trim();
            q = q.Where(r => r.CustomerId == cid);
        }

        if (request.VerificationStatus.HasValue)
            q = q.Where(r => r.VerificationStatus == request.VerificationStatus.Value);

        if (request.OnlyOpen == true)
            q = q.Where(r => r.VerifiedToBe > 0m);

        if (request.StockOutDateFrom.HasValue)
            q = q.Where(r => r.StockOutDate >= request.StockOutDateFrom.Value);

        if (request.StockOutDateTo.HasValue)
            q = q.Where(r => r.StockOutDate < request.StockOutDateTo.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                (r.ReceivableCode != null && r.ReceivableCode.ToLower().Contains(k)) ||
                r.StockOutCode.ToLower().Contains(k) ||
                (r.SellOrderCode != null && r.SellOrderCode.ToLower().Contains(k)) ||
                (r.CustomerName != null && r.CustomerName.ToLower().Contains(k)) ||
                (r.PN != null && r.PN.ToLower().Contains(k)));
        }

        return q;
    }
}
