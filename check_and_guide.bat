@echo off
chcp 65001 >nul
echo ========================================
echo  在 FrontCRM 数据库中创建 EBS 业务表
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务状态...
sc query postgresql-x64-16 | findstr "STATE"
echo.

echo 2. 检查端口 5432...
netstat -an | findstr ":5432" | findstr "LISTENING"
echo.

echo 3. 检查 SQL 文件...
if exist "ebs_core_tables.sql" (
    echo  ✅ SQL 文件存在: ebs_core_tables.sql
    for /f "tokens=2" %%a in ('find /c "CREATE TABLE" ebs_core_tables.sql') do (
        echo  ✅ 包含 %%a 个表创建语句
    )
) else (
    echo  ❌ SQL 文件不存在
    goto :end
)
echo.

echo 4. 创建执行指南...
echo.
echo ========================================
echo 执行方法（选择其一）:
echo ========================================
echo.
echo 方法 A: 使用 pgAdmin
echo   1. 打开 pgAdmin
echo   2. 连接到 localhost:5432
echo   3. 用户名: postgres, 密码: 1234
echo   4. 选择 FrontCRM 数据库
echo   5. 打开查询工具
echo   6. 执行 ebs_core_tables.sql
echo.
echo 方法 B: 安装 psql 命令行工具后执行:
echo   1. 下载 PostgreSQL (含命令行工具)
echo   2. 打开命令提示符执行:
echo      set PGPASSWORD=1234
echo      psql -h localhost -p 5432 -U postgres -d FrontCRM -f ebs_core_tables.sql
echo.
echo 方法 C: 使用 DBeaver
echo   1. 下载 DBeaver (dbeaver.io)
echo   2. 新建 PostgreSQL 连接
echo   3. 执行 SQL 文件
echo.
echo ========================================
echo 验证执行结果:
echo ========================================
echo.
echo 执行成功后，运行以下 SQL 验证:
echo.
echo SELECT table_name FROM information_schema.tables 
echo WHERE table_schema = 'public' ORDER BY table_name;
echo.
echo 应该看到以下表:
echo - customerinfo
echo - customercontactinfo
echo - customeraddress
echo - vendorinfo
echo - vendorcontactinfo
echo - sellorder
echo - purchaseorder
echo - material
echo.
echo ========================================
echo 已准备好所有文件:
echo - ebs_core_tables.sql (SQL脚本)
echo - create_ebs_tables.bat (执行指导)
echo - 此指南
echo ========================================
echo.

:end
echo 按任意键查看 SQL 文件前几行...
pause >nul
echo.
echo SQL 文件前 20 行:
echo ========================================
type ebs_core_tables.sql | head -20
echo ========================================
echo.
pause