using System.Globalization;

namespace CRM.Core.Models.Sales;

/// <summary>
/// 销售订单主表「订单信息」：结构化列 + <c>comment</c>（自由备注）；历史多行前缀格式解析与列表展示拼接。
/// </summary>
public static class SellOrderHeaderRemarkCodec
{
    /// <summary>解析历史多行文本（曾写入主表 <c>comment</c>）得到的结构化片段 + 自由段。</summary>
    public readonly struct HeaderRemarkBlocks
    {
        public string? ProductKind { get; init; }
        public string? CustomerContactName { get; init; }
        public string? InvoiceInfo { get; init; }
        public string? PaymentTermsText { get; init; }

        /// <summary>无法匹配前缀的行合并为自由备注段。</summary>
        public string? LooseRemark { get; init; }
    }

    private static readonly string[][] ProductPrefixes =
    [
        ["产品：", "产品:"],
        ["Product：", "Product:"]
    ];

    private static readonly string[][] ContactPrefixes =
    [
        ["客户联系人：", "客户联系人:"],
        ["Contact：", "Contact:"]
    ];

    private static readonly string[][] InvoicePrefixes =
    [
        ["发票信息：", "发票信息:"],
        ["Invoice：", "Invoice:"]
    ];

    private static readonly string[][] TermsPrefixes =
    [
        ["账期：", "账期:"],
        ["Terms：", "Terms:"]
    ];

    /// <summary>是否仍为历史「多行前缀 + 自由段」整块备注（用于解析迁移，避免把普通一句话误判为 legacy）。</summary>
    public static bool LooksLikeLegacyHeaderBlob(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        foreach (var p in new[] { "产品：", "产品:", "Product：", "Product:" })
        {
            if (s.StartsWith(p, StringComparison.Ordinal))
                return true;
        }

        return s.Contains("发票信息：", StringComparison.Ordinal)
               || s.Contains("发票信息:", StringComparison.Ordinal)
               || s.Contains("Invoice：", StringComparison.Ordinal)
               || s.Contains("Invoice:", StringComparison.Ordinal);
    }

    /// <summary>解析历史主表整块备注（多行、前缀行 + 自由备注）。</summary>
    public static HeaderRemarkBlocks ParseLegacyComment(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return default;

        var normalized = raw.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        string? product = null, contact = null, invoice = null, terms = null;
        var loose = new List<string>();
        foreach (var line in normalized.Split('\n', StringSplitOptions.None))
        {
            var t = line.Trim();
            if (t.Length == 0)
                continue;

            if (TryStrip(t, ProductPrefixes, out var pv))
                product = pv;
            else if (TryStrip(t, ContactPrefixes, out var cv))
                contact = cv;
            else if (TryStrip(t, InvoicePrefixes, out var iv))
                invoice = iv;
            else if (TryStrip(t, TermsPrefixes, out var tv))
                terms = tv;
            else
                loose.Add(t);
        }

        var remark = string.Join('\n', loose).Trim();
        return new HeaderRemarkBlocks
        {
            ProductKind = NullIfEmpty(product),
            CustomerContactName = NullIfEmpty(contact),
            InvoiceInfo = NullIfEmpty(invoice),
            PaymentTermsText = NullIfEmpty(terms),
            LooseRemark = NullIfEmpty(remark)
        };
    }

