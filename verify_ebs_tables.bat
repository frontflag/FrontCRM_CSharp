@echo off
chcp 65001 >nul
echo ========================================
echo  验证 FrontCRM 数据库中的 EBS 业务表
echo ========================================
echo.

echo 1. 检查 PostgreSQL 连接...
sc query postgresql-x64-16 | findstr "STATE"
echo.

echo 2. 创建验证 SQL 文件...
(
echo -- 验证 EBS 业务表创建情况
echo SELECT '=== EBS 业务表验证 ===' AS title;
echo.
echo -- 1. 检查所有表
echo SELECT 
echo     COUNT(*) as "总表数",
echo     STRING_AGG(table_name, ', ' ORDER BY table_name) as "表列表"
echo FROM information_schema.tables 
echo WHERE table_schema = 'public';
echo.
echo -- 2. 检查 EBS 核心表
echo SELECT 
echo     table_name as "表名",
echo     CASE 
echo         WHEN table_name IN ('customerinfo', 'customercontactinfo', 'customeraddress',
echo                             'vendorinfo', 'vendorcontactinfo',
echo                             'sellorder', 'purchaseorder', 'material') 
echo         THEN '✅ EBS 核心表'
echo         ELSE '其他表'
echo     END as "类型"
echo FROM information_schema.tables 
echo WHERE table_schema = 'public'
echo ORDER BY "类型" DESC, table_name;
echo.
echo -- 3. 检查表结构（前3个表）
echo SELECT '=== 表结构示例 ===' AS title;
echo.
echo -- customerinfo 表结构
echo SELECT 
echo     column_name as "列名",
echo     data_type as "数据类型",
echo     is_nullable as "可空"
echo FROM information_schema.columns 
echo WHERE table_schema = 'public' AND table_name = 'customerinfo'
echo ORDER BY ordinal_position
echo LIMIT 5;
echo.
echo -- customercontactinfo 表结构
echo SELECT 
echo     column_name as "列名",
echo     data_type as "数据类型", 
echo     is_nullable as "可空"
echo FROM information_schema.columns 
echo WHERE table_schema = 'public' AND table_name = 'customercontactinfo'
echo ORDER BY ordinal_position
echo LIMIT 5;
echo.
echo -- 4. 检查索引
echo SELECT '=== 索引检查 ===' AS title;
echo SELECT 
echo     tablename as "表名",
echo     COUNT(*) as "索引数"
echo FROM pg_indexes 
echo WHERE schemaname = 'public' 
echo     AND tablename IN ('customerinfo', 'customercontactinfo', 'customeraddress',
echo                        'vendorinfo', 'vendorcontactinfo',
echo                        'sellorder', 'purchaseorder', 'material')
echo GROUP BY tablename
echo ORDER BY tablename;
echo.
echo -- 5. 总结
echo SELECT '=== 验证结果 ===' AS title;
echo SELECT 
echo     COUNT(*) as "EBS表总数",
echo     SUM(CASE WHEN table_name IN ('customerinfo', 'customercontactinfo', 'customeraddress',
echo                                   'vendorinfo', 'vendorcontactinfo',
echo                                   'sellorder', 'purchaseorder', 'material') THEN 1 ELSE 0 END) as "已创建EBS表数",
echo     CASE 
echo         WHEN SUM(CASE WHEN table_name IN ('customerinfo', 'customercontactinfo', 'customeraddress',
echo                                           'vendorinfo', 'vendorcontactinfo',
echo                                           'sellorder', 'purchaseorder', 'material') THEN 1 ELSE 0 END) = 8
echo         THEN '✅ 所有 EBS 表已创建'
echo         ELSE '❌ 部分 EBS 表缺失'
echo     END as "验证结果"
echo FROM information_schema.tables 
echo WHERE table_schema = 'public';
) > verify_ebs_tables.sql

echo ✅ 验证 SQL 文件已创建: verify_ebs_tables.sql
echo.

echo 3. 执行验证（如果 psql 可用）...
where psql >nul 2>&1
if %errorlevel% equ 0 (
    echo 检测到 psql，正在执行验证...
    set PGPASSWORD=1234
    psql -h localhost -p 5432 -U postgres -d FrontCRM -f verify_ebs_tables.sql
) else (
    echo ❌ psql 不可用
    echo.
    echo 请使用以下方法之一验证:
    echo 1. 使用 pgAdmin 执行 verify_ebs_tables.sql
    echo 2. 安装 psql 命令行工具
    echo 3. 使用 DBeaver 或其他数据库工具
    echo.
    echo SQL 文件内容摘要:
    echo ========================================
    type verify_ebs_tables.sql | findstr "SELECT\|FROM\|WHERE" | head -20
    echo ========================================
)

echo.
echo 4. 手动验证命令:
echo.
echo 方法 A: 使用 pgAdmin
echo   1. 打开 pgAdmin
echo   2. 连接到 localhost:5432
echo   3. 选择 FrontCRM 数据库
echo   4. 执行以下 SQL:
echo.
echo      SELECT table_name FROM information_schema.tables 
echo      WHERE table_schema = 'public' 
echo      ORDER BY table_name;
echo.
echo 方法 B: 简单检查
echo   执行以下 SQL 检查 EBS 表:
echo.
echo      SELECT table_name,
echo             CASE WHEN table_name IN ('customerinfo','customercontactinfo','customeraddress',
echo                                      'vendorinfo','vendorcontactinfo',
echo                                      'sellorder','purchaseorder','material')
echo                  THEN '✅ EBS表'
echo                  ELSE '其他表'
echo             END as type
echo      FROM information_schema.tables 
echo      WHERE table_schema = 'public'
echo      ORDER BY type DESC, table_name;
echo.

echo ========================================
echo 验证完成后：
echo 1. 所有8个EBS表应显示为 ✅ EBS表
echo 2. 可以开始配置 CRM 应用程序
echo ========================================
echo.
pause