using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class RfqHeaderDeleteItemSelectorTests
{
    [Fact]
    public void PrefersLoggedItemIds_OverModifyTime()
    {
        var headerTime = DateTime.UtcNow;
        var a = new RFQItem { Id = "A", LineNo = 1, IsDeleted = true, ModifyTime = headerTime };
        var b = new RFQItem { Id = "B", LineNo = 2, IsDeleted = true, ModifyTime = headerTime };
        var c = new RFQItem { Id = "C", LineNo = 3, IsDeleted = true, ModifyTime = headerTime.AddMinutes(-10) };

        var selected = RfqHeaderDeleteItemSelector.Select(new[] { a, b, c }, headerTime, new HashSet<string> { "A", "B" });

        Assert.Equal(new[] { "A", "B" }, selected.Select(i => i.Id).ToArray());
    }

    [Fact]
    public void FallsBackToModifyTimeWindow_WhenNoLoggedIds()
    {
        var headerTime = DateTime.UtcNow;
        var a = new RFQItem { Id = "A", LineNo = 1, IsDeleted = true, ModifyTime = headerTime.AddSeconds(-1) };
        var c = new RFQItem { Id = "C", LineNo = 3, IsDeleted = true, ModifyTime = headerTime.AddHours(-2) };

        var selected = RfqHeaderDeleteItemSelector.Select(new[] { a, c }, headerTime, null);

        Assert.Single(selected);
        Assert.Equal("A", selected[0].Id);
    }

    [Fact]
    public void ExcludesNonDeleted()
    {
        var headerTime = DateTime.UtcNow;
        var live = new RFQItem { Id = "L", LineNo = 1, IsDeleted = false, ModifyTime = headerTime };

        var selected = RfqHeaderDeleteItemSelector.Select(new[] { live }, headerTime, new HashSet<string> { "L" });

        Assert.Empty(selected);
    }
}
