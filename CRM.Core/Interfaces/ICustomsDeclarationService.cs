using CRM.Core.Models.Customs;

namespace CRM.Core.Interfaces;

public interface ICustomsDeclarationService
{
    Task<CustomsDeclaration?> GetByIdAsync(string id);
    Task<CustomsDeclaration?> GetByStockOutRequestIdAsync(string stockOutRequestId);
    Task SetCustomsClearanceStatusAsync(string declarationId, short customsClearanceStatus, string? actingUserId);
    /// <summary>报关完成 + 移库一步（库存过账在后续迭代实现，当前会返回明确错误）。</summary>
    Task CompleteDeclarationAndTransferAsync(string declarationId, string? actingUserId);
}
