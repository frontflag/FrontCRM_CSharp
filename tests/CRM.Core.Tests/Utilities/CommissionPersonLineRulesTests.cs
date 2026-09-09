using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionPersonLineRulesTests
{
    [Fact]
    public void Receipt_complete_is_write_off_done()
    {
        Assert.False(CommissionPersonLineRules.IsReceiptWriteOffDone(0));
        Assert.False(CommissionPersonLineRules.IsReceiptWriteOffDone(1));
        Assert.True(CommissionPersonLineRules.IsReceiptWriteOffDone(2));
    }

    [Fact]
    public void In_commission_needs_complete_and_delay()
    {
        var today = new DateOnly(2026, 4, 12);
        var receipt = new DateOnly(2026, 4, 10);
        Assert.False(CommissionPersonLineRules.InCommission(2, receipt, today, 3));
        Assert.True(CommissionPersonLineRules.InCommission(2, receipt, today, 1));
        Assert.False(CommissionPersonLineRules.InCommission(1, receipt, today, 0));
        Assert.False(CommissionPersonLineRules.InCommission(2, null, today, 0));
    }

    [Fact]
    public void Days_since_receipt_null_when_no_date()
    {
        var today = new DateOnly(2026, 4, 12);
        Assert.Null(CommissionPersonLineRules.DaysSinceReceipt(null, today));
        Assert.Equal(2, CommissionPersonLineRules.DaysSinceReceipt(new DateOnly(2026, 4, 10), today));
    }
}
