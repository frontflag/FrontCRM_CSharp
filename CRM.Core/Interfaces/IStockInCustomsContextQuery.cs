using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>报关入库详情：沿 stock_in → 到货通知 → 报关明细 解析溯源上下文。</summary>
public interface IStockInCustomsContextQuery
{
    Task<StockInCustomsContextDto?> LoadAsync(StockIn stockIn, CancellationToken cancellationToken = default);
}

/// <summary>报关入库详情上下文（仅 StockInType=20 时填充）。</summary>
public class StockInCustomsContextDto
{
    public string? QcId { get; set; }
    public string? QcCode { get; set; }
    public List<StockInCustomsContextItemDto> Items { get; set; } = new();
}

public class StockInCustomsContextItemDto
{
    public string? ArrivalNotifyId { get; set; }
    public string? ArrivalNotifyCode { get; set; }
    public string DeclarationItemId { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string DeclarationId { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public string CustomsBrokerId { get; set; } = string.Empty;
    public string? CustomsBrokerName { get; set; }
    public string? CustomsBrokerCode { get; set; }
    public string? PackingId { get; set; }
    public string? PackingCode { get; set; }
    public string? FromWarehouseId { get; set; }
    public string? FromWarehouseCode { get; set; }
    public string? ToWarehouseId { get; set; }
    public string? ToWarehouseCode { get; set; }
    public string? SalesStockOutNotifyId { get; set; }
    public string? SalesStockOutNotifyCode { get; set; }
    public string? CustomsStockOutNotifyId { get; set; }
    public string? CustomsStockOutNotifyCode { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal? OriginalPurchasePrice { get; set; }
    public decimal? TaxIncludedUnitPrice { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PurchasePn { get; set; }
    public string? PurchaseBrand { get; set; }
    public int? DeclareQty { get; set; }
    public short? CustomsClearanceStatus { get; set; }
    public string? HsCode { get; set; }
    public decimal? DeclareUnitPrice { get; set; }
    public decimal? DutyAmount { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? CustomsPaymentGoods { get; set; }
    public decimal? CustomsAgencyFee { get; set; }
    public decimal? OtherFee { get; set; }
    public decimal? InspectionFee { get; set; }
    public decimal? TotalValueTax { get; set; }
    public DateTime? DeclareDate { get; set; }
    public decimal? DeclarationTotalTaxAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public List<StockInCustomsTimelineStepDto> Timeline { get; set; } = new();
}

/// <summary>报关链路时间线步骤（pendlist → 装箱 → 报关出库 → 报关单 → 移库 → 到货 → 质检 → 入库）。</summary>
public class StockInCustomsTimelineStepDto
{
    /// <summary>步骤编码：salesStockOutNotify | pendlist | customsStockOutNotify | packing | declaration | stockTransfer | arrivalNotify | qc | stockIn</summary>
    public string StepCode { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? DocId { get; set; }

    public string? DocCode { get; set; }

    public short? Status { get; set; }

    public DateTime? OccurredAt { get; set; }

    /// <summary>pending=未发生；done=已完成</summary>
    public string State { get; set; } = "pending";
}
