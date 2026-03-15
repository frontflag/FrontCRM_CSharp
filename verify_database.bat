@echo off
chcp 65001 >nul
echo ========================================
echo  验证 FrontCRM 数据库 EBS 业务表
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务...
sc query postgresql-x64-16 | findstr "STATE"
echo.

echo 2. 生成验证 SQL 文件...
(
echo -- 验证 EBS 业务表创建情况
echo SELECT '=== EBS 数据库验证报告 ===' AS title;
echo.
echo -- 1. 统计总表数
echo SELECT 
echo     '总表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public';
echo.
echo -- 2. 按模块统计
echo SELECT 
echo     '客户模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND table_name LIKE 'customer%%';
echo.
echo SELECT 
echo     '供应商模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND table_name LIKE 'vendor%%';
echo.
echo SELECT 
echo     '销售模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND table_name LIKE 'sell%%';
echo.
echo SELECT 
echo     '采购模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND table_name LIKE 'purchase%%';
echo.
echo SELECT 
echo     '物料模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND table_name LIKE 'material%%';
echo.
echo SELECT 
echo     '库存模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND (table_name LIKE 'stock%%' OR table_name LIKE 'warehouse%%');
echo.
echo SELECT 
echo     '财务模块表数' AS item,
echo     COUNT(*)::text AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public' AND (table_name LIKE 'finance%%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate'));
echo.
echo -- 3. 列出所有表
echo SELECT 
echo     '--- 所有表列表 ---' AS item,
echo     '' AS value;
echo.
echo SELECT 
echo     table_name AS item,
echo     CASE 
echo         WHEN table_name LIKE 'customer%%' THEN '客户模块'
echo         WHEN table_name LIKE 'vendor%%' THEN '供应商模块'
echo         WHEN table_name LIKE 'sell%%' THEN '销售模块'
echo         WHEN table_name LIKE 'purchase%%' THEN '采购模块'
echo         WHEN table_name LIKE 'material%%' THEN '物料模块'
echo         WHEN table_name LIKE 'stock%%' OR table_name LIKE 'warehouse%%' THEN '库存模块'
echo         WHEN table_name LIKE 'finance%%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN '财务模块'
echo         ELSE '其他'
echo     END AS value
echo FROM information_schema.tables 
echo WHERE table_schema = 'public'
echo ORDER BY value, table_name;
echo.
echo -- 4. 统计索引
echo SELECT 
echo     '总索引数' AS item,
echo     COUNT(*)::text AS value
echo FROM pg_indexes 
echo WHERE schemaname = 'public';
echo.
echo -- 5. 验证结果
echo SELECT 
echo     '--- 验证结果 ---' AS item,
echo     CASE 
echo         WHEN (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public') >= 41
echo         THEN '✓ 通过 - 所有表已创建'
echo         ELSE '✗ 失败 - 表数量不足'
echo     END AS value;
) > verify_db.sql

echo ✓ 验证 SQL 已生成: verify_db.sql
echo.

echo 3. 执行验证...
where psql >nul 2>&1
if %errorlevel% equ 0 (
    echo 正在连接数据库并执行验证...
    set PGPASSWORD=1234
    psql -h localhost -p 5432 -U postgres -d FrontCRM -f verify_db.sql
) else (
    echo ! psql 命令不可用
echo.
    echo 请使用 pgAdmin 执行 verify_db.sql 文件
echo.
    echo 或者手动执行以下 SQL:
echo ========================================
    type verify_db.sql
echo ========================================
)

echo.
echo 4. 预期结果:
echo    - 总表数: 41 个
echo    - 客户模块: 6 个表
echo    - 供应商模块: 7 个表
echo    - 销售模块: 6 个表
echo    - 采购模块: 6 个表
echo    - 物料模块: 6 个表
echo    - 库存模块: 8 个表
echo    - 财务模块: 7 个表
echo    - 索引数: 83 个
echo.

echo ========================================
pause