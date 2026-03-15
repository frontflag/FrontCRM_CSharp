#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
验证本地数据库更新状态
"""

import os

def check_database():
    """检查数据库更新状态"""
    
    print("=" * 60)
    print("FrontCRM 数据库更新验证")
    print("=" * 60)
    print()
    
    # 期望的新表
    expected_tables = {
        '财务表': [
            'invoice', 'invoiceitem',
            'payment', 'paymentitem',
            'receipt', 'receiptitem'
        ],
        '日志表': [
            'businesslog', 'businesslogdetail'
        ]
    }
    
    print("期望创建的新表：")
    print()
    
    all_tables = []
    for module, tables in expected_tables.items():
        print(f"  {module}:")
        for table in tables:
            print(f"    - {table}")
            all_tables.append(table)
        print()
    
    print(f"总计: {len(all_tables)} 个新表")
    print()
    
    # 生成验证 SQL
    tables_str = ', '.join([f"'{t}'" for t in all_tables])
    verify_sql = f"""-- 验证 SQL 脚本
-- 请在 pgAdmin 中执行以下 SQL 验证

-- 1. 检查所有新表是否存在
SELECT 
    table_name,
    '已创建' AS status
FROM information_schema.tables 
WHERE table_schema = 'public'
    AND table_name IN ({tables_str})
ORDER BY table_name;

-- 2. 统计数量
SELECT 
    COUNT(*) AS new_table_count,
    CASE 
        WHEN COUNT(*) = {len(all_tables)} THEN '通过 - 所有新表已创建'
        ELSE '失败 - 部分表缺失'
    END AS verify_result
FROM information_schema.tables 
WHERE table_schema = 'public'
    AND table_name IN ({tables_str});

-- 3. 检查索引
SELECT 
    tablename,
    COUNT(*) AS index_count
FROM pg_indexes
WHERE schemaname = 'public'
    AND tablename IN ({tables_str})
GROUP BY tablename
ORDER BY tablename;
"""
    
    # 保存验证 SQL
    with open('verify_new_tables.sql', 'w', encoding='utf-8') as f:
        f.write(verify_sql)
    
    print("验证 SQL 已生成: verify_new_tables.sql")
    print()
    
    print("=" * 60)
    print("验证方法：")
    print("=" * 60)
    print()
    print("方法 1: 使用 pgAdmin")
    print("  1. 打开 pgAdmin")
    print("  2. 连接到 localhost:5432 / FrontCRM")
    print("  3. 打开查询工具")
    print("  4. 执行文件: verify_new_tables.sql")
    print()
    print("方法 2: 手动检查")
    print("  执行以下 SQL:")
    print()
    print("  SELECT table_name")
    print("  FROM information_schema.tables")
    print("  WHERE table_schema = 'public'")
    print("    AND table_name IN ('invoice', 'payment', 'receipt', 'businesslog')")
    print("  ORDER BY table_name;")
    print()
    
    print("=" * 60)
    print("预期结果：")
    print("=" * 60)
    print()
    print("通过 - 新表数量: 8 个")
    print("   - 财务表: 6 个")
    print("     * invoice, invoiceitem")
    print("     * payment, paymentitem")
    print("     * receipt, receiptitem")
    print()
    print("   - 日志表: 2 个")
    print("     * businesslog")
    print("     * businesslogdetail")
    print()
    
    return True

if __name__ == "__main__":
    check_database()
