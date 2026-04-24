using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Quote;

namespace CRM.Core.Utilities;

// DTO 类型定义于 CRM.Core.Interfaces（IStockOutService / IInventoryCenterService 等文件内）。

/// <summary>
/// PRD §5.2.1：采购方向 + 主部门 <c>SaleDataScope == 4</c> 时，对 API 响应中的<strong>客户身份与销售侧金额</strong>就地脱敏。
/// 判定：非系统管理员；<c>SaleDataScope == 4</c>；且（<c>BelongsToPurchaseDept</c> 或主部门 <c>IdentityType</c> 为 2/3 采购/采购运营）。
/// 与前端 <c>useSaleSensitiveFieldMask</c> 一致。
/// </summary>
public static class SaleSensitiveFieldMask521
{
    public static bool ShouldMask(UserPermissionSummaryDto? s)
    {
        if (s == null || s.IsSysAdmin) return false;
        if (s.SaleDataScope != 4) return false;
        if (s.BelongsToPurchaseDept) return true;
        return s.IdentityType is 2 or 3;
    }

    public static void ApplyCustomerInfo(CustomerInfo? c, bool mask)
    {
        if (!mask || c == null) return;
        c.CustomerCode = string.Empty;
        c.OfficialName = null;
        c.StandardOfficialName = null;
        c.EnglishOfficialName = null;
        c.NickName = null;
        c.ExternalNumber = null;
        c.SalesUserId = null;
        c.CreditLine = 0m;
        c.CreditLineRemain = 0m;
        if (c.Contacts == null) return;
        foreach (var ct in c.Contacts)
        {
            ct.CName = null;
            ct.EName = null;
            ct.Mobile = null;
            ct.Tel = null;
            ct.Email = null;
        }
    }

    public static void ApplyCustomerInfos(IEnumerable<CustomerInfo>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var c in items)
            ApplyCustomerInfo(c, true);
    }

    public static void ApplyStockOutListItem(StockOutListItemDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.CustomerName = null;
        x.SalesUserName = null;
        x.TotalAmount = 0m;
    }

    public static void ApplyStockOutListItems(IEnumerable<StockOutListItemDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyStockOutListItem(x, true);
    }

    public static void ApplyStockOutDetailView(StockOutDetailViewDto? x, bool mask)
    {
        if (!mask || x == null) return;
        ApplyStockOutListItem(x, true);
    }

    public static void ApplyStockOutRequestListItem(StockOutRequestListItemDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.CustomerName = null;
        x.SalesUserName = null;
        x.CustomerId = string.Empty;
    }

    public static void ApplyStockOutRequestListItems(IEnumerable<StockOutRequestListItemDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyStockOutRequestListItem(x, true);
    }

    public static void ApplyStockOutItemListRow(StockOutItemListRowDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.CustomerName = null;
        x.SalesUserName = null;
    }

    public static void ApplyStockOutItemListRows(IEnumerable<StockOutItemListRowDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyStockOutItemListRow(x, true);
    }

    public static void ApplyPickingTaskListItem(PickingTaskListItemDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.CustomerName = null;
        x.SalesUserName = null;
    }

    public static void ApplyPickingTaskListItems(IEnumerable<PickingTaskListItemDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyPickingTaskListItem(x, true);
    }

    public static void ApplyPickingTaskDetailView(PickingTaskDetailViewDto? x, bool mask)
    {
        if (!mask || x == null) return;
        ApplyPickingTaskListItem(x, true);
    }

    public static void ApplyInventoryStockItemRow(InventoryStockItemRowDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.CustomerName = null;
        x.SalesPrice = null;
        x.SalesCurrency = null;
        x.SalesPriceUsd = null;
    }

    public static void ApplyInventoryStockItemRows(IEnumerable<InventoryStockItemRowDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyInventoryStockItemRow(x, true);
    }

    public static void ApplyInventoryStockItemListRow(InventoryStockItemListRowDto? x, bool mask)
    {
        if (!mask || x == null) return;
        ApplyInventoryStockItemRow(x, true);
        x.SalespersonName = null;
        x.ProfitOutBizUsd = 0m;
    }

    public static void ApplyInventoryStockItemListRows(IEnumerable<InventoryStockItemListRowDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyInventoryStockItemListRow(x, true);
    }

    public static void ApplyInventoryMaterialOverview(InventoryMaterialOverviewDto? x, bool mask)
    {
        if (!mask || x == null) return;
        x.InventoryAmount = 0m;
    }

    public static void ApplyInventoryMaterialOverviews(IEnumerable<InventoryMaterialOverviewDto>? rows, bool mask)
    {
        if (!mask || rows == null) return;
        foreach (var x in rows)
            ApplyInventoryMaterialOverview(x, true);
    }

    /// <summary>订单旅程图中销售侧金额节点（与列表/详情口径一致）。</summary>
    public static void ApplySalesOrderJourney(SalesOrderJourneyResponseDto? dto, bool mask)
    {
        if (!mask || dto?.Nodes == null) return;
        foreach (var n in dto.Nodes)
        {
            if (n.Type is "SALES_ORDER" or "STOCK_OUT" or "FINANCE_RECEIPT" or "SELL_INVOICE")
                n.Amount = null;
        }
    }

    /// <summary>报价主表：隐藏客户与业务员身份（与 RFQ 侧销售数据禁止一致）。</summary>
    public static void ApplyQuote(Quote? q, bool mask)
    {
        if (!mask || q == null) return;
        q.CustomerId = null;
        q.SalesUserId = null;
        q.SalesUserName = null;
    }

    public static void ApplyQuotes(IEnumerable<Quote>? quotes, bool mask)
    {
        if (!mask || quotes == null) return;
        foreach (var q in quotes)
            ApplyQuote(q, true);
    }

    /// <summary>收款单：客户身份与收款金额、关联销售单键、明细金额。</summary>
    public static void ApplyFinanceReceipt(FinanceReceipt? r, bool mask)
    {
        if (!mask || r == null) return;
        r.CustomerId = string.Empty;
        r.CustomerName = null;
        r.SalesUserId = null;
        r.ReceiptAmount = 0m;
        if (r.Items == null) return;
        foreach (var it in r.Items)
        {
            it.ReceiptAmount = 0m;
            it.ReceiptConvertAmount = 0m;
            it.VerifiedAmount = 0m;
            it.SellOrderId = null;
            it.SellOrderItemId = null;
        }
    }

    public static void ApplyFinanceReceipts(IEnumerable<FinanceReceipt>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var r in items)
            ApplyFinanceReceipt(r, true);
    }

    /// <summary>销项发票：客户身份与价税金额、明细单价与行金额。</summary>
    public static void ApplyFinanceSellInvoice(FinanceSellInvoice? inv, bool mask)
    {
        if (!mask || inv == null) return;
        inv.CustomerId = string.Empty;
        inv.CustomerName = null;
        inv.InvoiceTotal = 0m;
        inv.ReceiveDone = 0m;
        inv.ReceiveToBe = 0m;
        if (inv.Items == null) return;
        foreach (var it in inv.Items)
        {
            it.InvoiceTotal = 0m;
            it.ValueAddedTax = 0m;
            it.TaxFreeTotal = 0m;
            it.Price = 0m;
        }
    }

    public static void ApplyFinanceSellInvoices(IEnumerable<FinanceSellInvoice>? items, bool mask)
    {
        if (!mask || items == null) return;
        foreach (var inv in items)
            ApplyFinanceSellInvoice(inv, true);
    }
}
