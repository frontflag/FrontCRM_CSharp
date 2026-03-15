@echo off
echo ========================================
echo FrontCRM 数据库配置修复工具
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务状态...
sc query postgresql-x64-16 | findstr "RUNNING"
if %errorlevel% equ 0 (
    echo   [OK] PostgreSQL 服务正在运行
) else (
    echo   [ERROR] PostgreSQL 服务未运行
    pause
    exit /b 1
)

echo.
echo 2. 尝试连接到 PostgreSQL...
echo   请手动测试连接，然后告诉我:
echo   - 正确的密码是什么？
echo   - 是否已创建 FrontCRM 数据库？
echo.

echo 3. 当前项目配置:
echo   - appsettings.json 中的连接字符串:
type "CRM.API\appsettings.json" | findstr "DefaultConnection"
echo.

echo 4. 建议的操作:
echo   A. 如果知道正确密码，请修改连接字符串
echo   B. 如果不知道密码，请使用 pgAdmin 检查
echo   C. 如果数据库不存在，请手动创建
echo.

echo 5. 手动创建数据库的 SQL:
echo   CREATE DATABASE "FrontCRM";
echo   WITH OWNER = postgres
echo   ENCODING = 'UTF8'
echo   LC_COLLATE = 'Chinese (Simplified)_China.936'
echo   LC_CTYPE = 'Chinese (Simplified)_China.936';
echo.

echo ========================================
echo 请告诉我:
echo 1. 正确的 PostgreSQL 密码
echo 2. FrontCRM 数据库是否已存在
echo ========================================
pause