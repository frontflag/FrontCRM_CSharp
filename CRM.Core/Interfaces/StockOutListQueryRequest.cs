namespace CRM.Core.Interfaces;

/// <summary>出库单列表筛选条件。</summary>
public class StockOutListQueryRequest
{
    /// <summary>兼容旧版：出库单号/出货方式/快递单号/销售明细编号（子串）。</summary>
    public string? Keyword { get; set; }

    /// <summary>来源单号精确匹配（设置后忽略 <see cref="Keyword"/> 及其他筛选）。</summary>
    public string? SourceCode { get; set; }

    /// <summary>出库单头状态。</summary>
    public short? Status { get; set; }

    /// <summary>出库单号（子串）。</summary>
    public string? StockOutCode { get; set; }

    /// <summary>装箱单号（子串，关联出库明细或拣货任务）。</summary>
    public string? PackingCode { get; set; }

    /// <summary>出货方式字典码（子串）。</summary>
    public string? ShipmentMethod { get; set; }

    /// <summary>客户/公司名称（模糊）。</summary>
    public string? CustomerName { get; set; }

    /// <summary>业务员姓名/登录名（模糊）。</summary>
    public string? SalesUserName { get; set; }

    /// <summary>备注（子串）。</summary>
    public string? Remark { get; set; }

    /// <summary>出库日期起（含）。</summary>
    public DateTime? StockOutDateFrom { get; set; }

    /// <summary>出库日期止（含当日）。</summary>
    public DateTime? StockOutDateTo { get; set; }
}
