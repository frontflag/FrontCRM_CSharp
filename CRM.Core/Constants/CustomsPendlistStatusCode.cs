namespace CRM.Core.Constants;

/// <summary>待报关列表 <c>customs_pendlist.status</c>。</summary>
public static class CustomsPendlistStatusCode
{
    /// <summary>待处理（销售 SOR 已创建，尚未生成报关出库通知）。</summary>
    public const short Open = 1;

    /// <summary>已生成报关出库通知。</summary>
    public const short CustomsOutNotifyCreated = 2;

    /// <summary>已进入报关流程（装箱/报关记录等）。</summary>
    public const short InCustomsProcess = 3;

    /// <summary>已关闭（报关入库完成或业务完结）。</summary>
    public const short Closed = 10;

    /// <summary>已取消。</summary>
    public const short Cancelled = -1;
}
