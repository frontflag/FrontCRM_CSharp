-- 验证 SQL 脚本
-- 请在 pgAdmin 中执行以下 SQL 验证

-- 1. 检查所有新表是否存在
SELECT 
    table_name,
    '已创建' AS status
FROM information_schema.tables 
WHERE table_schema = 'public'
    AND table_name IN ('invoice', 'invoiceitem', 'payment', 'paymentitem', 'receipt', 'receiptitem', 'businesslog', 'businesslogdetail')
ORDER BY table_name;

-- 2. 统计数量
SELECT 
    COUNT(*) AS new_table_count,
    CASE 
        WHEN COUNT(*) = 8 THEN '通过 - 所有新表已创建'
        ELSE '失败 - 部分表缺失'
    END AS verify_result
FROM information_schema.tables 
WHERE table_schema = 'public'
    AND table_name IN ('invoice', 'invoiceitem', 'payment', 'paymentitem', 'receipt', 'receiptitem', 'businesslog', 'businesslogdetail');

-- 3. 检查索引
SELECT 
    tablename,
    COUNT(*) AS index_count
FROM pg_indexes
WHERE schemaname = 'public'
    AND tablename IN ('invoice', 'invoiceitem', 'payment', 'paymentitem', 'receipt', 'receiptitem', 'businesslog', 'businesslogdetail')
GROUP BY tablename
ORDER BY tablename;
