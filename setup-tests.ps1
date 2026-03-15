# 自动化测试项目初始化脚本
# 运行此脚本创建完整的测试项目结构

param(
    [switch]$SkipRestore,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CRM Automated Testing Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot
$testsDir = Join-Path $projectRoot "tests"

# 1. 创建测试目录结构
Write-Host "[1/6] Creating test project structure..." -ForegroundColor Yellow

if (Test-Path $testsDir) {
    Write-Host "      Tests directory already exists" -ForegroundColor Gray
} else {
    New-Item -ItemType Directory -Path $testsDir | Out-Null
    Write-Host "      Created: tests/" -ForegroundColor Green
}

Set-Location $testsDir

# 2. 创建测试项目
Write-Host "[2/6] Creating test projects..." -ForegroundColor Yellow

$projects = @(
    @{ Name = "CRM.Core.Tests"; Type = "xunit" },
    @{ Name = "CRM.Infrastructure.Tests"; Type = "xunit" },
    @{ Name = "CRM.API.Tests"; Type = "xunit" },
    @{ Name = "CRM.IntegrationTests"; Type = "xunit" },
    @{ Name = "CRM.TestCommon"; Type = "classlib" }
)

foreach ($proj in $projects) {
    if (Test-Path $proj.Name) {
        Write-Host "      Project exists: $($proj.Name)" -ForegroundColor Gray
    } else {
        if ($proj.Type -eq "xunit") {
            dotnet new xunit -n $proj.Name --no-restore
        } else {
            dotnet new classlib -n $proj.Name --no-restore
        }
        Write-Host "      Created: $($proj.Name)" -ForegroundColor Green
    }
}

# 3. 添加NuGet包
Write-Host "[3/6] Adding NuGet packages..." -ForegroundColor Yellow

$commonPackages = @(
    "xunit",
    "xunit.runner.visualstudio",
    "Microsoft.NET.Test.Sdk",
    "FluentAssertions",
    "Bogus",
    "coverlet.collector"
)

$mockPackages = @(
    "NSubstitute",
    "NSubstitute.Analyzers.CSharp"
)

$integrationPackages = @(
    "Testcontainers.PostgreSql",
    "Microsoft.AspNetCore.Mvc.Testing"
)

foreach ($proj in @("CRM.Core.Tests", "CRM.Infrastructure.Tests", "CRM.API.Tests", "CRM.IntegrationTests")) {
    Write-Host "      Configuring $proj..." -ForegroundColor Gray
    
    Set-Location $proj
    
    # 添加公共包
    foreach ($pkg in $commonPackages) {
        dotnet add package $pkg --no-restore 2>$null
    }
    
    # 根据项目类型添加特定包
    if ($proj -in @("CRM.Core.Tests", "CRM.Infrastructure.Tests")) {
        foreach ($pkg in $mockPackages) {
            dotnet add package $pkg --no-restore 2>$null
        }
    }
    
    if ($proj -eq "CRM.IntegrationTests") {
        foreach ($pkg in $integrationPackages) {
            dotnet add package $pkg --no-restore 2>$null
        }
    }
    
    Set-Location ..
}

# 4. 添加项目引用
Write-Host "[4/6] Adding project references..." -ForegroundColor Yellow

# CRM.Core.Tests -> CRM.Core
Set-Location "CRM.Core.Tests"
dotnet add reference ..\..\CRM.Core\CRM.Core.csproj 2>$null
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj 2>$null
Set-Location ..

# CRM.Infrastructure.Tests -> CRM.Infrastructure
Set-Location "CRM.Infrastructure.Tests"
dotnet add reference ..\..\CRM.Infrastructure\CRM.Infrastructure.csproj 2>$null
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj 2>$null
Set-Location ..

# CRM.API.Tests -> CRM.API
Set-Location "CRM.API.Tests"
dotnet add reference ..\..\CRM.API\CRM.API.csproj 2>$null
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj 2>$null
Set-Location ..

# CRM.IntegrationTests -> 所有层
Set-Location "CRM.IntegrationTests"
dotnet add reference ..\..\CRM.API\CRM.API.csproj 2>$null
dotnet add reference ..\..\CRM.Infrastructure\CRM.Infrastructure.csproj 2>$null
dotnet add reference ..\CRM.TestCommon\CRM.TestCommon.csproj 2>$null
Set-Location ..

# 5. 创建示例测试文件
Write-Host "[5/6] Creating sample test files..." -ForegroundColor Yellow

# 示例单元测试
$unitTestContent = @'
using Xunit;
using FluentAssertions;

namespace CRM.Core.Tests.Services
{
    public class SampleTests
    {
        [Fact]
        public void Sample_Test_ShouldPass()
        {
            // Arrange
            var value = true;

            // Act & Assert
            value.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 3, 5)]
        [InlineData(10, 20, 30)]
        public void Addition_ShouldWork(int a, int b, int expected)
        {
            // Act
            var result = a + b;

            // Assert
            result.Should().Be(expected);
        }
    }
}
'@

$unitTestPath = "CRM.Core.Tests/Services/SampleTests.cs"
if (-not (Test-Path $unitTestPath)) {
    New-Item -ItemType File -Path $unitTestPath -Value $unitTestContent -Force | Out-Null
    Write-Host "      Created: SampleTests.cs" -ForegroundColor Green
}

# 测试辅助类
$testCommonContent = @'
using Bogus;

namespace CRM.TestCommon.Builders
{
    /// <summary>
    /// 用户数据构造器
    /// </summary>
    public class UserBuilder
    {
        private readonly Faker _faker = new("zh_CN");
        
        public string GenerateUserName() => _faker.Internet.UserName();
        public string GenerateEmail() => _faker.Internet.Email();
        public string GeneratePhone() => _faker.Phone.PhoneNumber();
    }
}
'@

$builderPath = "CRM.TestCommon/Builders/UserBuilder.cs"
if (-not (Test-Path $builderPath)) {
    New-Item -ItemType File -Path $builderPath -Value $testCommonContent -Force | Out-Null
    Write-Host "      Created: UserBuilder.cs" -ForegroundColor Green
}

# 6. 添加到解决方案
Write-Host "[6/6] Adding projects to solution..." -ForegroundColor Yellow

Set-Location $projectRoot

foreach ($proj in $projects) {
    $projFile = "tests\$($proj.Name)\$($proj.Name).csproj"
    if (Test-Path $projFile) {
        dotnet sln add $projFile 2>$null
        Write-Host "      Added to solution: $($proj.Name)" -ForegroundColor Green
    }
}

# 恢复和构建
if (-not $SkipRestore) {
    Write-Host ""
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore
}

if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build --no-restore
}

# 运行测试
Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test tests/CRM.Core.Tests --no-build --verbosity minimal

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Projects Created:" -ForegroundColor White
Write-Host "  - tests/CRM.Core.Tests" -ForegroundColor Gray
Write-Host "  - tests/CRM.Infrastructure.Tests" -ForegroundColor Gray
Write-Host "  - tests/CRM.API.Tests" -ForegroundColor Gray
Write-Host "  - tests/CRM.IntegrationTests" -ForegroundColor Gray
Write-Host "  - tests/CRM.TestCommon" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Write your first unit test in CRM.Core.Tests" -ForegroundColor White
Write-Host "  2. Run tests: dotnet test" -ForegroundColor White
Write-Host "  3. Check coverage: dotnet test /p:CollectCoverage=true" -ForegroundColor White
Write-Host ""
