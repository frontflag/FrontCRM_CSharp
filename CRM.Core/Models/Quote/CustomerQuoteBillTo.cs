namespace CRM.Core.Models.Quote;

/// <summary>客户报价单报表 Bill To（客户 + 联系人快照）。</summary>
public class CustomerQuoteBillTo
{
    public string? Company { get; set; }
    public string? CompanyEn { get; set; }
    public string? Attn { get; set; }
    public string? Tel { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
