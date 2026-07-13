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
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IPurchaseQuoterPoolService>());

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

    [Fact]
    public async Task FilterCustomersAsync_finance_identity_not_blocked_when_sale_scope_is_none()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("finance-1").Returns(new UserPermissionSummaryDto
        {
            UserId = "finance-1",
            IsSysAdmin = false,
            IdentityType = 5,
            SaleDataScope = 4
        });

        var sut = CreateSut(rbac);
        var source = new List<CustomerInfo>
        {
            new() { Id = "c1", CustomerCode = "C001" },
            new() { Id = "c2", CustomerCode = "C002" }
        };

        var result = await sut.FilterCustomersAsync("finance-1", source);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FilterVendorsAsync_finance_identity_not_blocked_when_purchase_scope_is_none()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("finance-1").Returns(new UserPermissionSummaryDto
        {
            UserId = "finance-1",
            IsSysAdmin = false,
            IdentityType = 5,
            PurchaseDataScope = 4
        });

        var sut = CreateSut(rbac);
        var source = new List<VendorInfo> { new() { Id = "v1", Code = "V001" } };

        var result = await sut.FilterVendorsAsync("finance-1", source);

        Assert.Single(result);
        Assert.Equal("v1", result[0].Id);
    }

    [Fact]
    public async Task FilterSalesOrdersAsync_dept_employee_only_sees_own_orders_even_when_logistics_scope_all()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("king").Returns(new UserPermissionSummaryDto
        {
            UserId = "king",
            IsSysAdmin = false,
            IdentityType = 1,
            SaleDataScope = 3,
            LogisticsDataScope = 0,
            RoleCodes = new[] { "DEPT_EMPLOYEE", "sales_operator" },
            PrimaryDepartmentId = "dept-sales"
        });

        var deptRepo = Substitute.For<IRepository<RbacDepartment>>();
        deptRepo.GetAllAsync().Returns(new List<RbacDepartment>
        {
            new() { Id = "dept-sales", Path = "Root/销售部", Status = 1 }
        });

        var userDeptRepo = Substitute.For<IRepository<RbacUserDepartment>>();
        userDeptRepo.GetAllAsync().Returns(new List<RbacUserDepartment>
        {
            new() { UserId = "king", DepartmentId = "dept-sales", IsPrimary = true },
            new() { UserId = "cecilia", DepartmentId = "dept-sales", IsPrimary = true }
        });

        var userRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
        userRoleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<RbacUserRole, bool>>>())
            .Returns(new List<RbacUserRole>
            {
                new() { UserId = "cecilia", RoleId = "role-emp" }
            });

        var roleRepo = Substitute.For<IRepository<RbacRole>>();
        roleRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<RbacRole, bool>>>())
            .Returns(new List<RbacRole>
            {
                new() { Id = "role-emp", RoleCode = "DEPT_EMPLOYEE", RoleName = "部门员工" }
            });

        var sut = new DataPermissionService(
            rbac,
            deptRepo,
            userDeptRepo,
            userRoleRepo,
            roleRepo,
            Substitute.For<IRepository<RFQ>>(),
            Substitute.For<IRepository<RFQItem>>(),
            Substitute.For<IRepository<CustomerInfo>>(),
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IPurchaseQuoterPoolService>());

        var source = new List<SellOrder>
        {
            new() { Id = "so-king", SalesUserId = "king" },
            new() { Id = "so-cecilia", SalesUserId = "cecilia" }
        };

        var result = await sut.FilterSalesOrdersAsync("king", source);

        Assert.Single(result);
        Assert.Equal("so-king", result[0].Id);
    }
}
