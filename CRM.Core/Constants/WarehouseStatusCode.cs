namespace CRM.Core.Constants;

/// <summary>仓库档案 <c>warehouseinfo.Status</c>（启用状态）。</summary>
public static class WarehouseStatusCode
{
    /// <summary>停用。</summary>
    public const short Disabled = 0;

    /// <summary>启用。</summary>
    public const short Enabled = 1;

    public static short Normalize(short status) => status == Disabled ? Disabled : Enabled;
}
