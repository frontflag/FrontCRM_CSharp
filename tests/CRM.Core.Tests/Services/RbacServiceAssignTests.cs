using CRM.Core.Interfaces;
using CRM.Core.Models.Rbac;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public sealed class RbacServiceAssignTests
{
    [Fact]
    public async Task AssignUserRolesAsync_soft_deletes_extra_keeps_desired()
    {
        var roles = new MemoryRepository<RbacUserRole>();
        await roles.AddAsync(new RbacUserRole { Id = "ur-keep", UserId = "u1", RoleId = "r1" });
        await roles.AddAsync(new RbacUserRole { Id = "ur-drop", UserId = "u1", RoleId = "r2" });
        var sut = CreateSut(roles);

        await sut.AssignUserRolesAsync("u1", new[] { "r1" });

        var snap = roles.Snapshot();
        Assert.False(snap.Single(x => x.Id == "ur-keep").IsDeleted);
        Assert.True(snap.Single(x => x.Id == "ur-drop").IsDeleted);
    }

    [Fact]
    public async Task AssignUserRolesAsync_revives_soft_deleted_pair_instead_of_insert()
    {
        var roles = new MemoryRepository<RbacUserRole>();
        await roles.AddAsync(new RbacUserRole
        {
            Id = "ur-dead",
            UserId = "u1",
            RoleId = "r1",
            IsDeleted = true
        });
        var sut = CreateSut(roles);

        await sut.AssignUserRolesAsync("u1", new[] { "r1" });

        var snap = roles.Snapshot();
        Assert.Single(snap);
        Assert.Equal("ur-dead", snap[0].Id);
        Assert.False(snap[0].IsDeleted);
    }

    [Fact]
    public async Task AssignUserRolesAsync_inserts_missing_pair()
    {
        var roles = new MemoryRepository<RbacUserRole>();
        var sut = CreateSut(roles);

        await sut.AssignUserRolesAsync("u1", new[] { "r1" });

        var live = roles.Snapshot().Where(x => !x.IsDeleted).ToList();
        Assert.Single(live);
        Assert.Equal("u1", live[0].UserId);
        Assert.Equal("r1", live[0].RoleId);
    }

    [Fact]
    public async Task AssignUserDepartmentsAsync_revives_and_sets_primary()
    {
        var depts = new MemoryRepository<RbacUserDepartment>();
        await depts.AddAsync(new RbacUserDepartment
        {
            Id = "ud-dead",
            UserId = "u1",
            DepartmentId = "d1",
            IsPrimary = false,
            IsDeleted = true
        });
        var sut = CreateSut(depts: depts);

        await sut.AssignUserDepartmentsAsync("u1", new[] { "d1" }, "d1");

        var row = Assert.Single(depts.Snapshot());
        Assert.False(row.IsDeleted);
        Assert.True(row.IsPrimary);
        Assert.Equal("ud-dead", row.Id);
    }

    [Fact]
    public async Task AssignRolePermissionsAsync_revives_soft_deleted_pair()
    {
        var perms = new MemoryRepository<RbacRolePermission>();
        await perms.AddAsync(new RbacRolePermission
        {
            Id = "rp-dead",
            RoleId = "role-1",
            PermissionId = "p1",
            IsDeleted = true
        });
        var sut = CreateSut(perms: perms);

        await sut.AssignRolePermissionsAsync("role-1", new[] { "p1" });

        var row = Assert.Single(perms.Snapshot());
        Assert.Equal("rp-dead", row.Id);
        Assert.False(row.IsDeleted);
    }

    private static RbacService CreateSut(
        MemoryRepository<RbacUserRole>? roles = null,
        MemoryRepository<RbacUserDepartment>? depts = null,
        MemoryRepository<RbacRolePermission>? perms = null) =>
        new(
            Substitute.For<IRepository<RbacRole>>(),
            Substitute.For<IRepository<RbacPermission>>(),
            Substitute.For<IRepository<RbacDepartment>>(),
            roles ?? new MemoryRepository<RbacUserRole>(),
            depts ?? new MemoryRepository<RbacUserDepartment>(),
            perms ?? new MemoryRepository<RbacRolePermission>(),
            Substitute.For<IUnitOfWork>());
}
