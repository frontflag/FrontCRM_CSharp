namespace CRM.Core.Models.RFQ;

/// <summary>
/// RFQ 明细 <see cref="RFQItem.Status"/> 权威取值（与 DB、前端、文档一致）。
/// </summary>
public enum RfqItemStatus : short
{
    /// <summary>待报价</summary>
    Pending = 0,

    /// <summary>已报价</summary>
    Quoted = 1,

    /// <summary>已接受</summary>
    Accepted = 2,

    /// <summary>已拒绝</summary>
    Rejected = 3,

    /// <summary>已关闭</summary>
    Closed = 4,

    /// <summary>查无报价（采购确认无货无价，不创建报价单）</summary>
    NoQuoteFound = 5,
}
