# 自动化测试实施计划

## 一、测试策略概览

### 测试金字塔

```
       /\
      /  \      E2E测试 (UI测试)
     /----\     少量 - 验证完整业务流程
    /      \
   /--------\   集成测试 (API/DB测试)
  /          \   中等 - 验证组件交互
 /------------\
/              \ 单元测试 (服务/工具类)
---------------- 大量 - 验证业务逻辑
```

### 测试类型分配

| 测试类型 | 占比 | 数量(估算) | 工具 |
|----------|------|-----------|------|
| 单元测试 | 70% | 200+ | xUnit + NSubstitute |
| 集成测试 | 20% | 50+ | xUnit + TestContainers |
| API测试 | 10% | 30+ | xUnit + WebApplicationFactory |

---

## 二、测试项目结构

```
FrontCRM/
├── CRM.Core/                    # 核心领域层
├── CRM.Infrastructure/          # 基础设施层
├── CRM.API/                     # API层
├── CRM.Web/                     # Web前端
└── tests/                       # 测试项目根目录
    ├── CRM.Core.Tests/          # 核心层单元测试
    ├── CRM.Infrastructure.Tests/# 基础设施层测试
    ├── CRM.API.Tests/           # API层集成测试
    ├── CRM.IntegrationTests/    # 端到端集成测试
    └── CRM.TestCommon/          # 测试共享库
        ├── Fixtures/            # 测试夹具
        ├── Helpers/             # 测试辅助类
        ├── Builders/            # 测试数据构造器
        └── Data/                # 测试数据
```

---

## 三、实施步骤

### 步骤1: 创建测试项目 (1-2天)

#### 1.1 创建测试项目

```bash
# 进入解决方案目录
cd "d:\MyProject\FrontCRM_CSharp"

# 创建测试项目目录
mkdir tests
cd tests

# 创建单元测试项目
dotnet new xunit -n CRM.Core.Tests
dotnet new xunit -n CRM.Infrastructure.Tests

# 创建API测试项目
dotnet new xunit -n CRM.API.Tests

# 创建集成测试项目
dotnet new xunit -n CRM.IntegrationTests

# 创建共享库
dotnet new classlib -n CRM.TestCommon
```

#### 1.2 添加NuGet包

```bash
# 所有测试项目都需要
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package FluentAssertions
dotnet add package Bogus          # 假数据生成

# 单元测试项目
dotnet add package NSubstitute    # Mock框架

# 集成测试项目
dotnet add package Testcontainers.PostgreSql
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

#### 1.3 添加项目引用

```bash
# Core.Tests 引用 Core
cd CRM.Core.Tests
dotnet add reference ..\..\CRM.Core\CRM.Core.csproj
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj

# Infrastructure.Tests 引用 Infrastructure
cd ..\CRM.Infrastructure.Tests
dotnet add reference ..\..\CRM.Infrastructure\CRM.Infrastructure.csproj
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj

# API.Tests 引用 API
cd ..\CRM.API.Tests
dotnet add reference ..\..\CRM.API\CRM.API.csproj
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj

# IntegrationTests 引用所有层
cd ..\CRM.IntegrationTests
dotnet add reference ..\..\CRM.API\CRM.API.csproj
dotnet add reference ..\..\CRM.Infrastructure\CRM.Infrastructure.csproj
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj
```

---

### 步骤2: 编写单元测试 (第1周)

#### 2.1 核心服务单元测试

```csharp
// CRM.Core.Tests/Services/UserRegistrationServiceTests.cs
public class UserRegistrationServiceTests
{
    private readonly UserRegistrationService _service;

    public UserRegistrationServiceTests()
    {
        _service = new UserRegistrationService();
    }

