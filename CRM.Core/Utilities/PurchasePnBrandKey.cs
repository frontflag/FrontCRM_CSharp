namespace CRM.Core.Utilities;

/// <summary>采购物料型号 + 品牌比对键（去首尾空白，不区分大小写）。</summary>
public static class PurchasePnBrandKey
{
    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    public static string Combine(string? pn, string? brand)
    {
        var p = Normalize(pn);
        var b = Normalize(brand);
        if (p.Length == 0 || b.Length == 0)
            return string.Empty;
        return p + "\u001f" + b;
    }
}
