namespace CRM.Core.Models.Customer;

/// <summary>右栏「客户」页签摘要。无完整查看权限时仅填充编号与档案业务员。</summary>
public sealed class CustomerWorkspaceDto
{
    public bool HasCustomer { get; set; }
    public bool CanViewFull { get; set; }

    /// <summary>仅 <see cref="CanViewFull"/> 时返回，供跳转客户详情。</summary>
    public string? CustomerId { get; set; }

    public string? CustomerCode { get; set; }
    public string? SalesUserName { get; set; }

    public string? ChineseName { get; set; }
    public string? EnglishName { get; set; }
    public short? CustomerType { get; set; }
    public string? CustomerLevel { get; set; }
    public string? Industry { get; set; }
    public string? Region { get; set; }
    public decimal? CreditLimit { get; set; }
    public short? PaymentTerms { get; set; }
    public short? SettlementCurrency { get; set; }
    public decimal? TaxRate { get; set; }
    public short? InvoiceType { get; set; }
}
