using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// PRD：销售侧员工不可访问采购订单与付款单的上传附件；采购侧员工不可访问销售订单与收款单的上传附件。
/// 判定与列级脱敏 <see cref="PurchaseSensitiveFieldMask511"/> / <see cref="SaleSensitiveFieldMask521"/> 一致，便于前后端与文档对齐。
/// </summary>
public static class CrossSideDocumentAttachmentPolicy
{
    public const string BizPurchaseOrder = "PURCHASE_ORDER";
    public const string BizFinancePayment = "FINANCE_PAYMENT";
    public const string BizSalesOrder = "SALES_ORDER";
    public const string BizFinanceReceipt = "FINANCE_RECEIPT";

    public static bool IsPurchaseSideAttachmentBizType(string? bizType)
    {
        if (string.IsNullOrWhiteSpace(bizType)) return false;
        return bizType.Equals(BizPurchaseOrder, StringComparison.OrdinalIgnoreCase)
               || bizType.Equals(BizFinancePayment, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSalesSideAttachmentBizType(string? bizType)
    {
        if (string.IsNullOrWhiteSpace(bizType)) return false;
        return bizType.Equals(BizSalesOrder, StringComparison.OrdinalIgnoreCase)
               || bizType.Equals(BizFinanceReceipt, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>销售方向等（与 §5.1.1 一致）不可访问采购订单/付款附件。</summary>
    public static bool ShouldDenyPurchaseSideDocuments(UserPermissionSummaryDto? s) =>
        PurchaseSensitiveFieldMask511.ShouldMask(s);

    /// <summary>采购方向（与 §5.2.1 一致）不可访问销售订单/收款附件。</summary>
    public static bool ShouldDenySalesSideDocuments(UserPermissionSummaryDto? s) =>
        SaleSensitiveFieldMask521.ShouldMask(s);

    public static bool ShouldDeny(UserPermissionSummaryDto? s, string? bizType)
    {
        if (string.IsNullOrWhiteSpace(bizType)) return false;
        if (IsPurchaseSideAttachmentBizType(bizType))
            return ShouldDenyPurchaseSideDocuments(s);
        if (IsSalesSideAttachmentBizType(bizType))
            return ShouldDenySalesSideDocuments(s);
        return false;
    }
}