    private static bool TryStrip(string line, string[][] prefixGroups, out string value)
    {
        foreach (var group in prefixGroups)
        {
            foreach (var p in group)
            {
                if (line.StartsWith(p, false, CultureInfo.InvariantCulture))
                {
                    value = line[p.Length..].Trim();
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>是否已有任一结构化列（不含 <c>comment</c> 自由备注）。</summary>
    public static bool HasAnyStructuredColumn(SellOrder order)
    {
        if (order == null) return false;
        return !string.IsNullOrWhiteSpace(order.ProductKind)
               || !string.IsNullOrWhiteSpace(order.CustomerContactName)
               || !string.IsNullOrWhiteSpace(order.InvoiceInfo)
               || !string.IsNullOrWhiteSpace(order.PaymentTermsText);
    }

    /// <summary>列表/报表：结构化列按中文标签拼接，末尾追加物理 <c>comment</c> 自由备注（若有）。</summary>
    public static string BuildDisplayComment(SellOrder order)
    {
        if (order == null) return string.Empty;

        if (!HasAnyStructuredColumn(order))
            return (order.Comment ?? string.Empty).Trim();

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(order.ProductKind))
            lines.Add($"产品：{order.ProductKind.Trim()}");
        if (!string.IsNullOrWhiteSpace(order.CustomerContactName))
            lines.Add($"客户联系人：{order.CustomerContactName.Trim()}");
        if (!string.IsNullOrWhiteSpace(order.InvoiceInfo))
            lines.Add($"发票信息：{order.InvoiceInfo.Trim()}");
        if (!string.IsNullOrWhiteSpace(order.PaymentTermsText))
            lines.Add($"账期：{order.PaymentTermsText.Trim()}");
        if (!string.IsNullOrWhiteSpace(order.Comment))
        {
            if (lines.Count > 0)
                lines.Add(string.Empty);
            lines.Add(order.Comment.Trim());
        }

        return string.Join('\n', lines).Trim();
    }

    /// <summary>
    /// 读路径：若 <c>comment</c> 仍为历史整块且结构化列为空，则解析写入结构化列并将 <c>comment</c> 缩为自由段。
    /// </summary>
    public static bool TryMaterializeFromLegacyComment(SellOrder order)
    {
        if (order == null) return false;
        if (HasAnyStructuredColumn(order)) return false;
        if (string.IsNullOrWhiteSpace(order.Comment)) return false;
        if (!LooksLikeLegacyHeaderBlob(order.Comment)) return false;

        var b = ParseLegacyComment(order.Comment);
        MergeNonNullFromBlocks(order, b);
        order.Comment = NullIfEmpty(b.LooseRemark);
        return true;
    }

    /// <summary>
    /// Debug/运维：将仍为 legacy 格式的 <c>comment</c> 拆入结构化列（仅填空）；自由段写回 <c>comment</c>；非 legacy 不修改。
    /// </summary>
    public static bool TrySplitCommentOntoStructuredColumns(SellOrder order)
    {
        if (order == null || string.IsNullOrWhiteSpace(order.Comment)) return false;
        if (!LooksLikeLegacyHeaderBlob(order.Comment)) return false;

        const int maxProduct = 64;
        const int maxContact = 200;
        const int maxInv = 500;
        const int maxTerms = 500;
        const int maxRemark = 500;

        static string? FillEmpty(string? current, string? src, int maxLen)
        {
            if (!string.IsNullOrWhiteSpace(current)) return current;
            if (string.IsNullOrWhiteSpace(src)) return current;
            var t = src.Trim();
            return t.Length > maxLen ? t[..maxLen] : t;
        }

        var rawFull = order.Comment.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var b = ParseLegacyComment(order.Comment);

        order.ProductKind = FillEmpty(order.ProductKind, b.ProductKind, maxProduct);
        order.CustomerContactName = FillEmpty(order.CustomerContactName, b.CustomerContactName, maxContact);
        order.InvoiceInfo = FillEmpty(order.InvoiceInfo, b.InvoiceInfo, maxInv);
        order.PaymentTermsText = FillEmpty(order.PaymentTermsText, b.PaymentTermsText, maxTerms);

        var anyStructuredParsed = !string.IsNullOrWhiteSpace(b.ProductKind)
                                  || !string.IsNullOrWhiteSpace(b.CustomerContactName)
                                  || !string.IsNullOrWhiteSpace(b.InvoiceInfo)
                                  || !string.IsNullOrWhiteSpace(b.PaymentTermsText);

        // 拆完后物理 comment 仅存自由段：有解析出的 loose 用之；仅有结构化前缀无正文则清空；否则不应到达（legacy 应至少拆出一类）
        string? newFree;
        if (!string.IsNullOrWhiteSpace(b.LooseRemark))
            newFree = FillEmpty(null, b.LooseRemark, maxRemark);
        else if (!anyStructuredParsed)
            newFree = rawFull.Length > maxRemark ? rawFull[..maxRemark] : rawFull;
        else
            newFree = null;

        order.Comment = newFree;
        return true;
    }

    /// <summary>将解析块写入实体（非 null 即覆盖对应列；自由段写入 <c>comment</c>）。</summary>
    public static void MergeNonNullFromBlocks(SellOrder order, HeaderRemarkBlocks blocks)
    {
        if (blocks.ProductKind != null) order.ProductKind = blocks.ProductKind;
        if (blocks.CustomerContactName != null) order.CustomerContactName = blocks.CustomerContactName;
        if (blocks.InvoiceInfo != null) order.InvoiceInfo = blocks.InvoiceInfo;
        if (blocks.PaymentTermsText != null) order.PaymentTermsText = blocks.PaymentTermsText;
        if (blocks.LooseRemark != null) order.Comment = blocks.LooseRemark;
    }

    private static string? NullIfEmpty(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }
}
