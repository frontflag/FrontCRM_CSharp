namespace CRM.API.Models.DTOs;

public class CreateFreightForwarderCompanyRequest
{
    public string Cname { get; set; } = string.Empty;
    public string? Ename { get; set; }
    public string? Remark { get; set; }
}

public class UpdateFreightForwarderCompanyRequest
{
    public string Cname { get; set; } = string.Empty;
    public string? Ename { get; set; }
    public string? Remark { get; set; }
}

public class SetFreightForwarderCompanyStatusRequest
{
    public short Status { get; set; }
}

public class UpsertFreightForwarderCompanyBankRequest
{
    public string BankName { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? AccountNo { get; set; }
    public byte Currency { get; set; } = 1;
    public bool IsDefault { get; set; }
    public bool IsDisabled { get; set; }
}

public class UpdateReceiptFfCompanyRequest
{
    public string FreightForwarderCompanyId { get; set; } = string.Empty;
}
