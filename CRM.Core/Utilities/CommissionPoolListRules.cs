namespace CRM.Core.Utilities;

/// <summary>提成池只读列表的分页与筛选边界。</summary>
public static class CommissionPoolListRules
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public static (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (page, pageSize);
    }

    public static bool IsSalesStatus(short? value) => value is null or 0 or 1;

    public static bool IsPurchaseStatus(short? value) => value is null or 0 or 1 or 2;

    /// <summary>收款核销：0 待核销 / 1 部分核销 / 2 核销完成（出库行对应应收）。</summary>
    public static bool IsReceiptStatus(short? value) => value is null or 0 or 1 or 2;

    public static string? NormalizeKeyword(string? keyword)
    {
        var t = keyword?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    public static string? NormalizeUserId(string? userId) => NormalizeKeyword(userId);

    /// <summary>列表展示登录账号（<c>user.UserName</c>），不用中文姓名。</summary>
    public static string DisplayAccount(string? userName, string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userName)) return userName.Trim();
        return string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
    }

    public static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return null;
    }

    /// <summary>列表展示优先明细单号，没有则回退单据号。</summary>
    public static string DisplayDocCode(string? itemCode, string? headerCode) =>
        FirstNonEmpty(itemCode, headerCode) ?? string.Empty;

    public static bool ContainsKeyword(string? keyword, params string?[] values)
    {
        var kw = NormalizeKeyword(keyword);
        if (kw == null) return true;
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && value.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>出库数量：扩展出库量 &gt; 0 用扩展，否则用明细 Quantity。</summary>
    public static int? ResolveQty(bool hasExtend, int extQty, bool hasItem, int itemQty)
    {
        if (hasExtend && extQty > 0) return extQty;
        if (hasItem) return itemQty;
        if (hasExtend) return extQty;
        return null;
    }

    public static decimal? FirstDecimal(params decimal?[] values)
    {
        foreach (var v in values)
        {
            if (v.HasValue) return v;
        }
        return null;
    }

    /// <summary>列表物料/单价：出库明细+扩展快照，销售原币/USD 空时回退销售明细。</summary>
    public static CommissionPoolLineFacts ResolveLineFacts(
        string? itemPn,
        string? itemBrand,
        bool hasItem,
        int itemQty,
        bool hasExtend,
        int extQty,
        decimal extPurchasePrice,
        short extPurchaseCurrency,
        decimal extPurchasePriceUsd,
        decimal? extSalesPrice,
        short? extSalesCurrency,
        decimal? extSalesPriceUsd,
        string? sellPn,
        string? sellBrand,
        decimal? sellPrice,
        short? sellCurrency,
        decimal? sellConvertPrice)
    {
        return new CommissionPoolLineFacts(
            FirstNonEmpty(itemPn, sellPn),
            FirstNonEmpty(itemBrand, sellBrand),
            hasExtend ? extPurchasePrice : null,
            hasExtend ? extPurchaseCurrency : null,
            hasExtend ? extPurchasePriceUsd : null,
            FirstDecimal(extSalesPrice, sellPrice),
            extSalesPrice.HasValue ? extSalesCurrency : sellCurrency,
            FirstDecimal(extSalesPriceUsd, sellConvertPrice),
            ResolveQty(hasExtend, extQty, hasItem, itemQty));
    }
}

public readonly record struct CommissionPoolLineFacts(
    string? PurchasePn,
    string? PurchaseBrand,
    decimal? PurchasePrice,
    short? PurchaseCurrency,
    decimal? PurchasePriceUsd,
    decimal? SalesPrice,
    short? SalesCurrency,
    decimal? SalesPriceUsd,
    int? QtyStockOut);
