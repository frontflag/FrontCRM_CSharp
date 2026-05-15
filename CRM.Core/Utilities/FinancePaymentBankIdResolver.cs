using CRM.Core.Models.Finance;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 将请款场景中的「付款银行」占位（历史 Remark 片段、或误写入的名称/别名）解析为 <see cref="FinancePaymentBank"/> 主键。
    /// </summary>
    public static class FinancePaymentBankIdResolver
    {
        /// <summary>
        /// 先按主键精确匹配，再按 <see cref="FinancePaymentBank.BankName"/> 去首尾空白后 OrdinalIgnoreCase 匹配；同名时优先未禁用行。
        /// </summary>
        public static string? ResolveFromToken(string? token, IReadOnlyList<FinancePaymentBank> banks)
        {
            if (banks == null || banks.Count == 0 || string.IsNullOrWhiteSpace(token))
                return null;
            var t = token.Trim();
            if (t.Length == 0)
                return null;

            foreach (var b in banks)
            {
                if (string.Equals(b.Id, t, StringComparison.OrdinalIgnoreCase))
                    return b.Id;
            }

            FinancePaymentBank? disabledMatch = null;
            foreach (var b in banks)
            {
                var name = b.BankName?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                if (!string.Equals(name, t, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!b.IsDisabled)
                    return b.Id;
                disabledMatch ??= b;
            }

            return disabledMatch?.Id;
        }
    }
}
