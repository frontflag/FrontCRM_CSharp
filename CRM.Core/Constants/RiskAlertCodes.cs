namespace CRM.Core.Constants;

public static class RiskAlertSettingIds
{
    public const string Default = "risk-alert-default";
}

public static class RiskAlertItemCodes
{
    public const string InventoryAmount = "inventory-amount";
    public const string StockAge = "stock-age";
    public const string ReceivableAmount = "receivable-amount";
    public const string CustomerReceivable = "customer-receivable";
    public const string SoReceivableAge = "so-receivable-age";
}

public static class RiskAlertLimits
{
    public const decimal MaxUsd = 999_999_999.99m;
    public const int Scale = 2;
    public const int DefaultAgeDays = 90;
    public const int MaxDays = 9999;
}
