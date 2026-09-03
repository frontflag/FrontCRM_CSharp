using CRM.Core.Models.Customs;

namespace CRM.Core.Interfaces;

/// <summary>报关费用试算（对齐 EBS 报关.md §3.3）。</summary>
public interface ICustomsFeeCalculator
{
    /// <summary>按行输入计算费用链；<paramref name="otherFee"/> / <paramref name="inspectionFee"/> 不参与代理费基数。</summary>
    CustomsFeeLineResult CalculateLine(
        decimal originalPurchasePrice,
        short purchaseCurrency,
        decimal purchaseRatio,
        decimal exchangeRate,
        int declareQty,
        decimal dutyRate,
        decimal vatRate,
        decimal brokerAgencyRate,
        decimal otherFee,
        decimal inspectionFee,
        FinanceExchangeRateDto systemFx);

    /// <summary>
    /// 沿用已有货款/关税/增值税/杂费快照，只按新代理费率重算代理费、价税总额、含税单价 P1。
    /// </summary>
    CustomsFeeLineResult RecalculateAgencyFeeFromSnapshots(
        decimal customsPaymentGoods,
        decimal dutyAmount,
        decimal vatAmount,
        decimal otherFee,
        int declareQty,
        decimal brokerAgencyRate);
}

public sealed class CustomsFeeLineResult
{
    public decimal CostUsd { get; set; }
    public decimal CustomsUsdPrice { get; set; }
    public decimal CustomsPaymentGoods { get; set; }
    public decimal DutyAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal CustomsAgencyFee { get; set; }
    /// <summary>价税总额（不含商检）。</summary>
    public decimal TotalValueTax { get; set; }
    public decimal TaxIncludedUnitPrice { get; set; }
}
