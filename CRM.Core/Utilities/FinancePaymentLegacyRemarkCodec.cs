using System.Globalization;
using CRM.Core.Models.Finance;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 历史请款：采购端曾将结构化信息拼入 <see cref="FinancePayment.Remark"/>（管道符分段）。用于一次性回填新列。
    /// </summary>
    public static class FinancePaymentLegacyRemarkCodec
    {
        private const string VendorBankPrefix = "供应商银行:";
        private const string PayerPrefix = "中转行费用承担方:";
        private const string LineRemarkPrefix = "明细备注:";

        /// <summary>判断是否为旧版「管道 + 固定前缀」打包备注（降低误伤纯文本 Remark）。</summary>
        public static bool LooksLikePackedRemark(string? remark)
        {
            if (string.IsNullOrWhiteSpace(remark))
                return false;
            return remark.Contains(VendorBankPrefix, StringComparison.Ordinal)
                   && remark.Contains("费用(", StringComparison.Ordinal);
        }

        public sealed class ParseResult
        {
            public string? RequestFreeRemark { get; init; }
            public string? FinancePaymentBankId { get; init; }
            public decimal FeeIntermediateBank { get; init; }
            public decimal FeeBankCharge { get; init; }
            public decimal FeeFreight { get; init; }
            public decimal FeeMisc { get; init; }
            public decimal FeeRounding { get; init; }
            public bool HasFeeBlock { get; init; }
            public string? FeeIntermediateBankPayer { get; init; }
            public bool HasPayer { get; init; }
            /// <summary>键：明细行备注里的型号或采购单号（OrdinalIgnoreCase）；值：行备注文本。</summary>
            public Dictionary<string, string> LineRemarkByKey { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public bool HasLineRemarkBlock { get; init; }
        }

        /// <summary>解析旧版 Remark；若非 LooksLikePackedRemark 或无法识别费用段则返回 false。</summary>
        public static bool TryParse(string? remark, out ParseResult result)
        {
            result = new ParseResult();
            if (!LooksLikePackedRemark(remark))
                return false;

            var freeParts = new List<string>();
            string? bankId = null;
            decimal f0 = 0, f1 = 0, f2 = 0, f3 = 0, f4 = 0;
            var hasFee = false;
            string? payer = null;
            var hasPayer = false;
            var lineMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hasLineBlock = false;

            foreach (var raw in SplitSegments(remark!))
            {
                var seg = raw.Trim();
                if (seg.Length == 0)
                    continue;

                if (seg.StartsWith(VendorBankPrefix, StringComparison.Ordinal))
                {
                    bankId = Truncate(seg[VendorBankPrefix.Length..].Trim(), 36);
                    continue;
                }

                if (seg.StartsWith("费用(", StringComparison.Ordinal))
                {
                    var idx = seg.IndexOf("):", StringComparison.Ordinal);
                    if (idx < 0)
                        continue;
                    var tail = seg[(idx + 2)..].Trim();
                    var parts = tail.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5
                        && TryParseMoney(parts[0], out f0)
                        && TryParseMoney(parts[1], out f1)
                        && TryParseMoney(parts[2], out f2)
                        && TryParseMoney(parts[3], out f3)
                        && TryParseMoney(parts[4], out f4))
                    {
                        hasFee = true;
                    }

                    continue;
                }

                if (seg.StartsWith(PayerPrefix, StringComparison.Ordinal))
                {
                    payer = Truncate(seg[PayerPrefix.Length..].Trim(), 20);
                    hasPayer = true;
                    continue;
                }

                if (seg.StartsWith(LineRemarkPrefix, StringComparison.Ordinal))
                {
                    var block = seg[LineRemarkPrefix.Length..].Trim();
                    if (block.Length > 0)
                    {
                        ParseLineRemarkBlock(block, lineMap);
                        hasLineBlock = lineMap.Count > 0;
                    }

                    continue;
                }

                freeParts.Add(seg);
            }

            if (!hasFee)
                return false;

            var freeJoined = freeParts.Count == 0 ? null : Truncate(string.Join(" | ", freeParts), 500);
            result = new ParseResult
            {
                RequestFreeRemark = string.IsNullOrWhiteSpace(freeJoined) ? null : freeJoined,
                FinancePaymentBankId = string.IsNullOrWhiteSpace(bankId) ? null : bankId,
                FeeIntermediateBank = f0,
                FeeBankCharge = f1,
                FeeFreight = f2,
                FeeMisc = f3,
                FeeRounding = f4,
                HasFeeBlock = true,
                FeeIntermediateBankPayer = string.IsNullOrWhiteSpace(payer) ? null : payer,
                HasPayer = hasPayer,
                LineRemarkByKey = lineMap,
                HasLineRemarkBlock = hasLineBlock
            };
            return true;
        }

        /// <summary>将解析结果写入实体；成功时清空主表 <see cref="FinancePayment.Remark"/> 避免与新列重复。</summary>
        /// <param name="paymentBanks">用于将 Remark 中的「供应商银行」片段（可能为名称或非规范串）解析为真实 <see cref="FinancePaymentBank.Id"/>。</param>
        /// <returns>明细行备注实际写入条数。</returns>
        public static int ApplyToEntities(
            FinancePayment payment,
            IReadOnlyList<FinancePaymentItem> items,
            IReadOnlyDictionary<string, string?> purchaseOrderCodeById,
            ParseResult parsed,
            IReadOnlyList<FinancePaymentBank> paymentBanks)
        {
            var now = DateTime.UtcNow;
            var headerTouched = false;

            if (!string.IsNullOrWhiteSpace(parsed.FinancePaymentBankId))
            {
                var resolved = FinancePaymentBankIdResolver.ResolveFromToken(parsed.FinancePaymentBankId, paymentBanks);
                payment.FinancePaymentBankId = resolved;
                headerTouched = true;
            }

            if (parsed.HasFeeBlock)
            {
                payment.FeeIntermediateBank = parsed.FeeIntermediateBank;
                payment.FeeBankCharge = parsed.FeeBankCharge;
                payment.FeeFreight = parsed.FeeFreight;
                payment.FeeMisc = parsed.FeeMisc;
                payment.FeeRounding = parsed.FeeRounding;
                headerTouched = true;
            }

            if (parsed.HasPayer && parsed.FeeIntermediateBankPayer != null)
            {
                payment.FeeIntermediateBankPayer = parsed.FeeIntermediateBankPayer;
                headerTouched = true;
            }

            payment.RequestRemark = parsed.RequestFreeRemark;
            headerTouched = true;

            payment.Remark = null;
            headerTouched = true;

            if (headerTouched)
                payment.ModifyTime = now;

            var itemUpdates = 0;
            if (parsed.HasLineRemarkBlock && parsed.LineRemarkByKey.Count > 0 && items.Count > 0)
            {
                foreach (var item in items)
                {
                    if (item.IsDeleted)
                        continue;
                    string? hitKey = null;
                    var pn = item.PN?.Trim();
                    if (!string.IsNullOrEmpty(pn) && parsed.LineRemarkByKey.ContainsKey(pn))
                        hitKey = pn;
                    else if (!string.IsNullOrEmpty(item.PurchaseOrderId)
                             && purchaseOrderCodeById.TryGetValue(item.PurchaseOrderId, out var poCode)
                             && !string.IsNullOrWhiteSpace(poCode))
                    {
                        var code = poCode.Trim();
                        if (parsed.LineRemarkByKey.ContainsKey(code))
                            hitKey = code;
                    }

                    if (hitKey == null || !parsed.LineRemarkByKey.TryGetValue(hitKey, out var lineText))
                        continue;
                    var trimmed = Truncate(lineText.Trim(), 500);
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                    if (string.Equals(item.LineRemark, trimmed, StringComparison.Ordinal))
                        continue;
                    item.LineRemark = trimmed;
                    item.ModifyTime = now;
                    itemUpdates++;
                }
            }

            return itemUpdates;
        }

        private static IEnumerable<string> SplitSegments(string remark)
        {
            const string sep = " | ";
            var start = 0;
            while (start < remark.Length)
            {
                var idx = remark.IndexOf(sep, start, StringComparison.Ordinal);
                if (idx < 0)
                {
                    yield return remark[start..];
                    yield break;
                }

                yield return remark[start..idx];
                start = idx + sep.Length;
            }
        }

        private static void ParseLineRemarkBlock(string block, Dictionary<string, string> into)
        {
            foreach (var piece in block.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = piece.IndexOf(':');
                if (colon <= 0 || colon >= piece.Length - 1)
                    continue;
                var key = piece[..colon].Trim();
                var val = piece[(colon + 1)..].Trim();
                if (key.Length == 0 || val.Length == 0)
                    continue;
                into[key] = val;
            }
        }

        private static bool TryParseMoney(string s, out decimal d) =>
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d)
            || decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d);

        private static string Truncate(string s, int maxLen)
        {
            if (s.Length <= maxLen)
                return s;
            return s[..maxLen];
        }
    }
}
