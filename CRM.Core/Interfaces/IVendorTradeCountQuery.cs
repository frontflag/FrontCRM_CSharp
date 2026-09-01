namespace CRM.Core.Interfaces;

/// <summary>供应商交易次数现读（全公司，不按数据权限缩小）。</summary>
public interface IVendorTradeCountQuery
{
    /// <summary>
    /// 按付款单 <c>VendorId</c> 统计有效付款单下 distinct 采购明细行数。
    /// 请求中的供应商未出现时返回 0。不含货代付款。
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetTradeCountsAsync(
        IReadOnlyCollection<string> vendorIds,
        CancellationToken cancellationToken = default);
}
