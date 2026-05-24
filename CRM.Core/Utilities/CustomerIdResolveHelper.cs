using CRM.Core.Models.Customer;
using CRM.Core.Models.Sales;

namespace CRM.Core.Utilities;

/// <summary>从出库通知 / 销售订单 / 客户名称解析 <c>CustomerId</c>。</summary>
public static class CustomerIdResolveHelper
{
    public static Dictionary<string, string> BuildDisplayNameIndex(IEnumerable<CustomerInfo> customers)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in customers)
        {
            var id = c.Id?.Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            void TryAdd(string? name)
            {
                var n = name?.Trim();
                if (string.IsNullOrEmpty(n))
                    return;
                dict.TryAdd(n, id);
            }

            TryAdd(c.OfficialName);
            TryAdd(c.StandardOfficialName);
        }

        return dict;
    }

    public static string? ResolveForStockOutNotify(
        string? notifyCustomerId,
        SellOrder? sellOrder,
        IReadOnlyDictionary<string, string>? customerIdByDisplayName = null)
    {
        var cid = notifyCustomerId?.Trim();
        if (!string.IsNullOrEmpty(cid))
            return cid;

        cid = sellOrder?.CustomerId?.Trim();
        if (!string.IsNullOrEmpty(cid))
            return cid;

        var name = sellOrder?.CustomerName?.Trim();
        if (string.IsNullOrEmpty(name) || customerIdByDisplayName == null)
            return null;

        return customerIdByDisplayName.TryGetValue(name, out var resolved) ? resolved : null;
    }
}
