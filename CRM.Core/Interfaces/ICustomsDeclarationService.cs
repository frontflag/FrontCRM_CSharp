using CRM.Core.Models.Customs;

namespace CRM.Core.Interfaces;

public interface ICustomsDeclarationService
{
    Task<CustomsDeclaration?> GetByIdAsync(string id);
    Task<CustomsDeclaration?> GetByStockOutRequestIdAsync(string stockOutRequestId);
    Task SetCustomsClearanceStatusAsync(string declarationId, short customsClearanceStatus, string? actingUserId);
    /// <summary>报关完成 + 移库一步：循环调 <see cref="IInternalTransferPostingKernel"/>（<c>Kind=Customs</c>），写 <c>stocktransfer_customers</c> / 行，报关内部状态置完成。</summary>
    Task CompleteDeclarationAndTransferAsync(string declarationId, string? actingUserId);
}