    [Fact]
    public void RegisterUser_WithValidInput_ShouldCreateUser()
    {
        // Arrange
        var userName = "testuser";
        var password = "Test123!";
        var email = "test@example.com";

        // Act
        var user = _service.RegisterUser(userName, password, email);

        // Assert
        user.Should().NotBeNull();
        user.UserName.Should().Be(userName);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.Salt.Should().NotBeNullOrEmpty();
        user.PasswordPlain.Should().Be(password); // 测试用途
        user.Status.Should().Be(1);
    }

    [Theory]
    [InlineData("user1", "pass123", "user1@test.com")]
    [InlineData("user2", "password", "user2@test.com")]
    public void RegisterUser_WithDifferentInputs_ShouldWork(
        string userName, string password, string email)
    {
        // Act
        var user = _service.RegisterUser(userName, password, email);

        // Assert
        user.UserName.Should().Be(userName);
        user.PasswordHash.Should().NotBe(password); // 应该被哈希
    }

    [Fact]
    public void ValidateUser_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = _service.RegisterUser("test", "password123", "test@test.com");

        // Act
        var result = _service.ValidateUser(user, "password123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateUser_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var user = _service.RegisterUser("test", "password123", "test@test.com");

        // Act
        var result = _service.ValidateUser(user, "wrongpassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = _service.RegisterUser("test", "oldpass", "test@test.com");
        var oldHash = user.PasswordHash;

        // Act
        _service.ChangePassword(user, "newpass123");

        // Assert
        user.PasswordHash.Should().NotBe(oldHash);
        user.PasswordPlain.Should().Be("newpass123");
        user.PasswordChangeTime.Should().NotBeNull();
    }
}
```

#### 2.2 系统参数服务单元测试

```csharp
// CRM.Core.Tests/Services/SysParamServiceTests.cs
public class SysParamServiceTests
{
    private readonly ISysParamRepository _mockRepo;
    private readonly ISysParamService _service;

    public SysParamServiceTests()
    {
        _mockRepo = Substitute.For<ISysParamRepository>();
        _service = new SysParamService(_mockRepo);
    }

    [Fact]
    public async Task GetStringValueAsync_WithExistingParam_ShouldReturnValue()
    {
        // Arrange
        var param = new SysParam
        {
            ParamCode = "System.CompanyName",
            DataType = ParamDataType.String,
            ValueString = "Test Company"
        };
        _mockRepo.GetByCodeAsync("System.CompanyName")
            .Returns(Task.FromResult<SysParam?>(param));

        // Act
        var result = await _service.GetStringValueAsync("System.CompanyName");

        // Assert
        result.Should().Be("Test Company");
    }

    [Fact]
    public async Task GetStringValueAsync_WithNonExistingParam_ShouldReturnDefault()
    {
        // Arrange
        _mockRepo.GetByCodeAsync("NonExisting")
            .Returns(Task.FromResult<SysParam?>(null));

        // Act
        var result = await _service.GetStringValueAsync("NonExisting", "Default");

        // Assert
        result.Should().Be("Default");
    }

    [Fact]
    public async Task GetIntArrayAsync_WithArrayParam_ShouldReturnList()
    {
        // Arrange
        var param = new SysParam
        {
            ParamCode = "Business.StatusList",
            DataType = ParamDataType.Integer,
            IsArray = true,
            ValueJson = "[0, 1, 2, 3, 4]"
        };
        _mockRepo.GetByCodeAsync("Business.StatusList")
            .Returns(Task.FromResult<SysParam?>(param));

        // Act
        var result = await _service.GetIntArrayAsync("Business.StatusList");

        // Assert
        result.Should().HaveCount(5);
        result.Should().ContainInOrder(0, 1, 2, 3, 4);
    }

