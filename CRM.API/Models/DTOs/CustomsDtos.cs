namespace CRM.API.Models.DTOs;

public sealed class CustomsDeclarationListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public string? PackingId { get; set; }
    /// <summary>列表展示用：取首条明细关联的销售出库通知。</summary>
    public string? StockOutRequestId { get; set; }
    /// <summary>列表展示用：<see cref="StockOutRequestId"/> 对应业务单号。</summary>
    public string? StockOutRequestCode { get; set; }
    public string CustomsBrokerId { get; set; } = string.Empty;
    public string? CustomsBrokerName { get; set; }
    public short DeclarationType { get; set; }
    public short InternalStatus { get; set; }
    public short CustomsClearanceStatus { get; set; }
    public DateTime DeclareDate { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserDisplay { get; set; }
}

public sealed class CustomsDeclarationItemListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string DeclarationId { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public DateTime DeclareDate { get; set; }
    public int LineNo { get; set; }
    public string StockOutRequestId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? PurchasePn { get; set; }
    public string? PurchaseBrand { get; set; }
    public int DeclareQty { get; set; }
    /// <summary>拣货回写后的采购明细单号；未拣货或销售方向脱敏时为空。</summary>
    public string? PurchaseOrderItemCode { get; set; }
    /// <summary>跳转采购订单详情用；未拣货或脱敏时为空。</summary>
    public string? PurchaseOrderId { get; set; }
    /// <summary>P0 原币采购单价；未拣货或脱敏时为空。</summary>
    public decimal? OriginalPurchasePrice { get; set; }
    public short? PurchaseCurrency { get; set; }
    /// <summary>P0 × 申报数量；未拣货或脱敏时为空。</summary>
    public decimal? OriginalPurchaseAmount { get; set; }
    public decimal DeclareUnitPrice { get; set; }
    public decimal DutyAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal CustomsPaymentGoods { get; set; }
    public decimal CustomsAgencyFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal InspectionFee { get; set; }
    public decimal TotalValueTax { get; set; }
    public decimal TaxIncludedUnitPrice { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserDisplay { get; set; }
}

public sealed class CustomsDeclarationDetailViewDto
{
    public string Id { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public string? PackingId { get; set; }
    public string? PackingCode { get; set; }
    public string? StockOutRequestId { get; set; }
    public string? StockOutRequestCode { get; set; }
    public string CustomsBrokerId { get; set; } = string.Empty;
    public string? CustomsBrokerName { get; set; }
    public string? CustomsBrokerCode { get; set; }
    public short DeclarationType { get; set; }
    public short InternalStatus { get; set; }
    public short CustomsClearanceStatus { get; set; }
    public DateTime DeclareDate { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal BrokerAgencyRate { get; set; } = 1m;
    public bool AgencyRateManual { get; set; }
    public bool CostUsdManual { get; set; }
    /// <summary>报关公司资料当前代理费率，供费用面板「系统」模式对照。</summary>
    public decimal BrokerMasterAgencyRate { get; set; } = 1m;
    public decimal TotalTaxAmount { get; set; }
    public DateTime? FeesCalculatedAt { get; set; }
    public bool FeesLocked { get; set; }
    public string FromWarehouseId { get; set; } = string.Empty;
    public string ToWarehouseId { get; set; } = string.Empty;
    public string? FromWarehouseCode { get; set; }
    public string? ToWarehouseCode { get; set; }
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserDisplay { get; set; }
    public List<CustomsDeclarationDetailItemViewDto> Items { get; set; } = new();
    /// <summary>是否可人工生成报关到货通知（已结关、已维护目标仓、存在待生成明细）。</summary>
    public bool CanCreateArrivalNotifies { get; set; }
    public int PendingArrivalNotifyCount { get; set; }
    public int ExistingArrivalNotifyCount { get; set; }
    public List<string> ExistingArrivalNotifyCodes { get; set; } = new();
    public string? ArrivalNotifyBlockReason { get; set; }
}

public sealed class CustomsDeclarationDetailItemViewDto
{
    public string Id { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string? HsCode { get; set; }
    public string? PurchasePn { get; set; }
    public string? PurchaseBrand { get; set; }
    public int DeclareQty { get; set; }
    public decimal DeclareUnitPrice { get; set; }
    public decimal OriginalPurchasePrice { get; set; }
    public string? PurchaseCostParamId { get; set; }
    public decimal PurchaseRatio { get; set; } = 1m;
    public short? PurchaseCurrency { get; set; }
    public decimal DutyRate { get; set; }
    public decimal VatRate { get; set; } = 0.13m;
    public decimal CostUsd { get; set; }
    public bool CostUsdManual { get; set; }
    public decimal DutyAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal CustomsPaymentGoods { get; set; }
    public decimal CustomsAgencyFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal InspectionFee { get; set; }
    public decimal TotalValueTax { get; set; }
    public decimal TaxIncludedUnitPrice { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string StockOutRequestId { get; set; } = string.Empty;
    public string? ArrivalNotifyCode { get; set; }
}

public sealed class StockTransferListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string TransferCode { get; set; } = string.Empty;
    public string BizScene { get; set; } = string.Empty;
    public string CustomsDeclarationId { get; set; } = string.Empty;
    public string? DeclarationCode { get; set; }
    public short Status { get; set; }
    public DateTime? ConfirmedTime { get; set; }
    public string? ConfirmedByUserId { get; set; }
    public string FromWarehouseId { get; set; } = string.Empty;
    public string ToWarehouseId { get; set; } = string.Empty;
    public string? FromWarehouseName { get; set; }
    public string? ToWarehouseName { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserDisplay { get; set; }
    /// <summary>与「确认移仓」按钮一致：<c>ConfirmedTime</c> 为空视为未确认。</summary>
    public bool IsConfirmed { get; set; }
}
