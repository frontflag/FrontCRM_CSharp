using System.Globalization;

namespace CRM.Core.Utilities
{
    /// <summary>用户可见提示中的数量展示（整数，避免多余小数位）。</summary>
    public static class QuantityMessageFormatting
    {
        public static string ForUserMessage(decimal value) =>
            decimal.Round(value, 0, MidpointRounding.AwayFromZero)
                .ToString("0", CultureInfo.InvariantCulture);
    }
}
