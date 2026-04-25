namespace CRM.Core.Constants;

/// <summary>报关单内部状态（与海关状态分离）。</summary>
public static class CustomsDeclarationInternalStatus
{
    public const short Pending = 1;
    public const short Processing = 2;
    public const short Completed = 3;
    public const short Voided = -1;
}

/// <summary>海关侧状态（人工维护）；已结关后才允许「报关完成+移库」。</summary>
public static class CustomsClearanceStatusCodes
{
    public const short None = 0;
    public const short Released = 10;
    /// <summary>已结关：允许触发报关完成与移库。</summary>
    public const short Cleared = 100;
}

public static class CustomsDeclarationType
{
    public const short Import = 1;
    public const short Export = 2;
}

public static class StockTransferBizScene
{
    public const string CustomsImport = "CustomsImport";
    public const string Internal = "Internal";
}

/// <summary>移库单状态；一期确认过账后直接为已确认。</summary>
public static class StockTransferStatus
{
    public const short Confirmed = 2;
}

/// <summary>库存流水 BizType：移库（出/入合并一条）。</summary>
public static class StockLedgerBizType
{
    public const string StockTransfer = "STOCK_TRANS";

    /// <summary>手工移库流水。</summary>
    public const string ManualTransfer = "MANUAL_TRANS";
}

/// <summary>审批/日志用业务类型：出库通知需报关。</summary>
public static class CustomsApprovalBizTypes
{
    public const string StockOutRequestCustoms = "StockOutRequestCustoms";
}

/// <summary>报关公司服务方向（列 <c>Type</c>）。</summary>
public static class CustomsBrokerServiceRegion
{
    public const short Shenzhen = 10;
    public const short HongKong = 20;
}

/// <summary>报关公司 <c>Status</c>：与列表展示一致，非 1 视为停用。</summary>
public static class CustomsBrokerStatusCodes
{
    public const short Active = 1;
    public const short Inactive = 0;
}
