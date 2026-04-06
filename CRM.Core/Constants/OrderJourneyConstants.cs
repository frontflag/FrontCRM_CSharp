namespace CRM.Core.Constants;

/// <summary>订单旅程日志 <c>log_orderjourney</c> 实体类型（EntityKind / ParentEntityKind）。</summary>
public static class OrderJourneyEntityKinds
{
    public const string SellOrder = "SellOrder";
    public const string SellOrderItem = "SellOrderItem";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string PurchaseOrderItem = "PurchaseOrderItem";
}

/// <summary>订单旅程事件码（EventCode）。</summary>
public static class OrderJourneyEventCodes
{
    public const string SoCreated = "SO_CREATED";
    public const string SoUpdated = "SO_UPDATED";
    public const string SoStatusChanged = "SO_STATUS_CHANGED";
    public const string SoDeleted = "SO_DELETED";
    public const string SoItemCreated = "SO_ITEM_CREATED";

    public const string PoCreated = "PO_CREATED";
    public const string PoUpdated = "PO_UPDATED";
    public const string PoStatusChanged = "PO_STATUS_CHANGED";
    public const string PoDeleted = "PO_DELETED";
    public const string PoItemCreated = "PO_ITEM_CREATED";
}

public static class OrderJourneyActorKinds
{
    public const string User = "User";
    public const string Vendor = "Vendor";
    public const string System = "System";
}
