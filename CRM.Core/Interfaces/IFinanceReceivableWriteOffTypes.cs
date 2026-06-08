using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public class FinanceAdvancePoolAllocation
{
    public string FinanceReceivableId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>用于 SO 软约束提示（可选）</summary>
    public string? AdvanceSellOrderId { get; set; }
}

public class FinanceReceivableWriteOffResult
{
    public bool Applied { get; set; }
    public bool RequiresSoMismatchConfirm { get; set; }
    public List<FinanceReceivableWriteOffSoMismatch> SoMismatches { get; set; } = new();
}
