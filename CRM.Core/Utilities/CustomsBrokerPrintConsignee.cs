using CRM.Core.Models.Customs;

namespace CRM.Core.Utilities;

/// <summary>
/// 报关装箱单打印收货人：现查 <see cref="CustomsBroker"/>，不读销售客户。
/// </summary>
public static class CustomsBrokerPrintConsignee
{
    public const string MissingBrokerForPrintMessage = "报关装箱单缺少报关公司，无法打印。";

    public const string IncompleteForPrintMessage =
        "报关公司联系人、电话或地址未维护，无法打印报关装箱单。请先在「报关公司」中补全装箱单收货人资料。";

    public static string ResolvePrintName(CustomsBroker broker)
    {
        if (!string.IsNullOrWhiteSpace(broker.Ename))
            return broker.Ename.Trim();
        return (broker.Cname ?? string.Empty).Trim();
    }

    public static void EnsurePrintReady(CustomsBroker? broker)
    {
        if (broker == null || broker.IsDeleted)
            throw new InvalidOperationException(MissingBrokerForPrintMessage);
        if (string.IsNullOrWhiteSpace(broker.Cname))
            throw new InvalidOperationException(IncompleteForPrintMessage);
        if (string.IsNullOrWhiteSpace(broker.ContactName)
            || string.IsNullOrWhiteSpace(broker.Tel)
            || string.IsNullOrWhiteSpace(broker.Address))
            throw new InvalidOperationException(IncompleteForPrintMessage);
    }

    /// <summary>名称 / 地址 / 联系人 / 电话（无 Attn/Tel 前缀）。</summary>
    public static IReadOnlyList<string> BuildAddressLines(CustomsBroker broker)
    {
        EnsurePrintReady(broker);
        return new[]
        {
            ResolvePrintName(broker),
            broker.Address!.Trim(),
            broker.ContactName!.Trim(),
            broker.Tel!.Trim()
        };
    }

    public static string? PrintEmail(CustomsBroker broker) =>
        string.IsNullOrWhiteSpace(broker.Email) ? null : broker.Email.Trim();
}
