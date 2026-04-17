namespace CRM.API.Models.DTOs;

public sealed class CustomsDeclarationListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public string StockOutRequestId { get; set; } = string.Empty;
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
