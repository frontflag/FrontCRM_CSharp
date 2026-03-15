# 自动化测试快速启动指南

## 快速开始（3分钟）

### 1. 一键创建测试项目

```powershell
# 进入项目目录
cd "d:\MyProject\FrontCRM_CSharp"

# 运行初始化脚本
.\setup-tests.ps1
```

### 2. 运行测试

```powershell
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test tests/CRM.Core.Tests
dotnet test tests/CRM.IntegrationTests

# 带覆盖率
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 3. 查看结果

```
Test Run Successful.
Total tests: 1
     Passed: 1
 Total time: 2.3456 Seconds
```

---

## 编写第一个测试

### 步骤1: 创建测试类

```csharp
// tests/CRM.Core.Tests/Services/UserServiceTests.cs
using Xunit;
using FluentAssertions;

namespace CRM.Core.Tests.Services
{
    public class UserServiceTests
    {
        [Fact]
        public void TestName_ShouldDoSomething_WhenCondition()
        {
            // Arrange - 准备测试数据
            var input = "test";

            // Act - 执行被测方法
            var result = input.ToUpper();

            // Assert - 验证结果
            result.Should().Be("TEST");
        }
    }
}
```

### 步骤2: 运行测试

```powershell
dotnet test tests/CRM.Core.Tests --filter "FullyQualifiedName~UserServiceTests"
```

---

## 测试类型示例

### 单元测试

```csharp
[Fact]
public void RegisterUser_ShouldHashPassword()
{
    // Arrange
    var service = new UserRegistrationService();
    
    // Act
    var user = service.RegisterUser("test", "password", "test@test.com");
    
    // Assert
    user.PasswordHash.Should().NotBe("password");  // 应该被哈希
    user.Salt.Should().NotBeNullOrEmpty();
}
```

### 参数化测试

```csharp
[Theory]
[InlineData("user1", true)]
[InlineData("ab", false)]      // 太短
[InlineData("", false)]        // 空
public void ValidateUserName_ShouldWork(string userName, bool expected)
{
    // Act
    var result = ValidateUserName(userName);
    
    // Assert
    result.Should().Be(expected);
}
```

### Mock测试

```csharp
[Fact]
public async Task GetUser_ShouldReturnUser_WhenExists()
{
    // Arrange
    var mockRepo = Substitute.For<IUserRepository>();
    mockRepo.GetByIdAsync(Arg.Any<Guid>())
        .Returns(Task.FromResult(new User { UserName = "test" }));
    
    var service = new UserService(mockRepo);
    
    // Act
    var result = await service.GetUserAsync(Guid.NewGuid());
    
    // Assert
    result.Should().NotBeNull();
    result.UserName.Should().Be("test");
}
```

### 数据库集成测试

```csharp
public class DatabaseTests : IClassFixture<DatabaseFixture>
{
    private readonly AppDbContext _db;

    public DatabaseTests(DatabaseFixture fixture)
    {
        _db = fixture.DbContext;
    }

    [Fact]
    public async Task CanCreateUser()
    {
        // Arrange
        var user = new User { UserName = "test", Email = "test@test.com" };
        
        // Act
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        // Assert
        var saved = await _db.Users.FirstOrDefaultAsync(u => u.UserName == "test");
        saved.Should().NotBeNull();
    }
}
```

---

## 常用命令

| 命令 | 说明 |
|------|------|
| `dotnet test` | 运行所有测试 |
| `dotnet test --filter "FullyQualifiedName~UserService"` | 运行特定测试 |
| `dotnet test --filter "Category=Unit"` | 按分类运行 |
| `dotnet test /p:CollectCoverage=true` | 带覆盖率 |
| `dotnet test --logger "console;verbosity=detailed"` | 详细输出 |

---

## 测试结构

```
tests/
├── CRM.Core.Tests/
│   ├── Services/           # 服务层测试
│   │   ├── UserServiceTests.cs
│   │   └── OrderServiceTests.cs
│   ├── Models/             # 模型测试
│   └── Helpers/            # 工具类测试
├── CRM.IntegrationTests/
│   ├── Database/           # 数据库测试
│   └── Repositories/       # 仓储测试
├── CRM.API.Tests/
│   └── Controllers/        # 控制器测试
└── CRM.TestCommon/
    ├── Builders/           # 数据构造器
    ├── Fixtures/           # 测试夹具
    └── Data/               # 种子数据
```

---

## 最佳实践

### ✅ 应该做

- 测试方法名要描述行为：`MethodName_ShouldDoSomething_WhenCondition`
- 一个测试只验证一个概念
- 使用 `FluentAssertions` 提高可读性
- 使用 `Bogus` 生成假数据
- 测试要独立，不要相互依赖

### ❌ 不要做

- 不要测试私有方法（通过公共API测试）
- 不要在测试中使用 `Thread.Sleep`
- 不要测试数据库连接等基础设施
- 不要写重复的测试代码（使用参数化测试）

---

## 下一步

1. 阅读完整计划: [AUTOMATED_TESTING_PLAN.md](AUTOMATED_TESTING_PLAN.md)
2. 编写业务逻辑单元测试
3. 添加集成测试
4. 配置CI/CD

**有问题？查看完整文档或联系开发团队！**
