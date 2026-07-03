namespace CRM.Core.Models.Quote;

/// <summary>
/// 报价主表状态（与 quote.status smallint 一致）
/// </summary>
public enum QuoteMainStatus : short
{
    /// <summary>新建：刚创建，尚未成单</summary>
    New = 0,

    /// <summary>成单：已用于生成有效销售订单</summary>
    Won = 1,

    /// <summary>关闭：所属需求已关闭/取消，且未成单</summary>
    Closed = 2
}
