using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public sealed class CommissionRateServiceTests
{
    [Fact]
    public async Task GetSettings_seeds_v1_active_for_both_roles()
    {
        var sut = CreateSut();
        var settings = await sut.GetSettingsAsync();

        Assert.Single(settings.SalesVersions);
        Assert.Single(settings.PurchaseVersions);
        Assert.Equal(1, settings.SalesVersions[0].VersionNo);
        Assert.True(settings.SalesVersions[0].IsActive);
        Assert.True(settings.PurchaseVersions[0].IsActive);
        Assert.Equal(settings.SalesVersions[0].Id, settings.SalesActiveVersionId);
        Assert.Equal(0, settings.ReceiptWriteoffDelayDays);
    }

    [Fact]
    public async Task CreateVersion_copies_ladders_as_draft()
    {
        var sut = CreateSut();
        var v1 = (await sut.ListVersionsAsync((short)CommissionRoleType.Sales)).Single();
        var rows = await sut.ListAsync((short)CommissionRoleType.Sales, v1.Id);
        var level1 = rows.Single(r => r.UserLevel == 1);
        await sut.UpdateAsync(
            level1.Id,
            new[]
            {
                new CommissionLadderWriteDto { ThresholdAmount = 0, RatePoints = 5.0m },
                new CommissionLadderWriteDto { ThresholdAmount = 10000, RatePoints = 6.0m }
            }.Concat(Enumerable.Range(0, 8).Select(_ => new CommissionLadderWriteDto())).ToList(),
            "档",
            "op");

        var created = await sut.CreateVersionAsync((short)CommissionRoleType.Sales, "下一版", v1.Id, "op");

        Assert.Equal(2, created.VersionNo);
        Assert.False(created.IsActive);
        Assert.Equal("下一版", created.Remark);
        var copied = await sut.ListAsync((short)CommissionRoleType.Sales, created.Id);
        var copiedLv1 = copied.Single(r => r.UserLevel == 1);
        Assert.Equal(0m, copiedLv1.Thresholds[0]);
        Assert.Equal(5.0m, copiedLv1.RatePoints[0]);
        Assert.Equal(20, copied.Count);
    }

    [Fact]
    public async Task SetActive_moves_previous_active_to_draft()
    {
        var sut = CreateSut();
        var v1 = (await sut.ListVersionsAsync((short)CommissionRoleType.Sales)).Single();
        var p1 = (await sut.ListVersionsAsync((short)CommissionRoleType.Purchase)).Single();
        var v2 = await sut.CreateVersionAsync((short)CommissionRoleType.Sales, "试用", v1.Id, "op");

        await sut.SetActiveVersionsAsync(v2.Id, p1.Id, 7, "op");
        var settings = await sut.GetSettingsAsync();

        Assert.Equal(v2.Id, settings.SalesActiveVersionId);
        Assert.Equal(7, settings.ReceiptWriteoffDelayDays);
        Assert.False(settings.SalesVersions.Single(v => v.Id == v1.Id).IsActive);
        Assert.True(settings.SalesVersions.Single(v => v.Id == v2.Id).IsActive);
        Assert.Equal(p1.Id, settings.PurchaseActiveVersionId);
    }

    [Fact]
    public async Task List_without_version_uses_active()
    {
        var sut = CreateSut();
        var v1 = (await sut.ListVersionsAsync((short)CommissionRoleType.Sales)).Single();
        var p1 = (await sut.ListVersionsAsync((short)CommissionRoleType.Purchase)).Single();
        var v2 = await sut.CreateVersionAsync((short)CommissionRoleType.Sales, "计算用", v1.Id, "op");
        await sut.SetActiveVersionsAsync(v2.Id, p1.Id, 0, "op");

        var rows = await sut.ListAsync((short)CommissionRoleType.Sales);
        Assert.All(rows, r => Assert.Equal(v2.Id, r.VersionId));
    }

    [Fact]
    public async Task SetActive_rejects_cross_type()
    {
        var sut = CreateSut();
        var sales = (await sut.ListVersionsAsync((short)CommissionRoleType.Sales)).Single();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SetActiveVersionsAsync(sales.Id, sales.Id, 0, "op"));
    }

    [Fact]
    public async Task SetActive_rejects_delay_days_out_of_range()
    {
        var sut = CreateSut();
        var sales = (await sut.ListVersionsAsync((short)CommissionRoleType.Sales)).Single();
        var purchase = (await sut.ListVersionsAsync((short)CommissionRoleType.Purchase)).Single();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SetActiveVersionsAsync(sales.Id, purchase.Id, -1, "op"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SetActiveVersionsAsync(sales.Id, purchase.Id, 3651, "op"));
    }

    static CommissionRateService CreateSut()
    {
        var rates = new MemoryRepository<CommissionRate>();
        var versions = new MemoryRepository<CommissionRateVersion>();
        var settings = new MemoryRepository<CommissionCalcSetting>();
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync().Returns(1);
        return new CommissionRateService(rates, versions, settings, uow);
    }
}
