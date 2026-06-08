namespace CRM.Core.Interfaces;

/// <summary>出库通知列表筛选条件。</summary>
public class StockOutRequestListQueryRequest
{
    /// <summary>快捷关键词（通知单号/销售单号/物料/客户）。</summary>
    public string? Keyword { get; set; }

    /// <summary>已废弃流程筛选：all | done | pending_pick | picked_pending_out。</summary>
    public string? Workflow { get; set; }

    /// <summary>业务状态 <see cref="Constants.StockOutRequestStatusCode"/>。</summary>
    public short? Status { get; set; }

    /// <summary>地域 10=境内 20=境外。</summary>
    public short? RegionType { get; set; }

    /// <summary>出库类型 <see cref="Constants.StockOutTypeCode"/>（10销售 20报关 30退货 40报废）。</summary>
    public short? StockOutType { get; set; }

    /// <summary>客户/公司名称（模糊）。</summary>
    public string? CustomerName { get; set; }

    /// <summary>业务员姓名/登录名（模糊）。</summary>
    public string? SalesUserName { get; set; }

    /// <summary>物料型号 PN（模糊）。</summary>
    public string? MaterialModel { get; set; }

    /// <summary>预计出库日期起（含）。</summary>
    public DateTime? RequestDateFrom { get; set; }

    /// <summary>预计出库日期止（含当日）。</summary>
    public DateTime? RequestDateTo { get; set; }

    /// <summary>当前登录用户 Id（销售数据范围过滤）。</summary>
    public string? CurrentUserId { get; set; }
}
