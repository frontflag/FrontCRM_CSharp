@echo off
echo ========================================
echo FrontCRM 数据库设置工具
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务...
sc query postgresql-x64-16 | findstr "RUNNING"
if %errorlevel% equ 0 (
    echo   [OK] PostgreSQL 服务正在运行
) else (
    echo   [ERROR] PostgreSQL 服务未运行
    echo   请启动 PostgreSQL 服务
    pause
    exit /b 1
)

echo.
echo 2. 更新项目连接字符串...
echo   当前配置的密码: 1234
echo.

echo 3. 检查数据库连接...
echo   请手动执行以下操作:
echo.
echo   A. 使用 pgAdmin 连接:
echo      - 主机: localhost
echo      - 端口: 5432
echo      - 用户名: postgres
echo      - 密码: 尝试 1234, postgres, postgres123, 空密码
echo.
echo   B. 创建数据库 (如果不存在):
echo      CREATE DATABASE "FrontCRM";
echo.
echo   C. 验证连接:
echo      Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=正确的密码
echo.

echo 4. 项目文件已更新:
echo   文件: CRM.API\appsettings.json
echo   密码已设置为: 1234
echo.
echo   文件: CRM.API\publish\appsettings.json
echo   密码已设置为: 1234
echo.

echo 5. 如果需要修改密码，请编辑:
echo   1. CRM.API\appsettings.json
echo   2. CRM.API\publish\appsettings.json
echo   3. frontcrm_deploy\docker-compose.yml (Docker 容器密码)
echo.

echo ========================================
echo 操作完成
echo ========================================
echo.
echo 请测试连接并告诉我:
echo 1. 正确的 PostgreSQL 密码是什么?
echo 2. FrontCRM 数据库是否已创建成功?
echo.
pause