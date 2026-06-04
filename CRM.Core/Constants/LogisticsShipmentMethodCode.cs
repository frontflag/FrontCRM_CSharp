using CRM.Core.Models.Inventory;

namespace CRM.Core.Constants;

/// <summary>出货场景 <c>LogisticsArrivalMethod</c> 有效 ItemCode（不含「物流/4」）。</summary>
public static class LogisticsShipmentMethodCode
{
    public const string Delivery = "1";
    public const string SelfPickup = "2";
    public const string Express = "3";

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        Delivery, SelfPickup, Express
    };

    public static string? Normalize(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    public static string? NormalizeExpressCompany(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim();

    public static void EnsureRequired(string? code, string paramName = "ShipmentMethod")
    {
        var n = Normalize(code);
        if (string.IsNullOrEmpty(n))
            throw new ArgumentException("请选择出货方式", paramName);
        if (!Allowed.Contains(n))
            throw new ArgumentException("出货方式无效（仅支持送货、自提、快递）", paramName);
    }

    /// <summary>批量装箱前：各出库通知须已填且出货方式、快递公司一致。</summary>
    public static void EnsureStockOutRequestsConsistentForPacking(IReadOnlyList<StockOutRequest> requests)
    {
        if (requests == null || requests.Count == 0)
            return;

        if (requests.Any(r => string.IsNullOrWhiteSpace(r.ShipmentMethod)))
            throw new InvalidOperationException("所选出库通知缺少出货方式，无法生成装箱单");

        var methods = requests
            .Select(r => Normalize(r.ShipmentMethod)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (methods.Count > 1)
            throw new InvalidOperationException("所选出库通知的出货方式必须一致，请分批生成装箱单");
        if (methods.Any(m => !Allowed.Contains(m)))
            throw new InvalidOperationException("所选出库通知的出货方式无效，无法生成装箱单");

        var expressValues = requests
            .Select(r => NormalizeExpressCompany(r.ExpressCompany) ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (expressValues.Count > 1)
            throw new InvalidOperationException("所选出库通知的快递公司必须一致，请分批生成装箱单");
    }

    /// <summary>历史 <see cref="PackingDeliveryMethodCode"/> → 字典 ItemCode。</summary>
    public static string? MapLegacyDeliveryMethod(short? deliveryMethod) => deliveryMethod switch
    {
        PackingDeliveryMethodCode.Delivery => Delivery,
        PackingDeliveryMethodCode.SelfPickup => SelfPickup,
        _ => null
    };
}
