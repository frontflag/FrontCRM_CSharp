using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

/// <inheritdoc />
public sealed class StockInCustomsContextQuery : IStockInCustomsContextQuery
{
    private readonly ApplicationDbContext _db;

    public StockInCustomsContextQuery(ApplicationDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<StockInCustomsContextDto?> LoadAsync(StockIn stockIn, CancellationToken cancellationToken = default)
    {
        if (stockIn.StockInType != StockInTypeCode.Customs)
            return null;

        var notifyIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(stockIn.SourceId))
            notifyIdSet.Add(stockIn.SourceId.Trim());

        var qcIdTrim = stockIn.QcId?.Trim() ?? string.Empty;
        var qcRows = await _db.QCInfos.AsNoTracking()
            .Where(q => !q.IsDeleted && q.StockInType == StockInTypeCode.Customs &&
                        (q.StockInId == stockIn.Id ||
                         (!string.IsNullOrEmpty(qcIdTrim) && q.Id == qcIdTrim)))
            .ToListAsync(cancellationToken);

        foreach (var qc in qcRows)
        {
            if (!string.IsNullOrWhiteSpace(qc.StockInNotifyId))
                notifyIdSet.Add(qc.StockInNotifyId.Trim());
        }

        var ctx = new StockInCustomsContextDto
        {
            QcId = string.IsNullOrWhiteSpace(stockIn.QcId) ? qcRows.FirstOrDefault()?.Id : stockIn.QcId.Trim(),
            QcCode = string.IsNullOrWhiteSpace(stockIn.QcCode) ? qcRows.FirstOrDefault()?.QcCode : stockIn.QcCode.Trim()
        };

        if (notifyIdSet.Count == 0)
            return ctx;

        var notifies = await _db.StockInNotifies.AsNoTracking()
            .Where(n => notifyIdSet.Contains(n.Id) && !n.IsDeleted)
            .ToListAsync(cancellationToken);

        var cdiIds = notifies
            .Where(n => !string.IsNullOrWhiteSpace(n.CustomsDeclarationItemId))
            .Select(n => n.CustomsDeclarationItemId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (cdiIds.Count == 0)
            return ctx;

        var cdiList = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => cdiIds.Contains(i.Id) && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        if (cdiList.Count == 0)
            return ctx;

        var decIds = cdiList.Select(i => i.DeclarationId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var decList = await _db.CustomsDeclarations.AsNoTracking()
            .Where(d => decIds.Contains(d.Id) && !d.IsDeleted)
            .ToListAsync(cancellationToken);
        var decById = decList.ToDictionary(d => d.Id.Trim(), d => d, StringComparer.OrdinalIgnoreCase);

        var brokerIds = decList.Select(d => d.CustomsBrokerId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var brokers = await _db.CustomsBrokers.AsNoTracking()
            .Where(b => brokerIds.Contains(b.Id))
            .ToListAsync(cancellationToken);
        var brokerById = brokers.ToDictionary(b => b.Id.Trim(), b => b, StringComparer.OrdinalIgnoreCase);

        var packingIds = decList
            .Where(d => !string.IsNullOrWhiteSpace(d.PackingId))
            .Select(d => d.PackingId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var packings = packingIds.Count == 0
            ? new List<Packing>()
            : await _db.Packings.AsNoTracking().Where(p => packingIds.Contains(p.Id)).ToListAsync(cancellationToken);
        var packingById = packings.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var whIds = decList
            .SelectMany(d => new[] { d.FromWarehouseId, d.ToWarehouseId })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var warehouses = whIds.Count == 0
            ? new List<WarehouseInfo>()
            : await _db.Warehouses.AsNoTracking().Where(w => whIds.Contains(w.Id)).ToListAsync(cancellationToken);
        var whById = warehouses.ToDictionary(w => w.Id.Trim(), w => w, StringComparer.OrdinalIgnoreCase);

        var sorIds = cdiList
            .SelectMany(i => new[] { i.StockOutRequestId, i.CustomsStockOutNotifyId })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sors = sorIds.Count == 0
            ? new List<StockOutRequest>()
            : await _db.StockOutRequests.AsNoTracking().Where(s => sorIds.Contains(s.Id)).ToListAsync(cancellationToken);
        var sorById = sors.ToDictionary(s => s.Id.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

        var pendlistIds = cdiList
            .Where(i => !string.IsNullOrWhiteSpace(i.CustomsPendlistId))
            .Select(i => i.CustomsPendlistId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var salesSorIds = cdiList
            .Select(i => i.StockOutRequestId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pendlists = pendlistIds.Count == 0 && salesSorIds.Count == 0
            ? new List<CustomsPendlist>()
            : await _db.CustomsPendlists.AsNoTracking()
                .Where(p => !p.IsDeleted &&
                            (pendlistIds.Contains(p.Id) || salesSorIds.Contains(p.SalesStockOutNotifyId)))
                .ToListAsync(cancellationToken);
        var pendById = pendlists.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var pendBySalesSorId = pendlists
            .GroupBy(p => p.SalesStockOutNotifyId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var transfers = decIds.Count == 0
            ? new List<StockTransfer>()
            : await _db.StockTransfers.AsNoTracking()
                .Where(t => !t.IsDeleted && decIds.Contains(t.CustomsDeclarationId))
                .ToListAsync(cancellationToken);
        var transferByDecId = transfers
            .GroupBy(t => t.CustomsDeclarationId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var qcRowsByNotifyId = notifyIdSet.Count == 0
            ? new Dictionary<string, QCInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _db.QCInfos.AsNoTracking()
                .Where(q => !q.IsDeleted && notifyIdSet.Contains(q.StockInNotifyId))
                .OrderByDescending(q => q.CreateTime)
                .ToListAsync(cancellationToken))
            .GroupBy(q => q.StockInNotifyId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var vendorIds = cdiList
            .Where(i => !string.IsNullOrWhiteSpace(i.VendorId))
            .Select(i => i.VendorId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendors = vendorIds.Count == 0
            ? new List<CRM.Core.Models.Vendor.VendorInfo>()
            : await _db.Vendors.AsNoTracking().Where(v => vendorIds.Contains(v.Id)).ToListAsync(cancellationToken);
        var venById = vendors.ToDictionary(v => v.Id.Trim(), v => v, StringComparer.OrdinalIgnoreCase);

        var customerIds = cdiList
            .Where(i => !string.IsNullOrWhiteSpace(i.CustomerId))
            .Select(i => i.CustomerId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customers = customerIds.Count == 0
            ? new List<CRM.Core.Models.Customer.CustomerInfo>()
            : await _db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToListAsync(cancellationToken);
        var custById = customers.ToDictionary(c => c.Id.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

        var notifyByCdiId = notifies
            .Where(n => !string.IsNullOrWhiteSpace(n.CustomsDeclarationItemId))
            .GroupBy(n => n.CustomsDeclarationItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var cdi in cdiList.OrderBy(i => i.DeclarationId).ThenBy(i => i.LineNo))
        {
            if (!decById.TryGetValue(cdi.DeclarationId.Trim(), out var dec))
                continue;

            brokerById.TryGetValue(dec.CustomsBrokerId.Trim(), out var broker);
            Packing? packing = null;
            if (!string.IsNullOrWhiteSpace(dec.PackingId))
                packingById.TryGetValue(dec.PackingId.Trim(), out packing);

            whById.TryGetValue(dec.FromWarehouseId.Trim(), out var fromWh);
            whById.TryGetValue(dec.ToWarehouseId.Trim(), out var toWh);

            notifyByCdiId.TryGetValue(cdi.Id.Trim(), out var notify);

            sorById.TryGetValue(cdi.StockOutRequestId.Trim(), out var salesSor);
            StockOutRequest? customsSor = null;
            if (!string.IsNullOrWhiteSpace(cdi.CustomsStockOutNotifyId))
                sorById.TryGetValue(cdi.CustomsStockOutNotifyId.Trim(), out customsSor);

            CustomsPendlist? pendlist = null;
            if (!string.IsNullOrWhiteSpace(cdi.CustomsPendlistId))
                pendById.TryGetValue(cdi.CustomsPendlistId.Trim(), out pendlist);
            else if (salesSor != null)
                pendBySalesSorId.TryGetValue(salesSor.Id.Trim(), out pendlist);

            transferByDecId.TryGetValue(dec.Id.Trim(), out var transfer);

            QCInfo? itemQc = null;
            if (notify != null && qcRowsByNotifyId.TryGetValue(notify.Id.Trim(), out var notifyQc))
                itemQc = notifyQc;
            else if (!string.IsNullOrWhiteSpace(ctx.QcId))
                itemQc = qcRows.FirstOrDefault(q => string.Equals(q.Id, ctx.QcId, StringComparison.OrdinalIgnoreCase));

            string? vendorName = null;
            if (!string.IsNullOrWhiteSpace(cdi.VendorId) && venById.TryGetValue(cdi.VendorId.Trim(), out var ven))
            {
                vendorName = !string.IsNullOrWhiteSpace(ven.OfficialName) ? ven.OfficialName.Trim()
                    : !string.IsNullOrWhiteSpace(ven.NickName) ? ven.NickName.Trim()
                    : ven.Code?.Trim();
            }

            string? customerName = null;
            if (!string.IsNullOrWhiteSpace(cdi.CustomerId) && custById.TryGetValue(cdi.CustomerId.Trim(), out var cust))
            {
                customerName = !string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.OfficialName.Trim()
                    : !string.IsNullOrWhiteSpace(cust.NickName) ? cust.NickName.Trim()
                    : cust.CustomerCode?.Trim();
            }

            ctx.Items.Add(new StockInCustomsContextItemDto
            {
                ArrivalNotifyId = notify?.Id,
                ArrivalNotifyCode = string.IsNullOrWhiteSpace(notify?.NoticeCode) ? null : notify!.NoticeCode.Trim(),
                DeclarationItemId = cdi.Id,
                LineNo = cdi.LineNo,
                DeclarationId = dec.Id,
                DeclarationCode = dec.DeclarationCode,
                CustomsBrokerId = dec.CustomsBrokerId,
                CustomsBrokerName = broker?.Cname,
                CustomsBrokerCode = broker?.BrokerCode,
                PackingId = dec.PackingId,
                PackingCode = packing?.Code,
                FromWarehouseId = dec.FromWarehouseId,
                FromWarehouseCode = fromWh?.WarehouseCode,
                ToWarehouseId = dec.ToWarehouseId,
                ToWarehouseCode = toWh?.WarehouseCode,
                SalesStockOutNotifyId = cdi.StockOutRequestId,
                SalesStockOutNotifyCode = salesSor?.RequestCode,
                CustomsStockOutNotifyId = cdi.CustomsStockOutNotifyId,
                CustomsStockOutNotifyCode = customsSor?.RequestCode,
                VendorId = cdi.VendorId,
                VendorName = vendorName,
                OriginalPurchasePrice = cdi.OriginalPurchasePrice > 0m ? cdi.OriginalPurchasePrice : null,
                TaxIncludedUnitPrice = cdi.TaxIncludedUnitPrice > 0m ? cdi.TaxIncludedUnitPrice : null,
                SellOrderItemCode = cdi.SellOrderItemCode,
                CustomerId = cdi.CustomerId,
                CustomerName = customerName,
                PurchasePn = cdi.PurchasePn,
                PurchaseBrand = cdi.PurchaseBrand,
                DeclareQty = cdi.DeclareQty > 0 ? cdi.DeclareQty : null,
                CustomsClearanceStatus = dec.CustomsClearanceStatus,
                HsCode = string.IsNullOrWhiteSpace(cdi.HsCode) ? null : cdi.HsCode.Trim(),
                DeclareUnitPrice = cdi.DeclareUnitPrice > 0m ? cdi.DeclareUnitPrice : null,
                DutyAmount = cdi.DutyAmount > 0m ? cdi.DutyAmount : null,
                VatAmount = cdi.VatAmount > 0m ? cdi.VatAmount : null,
                CustomsPaymentGoods = cdi.CustomsPaymentGoods > 0m ? cdi.CustomsPaymentGoods : null,
                CustomsAgencyFee = cdi.CustomsAgencyFee > 0m ? cdi.CustomsAgencyFee : null,
                OtherFee = cdi.OtherFee > 0m ? cdi.OtherFee : null,
                InspectionFee = cdi.InspectionFee > 0m ? cdi.InspectionFee : null,
                TotalValueTax = cdi.TotalValueTax > 0m ? cdi.TotalValueTax : null,
                DeclareDate = dec.DeclareDate,
                DeclarationTotalTaxAmount = dec.TotalTaxAmount > 0m ? dec.TotalTaxAmount : null,
                ExchangeRate = dec.ExchangeRate > 0m ? dec.ExchangeRate : null,
                Timeline = StockInCustomsTimelineBuilder.Build(
                    stockIn, cdi, dec, notify, salesSor, customsSor, pendlist, packing, transfer, itemQc)
            });
        }

        return ctx;
    }
}
