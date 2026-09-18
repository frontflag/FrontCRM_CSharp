using CRM.Core.Constants;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionStockOutReceivableProgressTests
{
    static readonly DateOnly OutDate = new(2026, 3, 10);

    [Fact]
    public void Zero_amount_without_receivable_is_complete_on_stock_out_date()
    {
        var (status, date) = CommissionStockOutReceivableProgress.Resolve([], OutDate, 0m);
        Assert.Equal(FinanceVerificationStatusCode.Complete, status);
        Assert.Equal(OutDate, date);
    }

    [Fact]
    public void Missing_receivable_with_amount_is_pending()
    {
        var (status, date) = CommissionStockOutReceivableProgress.Resolve([], OutDate, 100m);
        Assert.Equal(FinanceVerificationStatusCode.Pending, status);
        Assert.Null(date);
    }

    [Fact]
    public void Partial_write_off_has_no_receipt_date()
    {
        var facts = new[]
        {
            new CommissionStockOutReceivableFact(
                FinanceVerificationStatusCode.Partial,
                100m,
                [new CommissionWriteOffStep(40m, new DateOnly(2026, 3, 20), Utc(2026, 3, 20))])
        };
        var (status, date) = CommissionStockOutReceivableProgress.Resolve(facts, OutDate, 100m);
        Assert.Equal(FinanceVerificationStatusCode.Partial, status);
        Assert.Null(date);
    }

    [Fact]
    public void Complete_uses_this_receivable_cover_date_not_later_unrelated_steps()
    {
        var facts = new[]
        {
            new CommissionStockOutReceivableFact(
                FinanceVerificationStatusCode.Complete,
                40m,
                [new CommissionWriteOffStep(40m, new DateOnly(2026, 3, 20), Utc(2026, 3, 20))])
        };
        var (status, date) = CommissionStockOutReceivableProgress.Resolve(facts, OutDate, 40m);
        Assert.Equal(FinanceVerificationStatusCode.Complete, status);
        Assert.Equal(new DateOnly(2026, 3, 20), date);
    }

    [Fact]
    public void Sibling_receivable_on_same_key_must_all_complete()
    {
        var facts = new[]
        {
            new CommissionStockOutReceivableFact(
                FinanceVerificationStatusCode.Complete,
                40m,
                [new CommissionWriteOffStep(40m, new DateOnly(2026, 3, 20), Utc(2026, 3, 20))]),
            new CommissionStockOutReceivableFact(
                FinanceVerificationStatusCode.Pending,
                60m,
                [])
        };
        var (status, date) = CommissionStockOutReceivableProgress.Resolve(facts, OutDate, 100m);
        Assert.Equal(FinanceVerificationStatusCode.Partial, status);
        Assert.Null(date);
    }

    [Fact]
    public void Complete_without_replayable_steps_has_status_but_no_date()
    {
        var facts = new[]
        {
            new CommissionStockOutReceivableFact(FinanceVerificationStatusCode.Complete, 100m, [])
        };
        var (status, date) = CommissionStockOutReceivableProgress.Resolve(facts, OutDate, 100m);
        Assert.Equal(FinanceVerificationStatusCode.Complete, status);
        Assert.Null(date);
    }

    static DateTime Utc(int y, int m, int d) => new(y, m, d, 8, 0, 0, DateTimeKind.Utc);
}