    [Fact]
    public async Task SetValueAsync_ShouldUpdateAndSaveHistory()
    {
        // Arrange
        var param = new SysParam
        {
            ParamId = Guid.NewGuid(),
            ParamCode = "System.Name",
            ValueString = "Old Name"
        };
        _mockRepo.GetByCodeAsync("System.Name")
            .Returns(Task.FromResult<SysParam?>(param));

        // Act
        await _service.SetStringValueAsync("System.Name", "New Name", "名称更新");

        // Assert
        await _mockRepo.Received(1).UpdateAsync(param);
        await _mockRepo.Received(1).AddHistoryAsync(
            Arg.Is<SysParamHistory>(h => 
                h.OldValue == "Old Name" && 
                h.NewValue == "New Name" &&
                h.ChangeReason == "名称更新"));
    }
}
```

---

### 步骤3: 编写集成测试 (第2周)

#### 3.1 数据库集成测试

```csharp
// CRM.IntegrationTests/Database/DatabaseTests.cs
public class DatabaseTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly AppDbContext _dbContext;

    public DatabaseTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _dbContext = fixture.DbContext;
    }

    [Fact]
    public async Task CanConnectToDatabase()
    {
        // Act
        var canConnect = await _dbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task CanCreateAndRetrieveUser()
    {
        // Arrange
        var user = new User
        {
            UserId = Guid.NewGuid(),
            UserName = "testuser",
            PasswordHash = "hash",
            Salt = "salt",
            Email = "test@test.com",
            Status = 1,
            CreateTime = DateTime.UtcNow
        };

        // Act
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == "testuser");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.UserName.Should().Be("testuser");
        retrieved.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task CanCreateOrderWithItems()
    {
        // Arrange
        var order = new SellOrder
        {
            SellOrderId = Guid.NewGuid(),
            SellOrderCode = "SO20240001",
            CustomerId = Guid.NewGuid(),
            Total = 10000m,
            TotalUSD = 1388.89m,
            Currency = 1,
            Status = 1,
            CreateTime = DateTime.UtcNow,
            Items = new List<SellOrderItem>
            {
                new()
                {
                    ItemId = Guid.NewGuid(),
                    MaterialId = Guid.NewGuid(),
                    Quantity = 100,
                    UnitPrice = 100m,
                    Amount = 10000m,
                    PriceUSD = 13.89m,
                    AmountUSD = 1388.89m
                }
            }
        };

        // Act
        _dbContext.SellOrders.Add(order);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.SellOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.SellOrderCode == "SO20240001");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Items.Should().HaveCount(1);
        retrieved.Total.Should().Be(10000m);
        retrieved.TotalUSD.Should().Be(1388.89m);
    }
}

// 测试夹具
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    public AppDbContext DbContext { get; private set; } = null!;

    public DatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        DbContext = new AppDbContext(options);
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

#### 3.2 API集成测试

```csharp
// CRM.API.Tests/Controllers/UsersControllerTests.cs
public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 替换为测试数据库
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegisterUser_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            UserName = "newuser",
            Password = "Password123!",
            Email = "new@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        result.Should().NotBeNull();
        result!.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task RegisterUser_WithExistingUserName_ShouldReturnBadRequest()
    {
        // Arrange - 先创建一个用户
        var existingUser = new RegisterUserRequest
        {
            UserName = "existing",
            Password = "Password123!",
            Email = "existing@example.com"
        };
        await _client.PostAsJsonAsync("/api/users/register", existingUser);

        // Act - 尝试创建同名用户
        var response = await _client.PostAsJsonAsync("/api/users/register", existingUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange - 注册用户
        var registerRequest = new RegisterUserRequest
        {
            UserName = "loginuser",
            Password = "Password123!",
            Email = "login@example.com"
        };
        await _client.PostAsJsonAsync("/api/users/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UserName = "loginuser",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }
}
```

---

### 步骤4: 测试数据管理

#### 4.1 测试数据构造器

