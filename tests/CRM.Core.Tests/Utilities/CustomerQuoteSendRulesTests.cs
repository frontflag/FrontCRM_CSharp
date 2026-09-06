using CRM.Core.Models.Quote;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

/// <summary>对照 QA：CQ-MAIL-003 / CQ-MAIL-005；作废拒绝发送。</summary>
public class CustomerQuoteSendRulesTests
{
    [Fact]
    public void Unsent_BecomesSent_WithSentAtAndEmailFlag()
    {
        var header = new CustomerQuote { Status = CustomerQuoteStatus.Unsent };
        var now = new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc);
        CustomerQuoteSendRules.ApplySentByEmail(header, now);
        Assert.Equal(CustomerQuoteStatus.Sent, header.Status);
        Assert.Equal(now, header.SentAt);
        Assert.True(header.SentByEmail);
    }

    [Fact]
    public void AlreadySent_KeepsStatusAndFirstSentAt()
    {
        var first = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        var header = new CustomerQuote
        {
            Status = CustomerQuoteStatus.Sent,
            SentAt = first,
            SentByEmail = false
        };
        CustomerQuoteSendRules.ApplySentByEmail(header, first.AddDays(1));
        Assert.Equal(CustomerQuoteStatus.Sent, header.Status);
        Assert.Equal(first, header.SentAt);
        Assert.True(header.SentByEmail);
    }

    [Fact]
    public void Void_Throws()
    {
        var header = new CustomerQuote { Status = CustomerQuoteStatus.Void };
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteSendRules.ApplySentByEmail(header, DateTime.UtcNow));
        Assert.Equal(CustomerQuoteSendRules.VoidCannotSendMessage, ex.Message);
        Assert.False(CustomerQuoteSendRules.CanSendEmail(CustomerQuoteStatus.Void));
        Assert.True(CustomerQuoteSendRules.CanSendEmail(CustomerQuoteStatus.Unsent));
        Assert.True(CustomerQuoteSendRules.CanSendEmail(CustomerQuoteStatus.Sent));
    }

    [Fact]
    public void MarkSent_Unsent_SetsSentAt_DoesNotSetEmailFlag()
    {
        var header = new CustomerQuote { Status = CustomerQuoteStatus.Unsent, SentByEmail = false };
        var now = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        CustomerQuoteSendRules.ApplyMarkSent(header, now);
        Assert.Equal(CustomerQuoteStatus.Sent, header.Status);
        Assert.Equal(now, header.SentAt);
        Assert.False(header.SentByEmail);
        Assert.True(CustomerQuoteSendRules.CanMarkSent(CustomerQuoteStatus.Unsent));
        Assert.False(CustomerQuoteSendRules.CanMarkSent(CustomerQuoteStatus.Sent));
        Assert.False(CustomerQuoteSendRules.CanMarkSent(CustomerQuoteStatus.Void));
    }

    [Fact]
    public void MarkSent_AlreadySent_Throws()
    {
        var header = new CustomerQuote { Status = CustomerQuoteStatus.Sent };
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteSendRules.ApplyMarkSent(header, DateTime.UtcNow));
        Assert.Equal(CustomerQuoteSendRules.OnlyUnsentCanMarkSentMessage, ex.Message);
    }

    [Fact]
    public void MarkSent_Void_Throws()
    {
        var header = new CustomerQuote { Status = CustomerQuoteStatus.Void };
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteSendRules.ApplyMarkSent(header, DateTime.UtcNow));
        Assert.Equal(CustomerQuoteSendRules.OnlyUnsentCanMarkSentMessage, ex.Message);
    }

    [Fact]
    public void CanDelete_OnlyUnsent()
    {
        Assert.True(CustomerQuoteSendRules.CanDelete(CustomerQuoteStatus.Unsent));
        Assert.False(CustomerQuoteSendRules.CanDelete(CustomerQuoteStatus.Sent));
        Assert.False(CustomerQuoteSendRules.CanDelete(CustomerQuoteStatus.Void));
    }

    [Theory]
    [InlineData("a@b.com", true)]
    [InlineData("  sales@example.co.uk  ", true)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    [InlineData("a@b", false)]
    [InlineData("a @b.com", false)]
    public void LooksLikeEmail_Validates(string input, bool expected)
    {
        Assert.Equal(expected, CustomerQuoteSendRules.LooksLikeEmail(input));
    }
}
