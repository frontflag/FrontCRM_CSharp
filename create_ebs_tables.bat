@echo off
echo ========================================
echo 在本地 FrontCRM 数据库中创建 EBS 业务表
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
echo 2. 检查数据库连接...
echo   数据库: FrontCRM
echo   用户: postgres
echo   密码: 1234
echo.

echo 3. 创建 EBS 业务表...
echo   将创建以下核心表:
echo   - customerinfo (客户主表)
echo   - customercontactinfo (客户联系人表)
echo   - customeraddress (客户地址表)
echo   - vendorinfo (供应商主表)
echo   - vendorcontactinfo (供应商联系人表)
echo   - sellorder (销售订单主表)
echo   - purchaseorder (采购订单主表)
echo   - material (物料主表)
echo.

echo 4. 执行 SQL 脚本...
echo   请使用以下命令手动执行:
echo.
echo   set PGPASSWORD=1234
echo   psql -h localhost -p 5432 -U postgres -d FrontCRM -f ebs_core_tables.sql
echo.

echo 5. 如果 psql 不可用，请使用 pgAdmin 执行:
echo   a. 打开 pgAdmin
echo   b. 连接到 localhost:5432
echo   c. 选择 FrontCRM 数据库
echo   d. 打开查询工具
echo   e. 复制 ebs_core_tables.sql 内容并执行
echo.

echo ========================================
echo SQL 文件已准备: ebs_core_tables.sql
echo 请按照上述步骤执行
echo ========================================
pause