```csharp
// CRM.TestCommon/Builders/UserBuilder.cs
public class UserBuilder
{
    private readonly User _user;
    private readonly Faker _faker = new("zh_CN");

    public UserBuilder()
    {
        _user = new User
        {
            UserId = Guid.NewGuid(),
            UserName = _faker.Internet.UserName(),
            PasswordHash = "hashedpassword",
            Salt = "saltvalue",
            PasswordPlain = "password123",
            Email = _faker.Internet.Email(),
            RealName = _faker.Name.FullName(),
            Mobile = _faker.Phone.PhoneNumber(),
            Status = 1,
            CreateTime = DateTime.UtcNow
        };
    }

    public UserBuilder WithUserName(string userName)
    {
        _user.UserName = userName;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public UserBuilder WithStatus(short status)
    {
        _user.Status = status;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _user.IsAdmin = true;
        return this;
    }

    public User Build()
    {
        return _user;
    }

    public List<User> BuildList(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new UserBuilder().Build())
            .ToList();
    }
}

// CRM.TestCommon/Builders/OrderBuilder.cs
public class OrderBuilder
{
    private readonly SellOrder _order;
    private readonly Faker _faker = new("zh_CN");

    public OrderBuilder()
    {
        _order = new SellOrder
        {
            SellOrderId = Guid.NewGuid(),
            SellOrderCode = $"SO{DateTime.Now:yyyyMMdd}{_faker.Random.Number(1000, 9999)}",
            CustomerId = Guid.NewGuid(),
            Currency = 1,
            Total = 0,
            TotalUSD = 0,
            Status = 1,
            CreateTime = DateTime.UtcNow,
            Items = new List<SellOrderItem>()
        };
    }

    public OrderBuilder WithCustomer(Guid customerId)
    {
        _order.CustomerId = customerId;
        return this;
    }

    public OrderBuilder WithCurrency(short currency)
    {
        _order.Currency = currency;
        return this;
    }

    public OrderBuilder WithItem(Action<SellOrderItemBuilder> itemConfig)
    {
        var itemBuilder = new SellOrderItemBuilder();
        itemConfig(itemBuilder);
        _order.Items.Add(itemBuilder.Build());
        RecalculateTotal();
        return this;
    }

    public OrderBuilder WithItems(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _order.Items.Add(new SellOrderItemBuilder().Build());
        }
        RecalculateTotal();
        return this;
    }

    private void RecalculateTotal()
    {
        _order.Total = _order.Items.Sum(i => i.Amount);
        _order.TotalUSD = _order.Items.Sum(i => i.AmountUSD);
    }

    public SellOrder Build()
    {
        return _order;
    }
}
```

#### 4.2 种子数据

```csharp
// CRM.TestCommon/Data/SeedData.cs
public static class SeedData
{
    public static async Task SeedUsersAsync(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new UserBuilder().WithUserName("admin").AsAdmin().Build(),
                new UserBuilder().WithUserName("user1").Build(),
                new UserBuilder().WithUserName("user2").Build(),
                new UserBuilder().WithUserName("inactive").WithStatus(0).Build()
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedOrdersAsync(AppDbContext context)
    {
        if (!context.SellOrders.Any())
        {
            var orders = new OrderBuilder()
                .WithItems(3)
                .Build();

            await context.SellOrders.AddAsync(orders);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedParamsAsync(AppDbContext context)
    {
        if (!context.SysParams.Any())
        {
            var groups = new List<SysParamGroup>
            {
                new() { GroupId = Guid.NewGuid(), GroupCode = "TEST", GroupName = "测试分组", Level = 1 }
            };

            var @params = new List<SysParam>
            {
                new()
                {
                    ParamId = Guid.NewGuid(),
                    ParamCode = "Test.String",
                    ParamName = "测试字符串",
                    DataType = ParamDataType.String,
                    ValueString = "Test Value",
                    Status = 1
                },
                new()
                {
                    ParamId = Guid.NewGuid(),
                    ParamCode = "Test.Int",
                    ParamName = "测试整数",
                    DataType = ParamDataType.Integer,
                    ValueInt = 100,
                    Status = 1
                }
            };

            await context.SysParamGroups.AddRangeAsync(groups);
            await context.SysParams.AddRangeAsync(@params);
            await context.SaveChangesAsync();
        }
    }
}
```

---

### 步骤5: CI/CD配置

#### 5.1 GitHub Actions工作流

