using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.System;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public sealed class UserLevelServiceTests
{
    [Fact]
    public async Task ChangeAsync_when_level_changes_writes_history_with_account_snapshot()
    {
        var (sut, users, history) = CreateSut();
        await users.AddAsync(new User
        {
            Id = "u1",
            UserName = "alice",
            RealName = "Alice",
            Level = 1,
            PasswordHash = "x",
            Salt = "s"
        });
        await users.AddAsync(new User
        {
            Id = "op1",
            UserName = "admin",
            RealName = "管理员",
            Level = 1,
            PasswordHash = "x",
            Salt = "s"
        });

        var result = await sut.ChangeAsync("u1", 3, "晋级", "op1");

        Assert.True(result.LevelChanged);
        Assert.Equal((short)3, result.Level);
        Assert.NotNull(result.LevelChangedAt);
        var row = Assert.Single(history.Snapshot());
        Assert.Equal("u1", row.UserId);
        Assert.Equal("alice", row.UserName);
        Assert.Equal((short)1, row.OldLevel);
        Assert.Equal((short)3, row.NewLevel);
        Assert.Equal("晋级", row.Remark);
        Assert.Equal("op1", row.OperatorUserId);
        Assert.Equal("管理员", row.OperatorUserName);
        var target = (await users.GetByIdAsync("u1"))!;
        Assert.Equal((short)3, target.Level);
        target.UserName = "alice-renamed";
        await users.UpdateAsync(target);
        var historyAfterRename = Assert.Single(history.Snapshot());
        Assert.Equal("alice", historyAfterRename.UserName);
    }

    [Fact]
    public async Task ChangeAsync_same_level_updates_remark_without_history()
    {
        var (sut, users, history) = CreateSut();
        await users.AddAsync(new User
        {
            Id = "u1",
            UserName = "alice",
            Level = 2,
            LevelRemark = "旧",
            PasswordHash = "x",
            Salt = "s"
        });

        var result = await sut.ChangeAsync("u1", 2, "仅备注", "op1");

        Assert.False(result.LevelChanged);
        Assert.Empty(history.Snapshot());
        Assert.Equal("仅备注", (await users.GetByIdAsync("u1"))!.LevelRemark);
        Assert.Null((await users.GetByIdAsync("u1"))!.LevelChangedAt);
    }

    [Fact]
    public async Task ChangeAsync_rejects_out_of_range()
    {
        var (sut, users, _) = CreateSut();
        await users.AddAsync(new User { Id = "u1", UserName = "a", Level = 1, PasswordHash = "x", Salt = "s" });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ChangeAsync("u1", 0, null, "op"));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.ChangeAsync("u1", 21, null, "op"));
        Assert.Equal(UserLevelCode.Default, (await users.GetByIdAsync("u1"))!.Level);
    }

    [Fact]
    public async Task GetHistoryAsync_orders_newest_first()
    {
        var (sut, _, history) = CreateSut();
        await history.AddAsync(new UserLevelHistory
        {
            Id = "h1",
            UserId = "u1",
            UserName = "alice",
            OldLevel = 1,
            NewLevel = 2,
            ChangeTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await history.AddAsync(new UserLevelHistory
        {
            Id = "h2",
            UserId = "u1",
            UserName = "alice",
            OldLevel = 2,
            NewLevel = 4,
            ChangeTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var rows = await sut.GetHistoryAsync("u1");
        Assert.Equal(new[] { "h2", "h1" }, rows.Select(x => x.Id).ToArray());
    }

    private static (UserLevelService sut, MemoryRepository<User> users, MemoryRepository<UserLevelHistory> history) CreateSut()
    {
        var users = new MemoryRepository<User>();
        var history = new MemoryRepository<UserLevelHistory>();
        var userService = Substitute.For<IUserService>();
        userService.GetByIdForAdminAsync(Arg.Any<string>())
            .Returns(ci => users.GetByIdAsync(ci.Arg<string>()));
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync().Returns(1);
        var sut = new UserLevelService(userService, users, history, uow);
        return (sut, users, history);
    }
}
