using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomsLockedCostUsdCorrectionTests
{
    [Fact]
    public void Unlocked_AllowsAnyone()
    {
        CustomsLockedCostUsdCorrection.EnsureCanChangeCostUsd(false, false);
        CustomsLockedCostUsdCorrection.EnsureCanRecalculate(false, false);
    }

    [Fact]
    public void Locked_Admin_Allows()
    {
        CustomsLockedCostUsdCorrection.EnsureCanChangeCostUsd(true, true);
        CustomsLockedCostUsdCorrection.EnsureCanRecalculate(true, true);
    }

    [Fact]
    public void Locked_NonAdmin_CannotChangeCostUsd()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsLockedCostUsdCorrection.EnsureCanChangeCostUsd(true, false));
        Assert.Equal(CustomsLockedCostUsdCorrection.LockedForbiddenMessage, ex.Message);
    }

    [Fact]
    public void Completed_Admin_AllowsChange()
    {
        CustomsLockedCostUsdCorrection.EnsureCanChangeCostUsd(false, true, completed: true);
        CustomsLockedCostUsdCorrection.EnsureCanRecalculate(false, true, completed: true);
    }

    [Fact]
    public void Completed_NonAdmin_CannotChangeCostUsd()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsLockedCostUsdCorrection.EnsureCanChangeCostUsd(false, false, completed: true));
        Assert.Equal(CustomsLockedCostUsdCorrection.CompletedForbiddenMessage, ex.Message);
    }

    [Fact]
    public void Completed_NonAdmin_CannotRecalculate()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsLockedCostUsdCorrection.EnsureCanRecalculate(false, false, completed: true));
        Assert.Equal(CustomsLockedCostUsdCorrection.CompletedForbiddenMessage, ex.Message);
    }

    [Fact]
    public void Locked_NonAdmin_CannotRecalculate()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsLockedCostUsdCorrection.EnsureCanRecalculate(true, false));
        Assert.Equal(CustomsLockedCostUsdCorrection.LockedRecalcForbiddenMessage, ex.Message);
    }
}
