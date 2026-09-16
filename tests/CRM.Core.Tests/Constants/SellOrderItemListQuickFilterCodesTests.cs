using CRM.Core.Constants;
using FluentAssertions;

namespace CRM.Core.Tests.Constants;

public sealed class SellOrderItemListQuickFilterCodesTests
{
    [Fact]
    public void IsKnown_includes_document_presets()
    {
        SellOrderItemListQuickFilterCodes.IsKnown("has_sales_order_docs").Should().BeTrue();
        SellOrderItemListQuickFilterCodes.IsKnown("no_sales_order_docs").Should().BeTrue();
        SellOrderItemListQuickFilterCodes.IsKnown("pending_submit_audit").Should().BeTrue();
        SellOrderItemListQuickFilterCodes.IsKnown("unknown_docs").Should().BeFalse();
        SellOrderItemListQuickFilterCodes.IsKnown("").Should().BeFalse();
    }
}
