using CRM.Core.Constants;
using FluentAssertions;

namespace CRM.Core.Tests.Constants;

public sealed class PurchaseOrderItemListQuickFilterCodesTests
{
    [Fact]
    public void IsKnown_includes_document_presets()
    {
        PurchaseOrderItemListQuickFilterCodes.IsKnown("has_purchase_order_docs").Should().BeTrue();
        PurchaseOrderItemListQuickFilterCodes.IsKnown("no_purchase_order_docs").Should().BeTrue();
        PurchaseOrderItemListQuickFilterCodes.IsKnown("pending_submit_audit").Should().BeTrue();
        PurchaseOrderItemListQuickFilterCodes.IsKnown("unknown_docs").Should().BeFalse();
        PurchaseOrderItemListQuickFilterCodes.IsKnown("").Should().BeFalse();
    }
}
