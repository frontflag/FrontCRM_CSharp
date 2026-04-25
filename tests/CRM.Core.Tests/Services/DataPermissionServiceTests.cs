using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public sealed class DataPermissionServiceTests
{
    private static DataPermissionService CreateSut(
        IRbacService? rbacService = null) =>
        new(
            rbacService ?? Substitute.For<IRbacService>(),
            Substitute.For<IRepository<RbacDepartment>>(),
            Substitute.For<IRepository<RbacUserDepartment>>(),
            Substitute.For<IRepository<RbacUserRole>>(),
            Substitute.For<IRepository<RbacRole>>(),
            Substitute.For<IRepository<RFQ>>(),
            Substitute.For<IRepository<RFQItem>>(),
            Substitute.For<IRepository<CustomerInfo>>(),
            Substitute.For<IRepository<VendorInfo>>());

    [Fact]
    public async Task FilterVendorsAsync_sys_admin_not_cleared_when_purchase_scope_is_none()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("admin").Returns(new UserPermissionSummaryDto
        {
            UserId = "admin",
            IsSysAdmin = true,
            PurchaseDataScope = 4
        });

        var sut = CreateSut(rbac);
        var source = new List<VendorInfo>
        {
            new() { Id = "v1", Code = "V001" },
            new() { Id = "v2", Code = "V002" }
        };

        var result = await sut.FilterVendorsAsync("admin", source);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "v1", "v2" }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task FilterVendorsAsync_non_admin_with_purchase_scope_none_returns_empty()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("user-1").Returns(new UserPermissionSummaryDto
        {
            UserId = "user-1",
            IsSysAdmin = false,
            PurchaseDataScope = 4
        });

        var sut = CreateSut(rbac);
        var source = new List<VendorInfo> { new() { Id = "v1", Code = "V001" } };

        var result = await sut.FilterVendorsAsync("user-1", source);

        Assert.Empty(result);
    }
}
