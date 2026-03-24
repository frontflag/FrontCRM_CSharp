using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public sealed class EntityLookupServiceTests
{
    private static EntityLookupService CreateSut(
        IRepository<CustomerInfo>? customers = null,
        IUserService? users = null) =>
        new(
            customers ?? Substitute.For<IRepository<CustomerInfo>>(),
            Substitute.For<IRepository<CustomerContactInfo>>(),
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IRepository<VendorContactInfo>>(),
            users ?? Substitute.For<IUserService>());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCustomerByIdAsync_empty_id_skips_repository(string? id)
    {
        var customers = Substitute.For<IRepository<CustomerInfo>>();
        var svc = CreateSut(customers);

        Assert.Null(await svc.GetCustomerByIdAsync(id));
        await customers.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_delegates_to_repository()
    {
        var customers = Substitute.For<IRepository<CustomerInfo>>();
        var expected = new CustomerInfo { Id = "c1", OfficialName = "ACME" };
        customers.GetByIdAsync("c1").Returns(expected);
        var svc = CreateSut(customers);

        var r = await svc.GetCustomerByIdAsync("c1");
        Assert.Same(expected, r);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetUserDisplayNameAsync_empty_id_does_not_call_user_service(string? id)
    {
        var users = Substitute.For<IUserService>();
        var svc = CreateSut(users: users);

        Assert.Null(await svc.GetUserDisplayNameAsync(id));
        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
    }

    [Fact]
    public async Task GetUserDisplayNameAsync_prefers_real_name()
    {
        var users = Substitute.For<IUserService>();
        users.GetByIdAsync("u1").Returns(new User { Id = "u1", UserName = "login", RealName = "张三" });
        var svc = CreateSut(users: users);

        Assert.Equal("张三", await svc.GetUserDisplayNameAsync("u1"));
    }

    [Fact]
    public async Task GetUserDisplayNameAsync_falls_back_to_user_name()
    {
        var users = Substitute.For<IUserService>();
        users.GetByIdAsync("u1").Returns(new User { Id = "u1", UserName = "login", RealName = "   " });
        var svc = CreateSut(users: users);

        Assert.Equal("login", await svc.GetUserDisplayNameAsync("u1"));
    }
}
