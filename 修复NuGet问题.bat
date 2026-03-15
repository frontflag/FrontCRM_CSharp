@echo off
echo ================================================
echo FrontCRM 项目 NuGet 包下载问题修复工具
echo ================================================
echo.

echo 1. 检查 .NET SDK 版本...
dotnet --version
if %errorlevel% neq 0 (
    echo 错误: .NET SDK 未安装或未在 PATH 中
    echo 请安装 .NET 9.0 SDK: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo.
echo 2. 配置国内镜像源...
echo.
echo 已创建 nuget.config 文件，配置了以下镜像源：
echo   - 腾讯云: https://mirrors.cloud.tencent.com/nuget/
echo   - 阿里云: https://nuget.alicdn.com/nuget/index.json
echo.

echo 3. 设置全局 NuGet 配置...
dotnet nuget add source https://mirrors.cloud.tencent.com/nuget/ -n tencent
dotnet nuget add source https://nuget.alicdn.com/nuget/index.json -n aliyun
echo.

echo 4. 检查项目依赖...
cd /d "d:\MyProject\FrontCRM_CSharp\CRM.API"
echo 项目目录: %cd%
echo.

echo 5. 尝试恢复 NuGet 包（使用国内镜像）...
echo.
echo 方法 A: 使用特定源
dotnet restore --source https://mirrors.cloud.tencent.com/nuget/ --source https://nuget.alicdn.com/nuget/index.json --no-cache
echo.

if %errorlevel% neq 0 (
    echo 方法 A 失败，尝试方法 B...
    echo.
    echo 方法 B: 使用代理环境变量
    set https_proxy=http://127.0.0.1:7890
    set http_proxy=http://127.0.0.1:7890
    dotnet restore --no-cache
    if %errorlevel% eq 0 (
        echo 使用代理成功！
    ) else (
        echo 方法 B 也失败。
    )
)

echo.
echo 6. 备用方案：离线包下载...
echo.
echo 如果在线下载失败，可以：
echo 1. 在其他网络正常的机器下载包
echo 2. 手动下载以下包：
echo    - Microsoft.AspNetCore.Authentication.JwtBearer 9.0.11
echo    - Microsoft.EntityFrameworkCore 9.0.11
echo    - Npgsql.EntityFrameworkCore.PostgreSQL 9.0.3
echo    - Serilog.AspNetCore 10.0.0
echo    - System.IdentityModel.Tokens.Jwt 8.8.0
echo.
echo 下载地址: https://www.nuget.org/packages/
echo 将下载的 .nupkg 文件放到: C:\Users\%USERNAME%\.nuget\packages\
echo.

echo 7. 测试构建...
dotnet build --no-restore
if %errorlevel% eq 0 (
    echo ✅ 构建成功！
    echo.
    echo 项目已准备好，可以：
    echo 1. 在本地编译后端
    echo 2. 部署到服务器
) else (
    echo ❌ 构建失败。
    echo 请检查以上错误信息。
)

echo.
echo ================================================
echo 修复完成！
echo ================================================
pause