using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Utilities;

/// <summary>
/// PRD §5.1.1：销售方向 + 主部门 <c>PurchaseDataScope == 4</c> 时，对 API 响应中的<strong>采购执行链路</strong>敏感字段就地脱敏（入库/库存采购价、采购单金额、供应商身份与编码、进项/付款对方等）。
/// RFQ <c>QuoteItem</c>：销售员可继续看<strong>询价单价</strong>，但<strong>不返回</strong>报价行上的供应商标识（名称/编码/ID/联系人），与「采购执行侧可见供应商」区分。
/// 判定口径与前端 <c>usePurchaseSensitiveFieldMask</c> 一致。
/// </summary>
public static class PurchaseSensitiveFieldMask511
{
    public static bool ShouldMask(UserPermissionSummaryDto? s)
    {
        if (s == null || s.IsSysAdmin) return false;
        if (s.PurchaseDataScope != 4) return false;
        if (s.BelongsToPurchaseDept) return false;
        var it = s.IdentityType;
        if (it is 4 or 5 or 6) return false;
        if (it is 2 or 3) return false;
        if (it == 1) return true;
        if (it == 0 && HasSalesOrderPermission(s.PermissionCodes)) return true;
        return false;
    }

    private static bool HasSalesOrderPermission(IReadOnlyList<string>? codes)
    {
        if (codes == null || codes.Count == 0) return false;
        foreach (var c in codes)
        {
            var x = (c ?? string.Empty).Trim().ToLowerInvariant();
            if (x is "sales-order.read" or "sales-order.write") return true;
        }

        return false;
    }

    public static void ApplyStockInListItems(IEnumerable<StockInListItemDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
        {
            x.VendorName = null;
            x.VendorId = null;
            x.TotalAmount = 0m;
            x.CurrencyCode = null;
        }
    }

    public static void ApplyStockIn(StockIn? stockIn, bool mask)
    {
        if (!mask || stockIn == null) return;
        stockIn.VendorId = null;
        stockIn.DetailVendorName = null;
        stockIn.TotalAmount = 0m;
        if (stockIn.Items == null) return;
        foreach (var it in stockIn.Items)
        {
            it.Price = 0m;
            it.Amount = 0m;
            it.Currency = null;
        }
    }

    public static void ApplyStockInNotifies(IEnumerable<StockInNotify>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var n in rows)
        {
            n.VendorName = null;
            n.VendorCode = null;
            n.VendorId = null;
            n.Cost = 0m;
            n.ExpectTotal = 0m;
            n.ReceiveTotal = 0m;
        }
    }

    public static void ApplyQcInfos(IEnumerable<QCInfo>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var q in rows)
            q.VendorName = null;
    }

    public static void ApplyInventoryStockItemRows(IEnumerable<InventoryStockItemRowDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
        {
            x.VendorName = null;
            x.PurchasePrice = 0m;
            x.PurchasePriceUsd = 0m;
        }
    }

    public static void ApplyInventoryStockItemListRows(IEnumerable<InventoryStockItemListRowDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
        {
            x.VendorName = null;
            x.PurchasePrice = 0m;
            x.PurchasePriceUsd = 0m;
            x.ProfitOutBizUsd = 0m;
        }
    }

    public static void ApplyInventoryMaterialTraces(IEnumerable<InventoryMaterialTraceDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            x.UnitPrice = 0m;
    }

    public static void ApplyInventoryFinanceSummary(InventoryFinanceSummaryDto? dto, bool mask)
    {
        if (!mask || dto == null) return;
        dto.InventoryCapital = 0m;
        dto.MonthlyOutCost = 0m;
        dto.AverageInventoryCapital = 0m;
        dto.TurnoverRate = 0m;
        dto.TurnoverDays = 0m;
    }

    public static void ApplyVendorInfos(IEnumerable<VendorInfo>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var v in items)
        {
            v.OfficialName = null;
            v.NickName = null;
            v.EnglishOfficialName = null;
            v.Code = string.Empty;
        }
    }

    public static void ApplyVendorInfo(VendorInfo? v, bool mask)
    {
        if (!mask || v == null) return;
        v.OfficialName = null;
        v.NickName = null;
        v.EnglishOfficialName = null;
        v.Code = string.Empty;
    }

    /// <summary>报价单行：隐藏供应商身份，保留单价/数量等业务字段。</summary>
    public static void ApplyQuoteVendorIdentityOnly(Quote? q, bool mask)
    {
        if (!mask || q?.Items == null) return;
        foreach (var it in q.Items)
            ApplyQuoteItemVendorIdentityOnly(it, mask);
    }

    public static void ApplyQuotesVendorIdentityOnly(IEnumerable<Quote>? quotes, bool mask)
    {
        if (!mask || quotes == null) return;
        foreach (var q in quotes)
            ApplyQuoteVendorIdentityOnly(q, mask);
    }

    private static void ApplyQuoteItemVendorIdentityOnly(QuoteItem? it, bool mask)
    {
        if (!mask || it == null) return;
        it.VendorName = null;
        it.VendorCode = null;
        it.VendorId = null;
        it.ContactId = null;
        it.ContactName = null;
    }

    public static void ApplyFinancePayment(FinancePayment? p, bool mask)
    {
        if (!mask || p == null) return;
        p.VendorId = string.Empty;
        p.VendorName = null;
        p.VendorCode = null;
    }

    public static void ApplyFinancePayments(IEnumerable<FinancePayment>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var p in items)
            ApplyFinancePayment(p, mask);
    }

    public static void ApplyFinancePurchaseInvoice(FinancePurchaseInvoice? inv, bool mask)
    {
        if (!mask || inv == null) return;
        inv.VendorId = string.Empty;
        inv.VendorName = null;
        inv.InvoiceAmount = 0m;
        inv.BillAmount = 0m;
        inv.TaxAmount = 0m;
        inv.ExcludTaxAmount = 0m;
        if (inv.Items == null) return;
        foreach (var it in inv.Items)
        {
            it.StockInCost = 0m;
            it.BillCost = 0m;
            it.BillAmount = 0m;
            it.TaxAmount = 0m;
            it.ExcludTaxAmount = 0m;
        }
    }

    public static void ApplyFinancePurchaseInvoices(IEnumerable<FinancePurchaseInvoice>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var inv in items)
            ApplyFinancePurchaseInvoice(inv, mask);
    }
}
