using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>
/// 报关装箱（<c>StockOutType = 20</c>）Commercial Invoice：按报关明细拆行，单价用已试算的 <c>cost_usd</c>，币别 USD。
/// 未试算禁止打印。销售装箱 Invoice 不走本规则。
/// </summary>
public static class CustomsInvoiceReportPriceRules
{
    public const string FeesRequiredForInvoiceMessage = "请先试算费用";

    public static bool IsCustomsPacking(short stockOutType) =>
        StockOutTypeCode.NormalizeForNotify(stockOutType) == StockOutTypeCode.Customs;

    /// <summary>
    /// 报关装箱 Invoice 前置：须已有报关单、已试算，且每行 <c>cost_usd</c> &gt; 0。
    /// </summary>
    public static void EnsureDeclarationReadyForInvoice(
        bool declarationExists,
        DateTime? feesCalculatedAt,
        IReadOnlyCollection<decimal> costUsdByLine)
    {
        if (!declarationExists
            || feesCalculatedAt == null
            || costUsdByLine.Count == 0
            || costUsdByLine.Any(v => v <= 0m))
        {
            throw new InvalidOperationException(FeesRequiredForInvoiceMessage);
        }
    }
}
