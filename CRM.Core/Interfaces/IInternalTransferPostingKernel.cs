using CRM.Core.Services.InternalTransfer;

namespace CRM.Core.Interfaces;

/// <summary>
/// 移库共享内核：唯一 <c>stock</c>/<c>stock_item</c> 数量写口 + 对称虚拟调拨出/入 + <c>stockledger</c>；
/// 禁止调用 <c>StockOutService</c>/<c>StockInService</c> 正式过账入口（防双扣）。
/// </summary>
public interface IInternalTransferPostingKernel
{
    Task<InternalTransferPostingResult> ExecuteAsync(
        InternalTransferPostingRequest request,
        CancellationToken cancellationToken = default);
}
