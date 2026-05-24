namespace CRM.Core.Interfaces;

/// <summary>装箱单列表筛选条件。</summary>
public class PackingListQueryRequest
{
    public string? PackingCode { get; set; }
    public short? Status { get; set; }
    public short? StockOutType { get; set; }
    public short? MaterialType { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesUserName { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }
}
