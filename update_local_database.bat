@echo off
chcp 65001 >nul
echo ========================================
echo  更新本地 FrontCRM 数据库
echo  添加财务表和日志表
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务...
sc query postgresql-x64-16 | findstr "STATE"
if %errorlevel% neq 0 (
    echo [错误] PostgreSQL 服务未运行
    pause
    exit /b 1
)
echo [OK] PostgreSQL 服务正在运行
echo.

echo 2. 准备执行的 SQL 文件...
echo    - ebs_new_tables.sql (财务表 + 日志表)
echo.

echo 3. 执行 SQL 脚本...
where psql >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] 找到 psql 命令
echo.
    echo 正在连接数据库并执行...
    echo    主机: localhost:5432
    echo    数据库: FrontCRM
    echo    用户: postgres
    echo.
    
    set PGPASSWORD=1234
    psql -h localhost -p 5432 -U postgres -d FrontCRM -f ebs_new_tables.sql
    
    if %errorlevel% equ 0 (
        echo.
        echo [成功] SQL 脚本执行完成！
    ) else (
        echo.
        echo [错误] SQL 执行失败，错误码: %errorlevel%
    )
) else (
    echo [提示] 未找到 psql 命令
echo.
    echo 请使用以下方法之一执行 SQL:
echo.
    echo 方法 1: 使用 pgAdmin
    echo    1. 打开 pgAdmin
    echo    2. 连接到 localhost:5432 / FrontCRM
    echo    3. 打开查询工具
    echo    4. 执行文件: ebs_new_tables.sql
    echo.
    echo 方法 2: 安装 PostgreSQL 命令行工具
    echo    下载地址: https://www.postgresql.org/download/
    echo.
    echo 方法 3: 使用 DBeaver
    echo    1. 打开 DBeaver
    echo    2. 连接到 FrontCRM 数据库
    echo    3. 执行 SQL 文件: ebs_new_tables.sql
)

echo.
echo ========================================
echo  新表清单:
echo ========================================
echo  财务模块:
echo    - invoice (发票主表)
echo    - invoiceitem (发票明细表)
echo    - payment (付款单主表)
echo    - paymentitem (付款单明细表)
echo    - receipt (收款单主表)
echo    - receiptitem (收款单明细表)
echo.
echo  系统模块:
echo    - businesslog (业务日志主表)
echo    - businesslogdetail (业务日志明细表)
echo ========================================
echo.
pause