using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class PurchaseLineFinancePaymentStatusTests
{
    [Theory]
    [InlineData(0, 21375, 0)]
    [InlineData(0.00, 21375, 0)]
    public void Unpaid_WhenPaidIsZero(decimal paid, decimal total, short expected)
    {
        Assert.Equal(expected, PurchaseLineFinancePaymentStatus.FromPaidVersusLineTotal(paid, total));
    }

    [Fact]
    public void Partial_WhenPaidLessThanLineTotal()
    {
        Assert.Equal(
            PurchaseLineFinancePaymentStatus.Partial,
            PurchaseLineFinancePaymentStatus.FromPaidVersusLineTotal(6412.50m, 21375m));
    }

    [Theory]
    [InlineData(21375, 21375)]
    [InlineData(21375.01, 21375)]
    public void Complete_WhenPaidReachesLineTotal(decimal paid, decimal total)
    {
        Assert.Equal(
            PurchaseLineFinancePaymentStatus.Complete,
            PurchaseLineFinancePaymentStatus.FromPaidVersusLineTotal(paid, total));
    }

    [Fact]
    public void Sum_ExcludesCancelledAndAuditFailedPayments()
    {
        var items = new[]
        {
            new FinancePaymentItem { FinancePaymentId = "ok", VerificationDone = 100m },
            new FinancePaymentItem { FinancePaymentId = "can", VerificationDone = 50m },
            new FinancePaymentItem { FinancePaymentId = "fail", VerificationDone = 30m }
        };
        var payments = new[]
        {
            new FinancePayment { Id = "ok", Status = 100 },
            new FinancePayment { Id = "can", Status = -2 },
            new FinancePayment { Id = "fail", Status = -1 }
        };

        Assert.Equal(100m, PurchaseLineFinancePaymentStatus.SumVerificationDoneOnValidPayments(items, payments));
    }

    [Fact]
    public void Header_Partial_WhenAnyLinePartial()
    {
        Assert.Equal(
            PurchaseLineFinancePaymentStatus.Partial,
            PurchaseLineFinancePaymentStatus.HeaderFromLineStatuses(new short[] { 2, 1, 0 }));
    }
}