```yaml
# .github/workflows/dotnet-test.yml
name: .NET CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: testdb
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore --configuration Release

    # 运行单元测试
    - name: Run Unit Tests
      run: dotnet test tests/CRM.Core.Tests --no-build --verbosity normal

    # 运行集成测试
    - name: Run Integration Tests
      env:
        ConnectionStrings__DefaultConnection: "Host=localhost;Database=testdb;Username=postgres;Password=postgres"
      run: dotnet test tests/CRM.IntegrationTests --no-build --verbosity normal

    # 运行API测试
    - name: Run API Tests
      run: dotnet test tests/CRM.API.Tests --no-build --verbosity normal

    # 生成测试报告
    - name: Generate Test Report
      run: |
        dotnet test --logger "trx;LogFileName=test-results.trx" \
                   --results-directory ./TestResults

    - name: Upload Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results
        path: ./TestResults

    # 代码覆盖率
    - name: Test with Coverage
      run: |
        dotnet test /p:CollectCoverage=true \
                   /p:CoverletOutputFormat=cobertura \
                   /p:CoverletOutput=./coverage.xml

    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage.xml
        fail_ci_if_error: false
```

#### 5.2 测试脚本

```powershell
# run-tests.ps1
param(
    [string]$Filter = "",
    [switch]$Coverage,
    [switch]$Integration
)

$testProjects = @(
    "tests\CRM.Core.Tests",
    "tests\CRM.Infrastructure.Tests"
)

if ($Integration) {
    $testProjects += "tests\CRM.IntegrationTests"
    $testProjects += "tests\CRM.API.Tests"
}

foreach ($project in $testProjects) {
    Write-Host "Testing $project..." -ForegroundColor Cyan
    
    if ($Coverage) {
        dotnet test $project --collect:"XPlat Code Coverage" --verbosity normal
    } else {
        dotnet test $project --verbosity normal
    }
}
```

---

## 四、实施时间表

| 阶段 | 任务 | 预计时间 | 产出 |
|------|------|---------|------|
| **第1周** | 测试框架搭建 | 2天 | 测试项目结构、基础配置 |
| | 核心服务单元测试 | 3天 | 50+ 单元测试 |
| **第2周** | 集成测试开发 | 3天 | 20+ 集成测试 |
| | API测试开发 | 2天 | 15+ API测试 |
| **第3周** | 测试数据完善 | 2天 | 测试数据构造器 |
| | CI/CD配置 | 2天 | GitHub Actions工作流 |
| | 文档完善 | 1天 | 测试文档 |
| **第4周** | 测试覆盖率提升 | 3天 | 达到70%+覆盖率 |
| | 性能测试 | 2天 | 性能基准测试 |

---

## 五、质量指标

| 指标 | 目标值 | 当前值 |
|------|--------|--------|
| 代码覆盖率 | >= 70% | 0% |
| 单元测试数量 | >= 200 | 0 |
| 集成测试数量 | >= 50 | 0 |
| API测试数量 | >= 30 | 0 |
| 测试通过率 | >= 95% | - |
| CI构建时间 | <= 10分钟 | - |

---

## 六、生成的文件

| 文件 | 说明 |
|------|------|
| `tests/CRM.Core.Tests/` | 核心层单元测试 |
| `tests/CRM.IntegrationTests/` | 集成测试 |
| `tests/CRM.API.Tests/` | API测试 |
| `tests/CRM.TestCommon/` | 测试共享库 |
| `.github/workflows/dotnet-test.yml` | CI工作流 |
| `AUTOMATED_TESTING_PLAN.md` | 本文档 |

---

## 七、下一步行动

1. ✅ **创建测试项目结构**
2. ⏳ **编写第一个单元测试**
3. ⏳ **配置测试数据库**
4. ⏳ **运行测试并查看覆盖率**
5. ⏳ **集成到CI/CD**

**准备好开始实施了吗？我可以帮您创建第一个测试项目！**
