using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class FinancePaymentHeaderVerificationTests
{
    [Fact]
    public void Resolve_AllPending_IsPending()
    {
        Assert.Equal(
            FinanceVerificationStatusCode.Pending,
            FinancePaymentHeaderVerification.Resolve(
                FinanceVerificationStatusCode.Pending,
                FinanceVerificationStatusCode.Pending));
    }

    [Fact]
    public void Resolve_AllComplete_IsComplete()
    {
        Assert.Equal(
            FinanceVerificationStatusCode.Complete,
            FinancePaymentHeaderVerification.Resolve(
                FinanceVerificationStatusCode.Complete,
                FinanceVerificationStatusCode.Complete));
    }

    [Fact]
    public void Resolve_Mixed_IsPartial()
    {
        Assert.Equal(
            FinanceVerificationStatusCode.Partial,
            FinancePaymentHeaderVerification.Resolve(
                FinanceVerificationStatusCode.Pending,
                FinanceVerificationStatusCode.Complete));
    }
}
