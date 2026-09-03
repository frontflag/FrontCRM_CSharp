using CRM.Core.Models.Quote;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class PurchaseSensitiveFieldMask511QuoteVendorTests
{
    [Fact]
    public void ApplyQuotesVendorIdentityOnly_ClearsEnglishName()
    {
        var quote = new Quote
        {
            Items =
            {
                new QuoteItem
                {
                    VendorId = "v1",
                    VendorName = "中文",
                    VendorEnglishName = "ENGLISH",
                    VendorCode = "V001",
                    UnitPrice = 12.5m
                }
            }
        };

        PurchaseSensitiveFieldMask511.ApplyQuotesVendorIdentityOnly(new[] { quote }, true);

        var item = quote.Items.First();
        Assert.Null(item.VendorId);
        Assert.Null(item.VendorName);
        Assert.Null(item.VendorEnglishName);
        Assert.Null(item.VendorCode);
        Assert.Equal(12.5m, item.UnitPrice);
    }
}
