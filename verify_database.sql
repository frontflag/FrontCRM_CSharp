-- ============================================================
-- FrontCRM 数据库验证脚本
-- 验证 EBS 业务表是否已正确创建
-- ============================================================

-- 1. 验证总表数
SELECT '1. 总表数统计' AS check_item;
SELECT COUNT(*) AS total_tables
FROM information_schema.tables 
WHERE table_schema = 'public';

-- 2. 按模块统计
SELECT '2. 按模块统计' AS check_item;
SELECT 
    CASE 
        WHEN table_name LIKE 'customer%' THEN '客户模块'
        WHEN table_name LIKE 'vendor%' THEN '供应商模块'
        WHEN table_name LIKE 'sell%' THEN '销售模块'
        WHEN table_name LIKE 'purchase%' THEN '采购模块'
        WHEN table_name LIKE 'material%' THEN '物料模块'
        WHEN table_name LIKE 'stock%' OR table_name LIKE 'warehouse%' THEN '库存模块'
        WHEN table_name LIKE 'finance%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN '财务模块'
        ELSE '其他'
    END AS module,
    COUNT(*) AS table_count
FROM information_schema.tables 
WHERE table_schema = 'public'
GROUP BY 
    CASE 
        WHEN table_name LIKE 'customer%' THEN '客户模块'
        WHEN table_name LIKE 'vendor%' THEN '供应商模块'
        WHEN table_name LIKE 'sell%' THEN '销售模块'
        WHEN table_name LIKE 'purchase%' THEN '采购模块'
        WHEN table_name LIKE 'material%' THEN '物料模块'
        WHEN table_name LIKE 'stock%' OR table_name LIKE 'warehouse%' THEN '库存模块'
        WHEN table_name LIKE 'finance%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN '财务模块'
        ELSE '其他'
    END
ORDER BY module;

-- 3. 列出所有表
SELECT '3. 所有表列表' AS check_item;
SELECT 
    table_name,
    CASE 
        WHEN table_name LIKE 'customer%' THEN '客户模块'
        WHEN table_name LIKE 'vendor%' THEN '供应商模块'
        WHEN table_name LIKE 'sell%' THEN '销售模块'
        WHEN table_name LIKE 'purchase%' THEN '采购模块'
        WHEN table_name LIKE 'material%' THEN '物料模块'
        WHEN table_name LIKE 'stock%' OR table_name LIKE 'warehouse%' THEN '库存模块'
        WHEN table_name LIKE 'finance%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN '财务模块'
        ELSE '其他'
    END AS module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;

-- 4. 统计索引
SELECT '4. 索引统计' AS check_item;
SELECT COUNT(*) AS total_indexes
FROM pg_indexes 
WHERE schemaname = 'public';

-- 5. 检查核心表是否存在
SELECT '5. 核心表检查' AS check_item;
SELECT 
    table_name,
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.tables 
                      WHERE table_schema = 'public' AND table_name = t.table_name)
        THEN '已创建'
        ELSE '未创建'
    END AS status
FROM (VALUES 
    ('customerinfo'),
    ('customercontactinfo'),
    ('vendorinfo'),
    ('vendorcontactinfo'),
    ('sellorder'),
    ('purchaseorder'),
    ('material'),
    ('stock'),
    ('financeaccount'),
    ('invoice')
) AS t(table_name);

-- 6. 最终验证结果
SELECT '6. 验证结果' AS check_item;
SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public') >= 41
        THEN '通过 - 所有 EBS 业务表已创建'
        ELSE '失败 - 表数量不足，请检查导入过程'
    END AS result_message,
    (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public') AS actual_count,
    41 AS expected_count;