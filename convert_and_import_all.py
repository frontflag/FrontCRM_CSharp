#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MySQL 到 PostgreSQL 转换并导入脚本
转换 34_EBS业务数据库表结构_SQL脚本.sql 为 PostgreSQL 语法并导入
"""

import re
import os
import sys

def convert_mysql_to_postgresql(input_file, output_file):
    """将 MySQL SQL 转换为 PostgreSQL SQL"""
    
    print(f"正在读取 MySQL SQL 文件: {input_file}")
    
    with open(input_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    print(f"文件大小: {len(content)} 字符")
    
    # 转换规则
    conversions = [
        # 1. 移除 MySQL 特定设置
        (r'SET FOREIGN_KEY_CHECKS = \d+;', ''),
        (r'SET NAMES \w+;', ''),
        (r'SET SQL_MODE = [^;]+;', ''),
        
        # 2. 转换 DROP TABLE IF EXISTS
        (r'DROP TABLE IF EXISTS `([^`]+)`;', r'DROP TABLE IF EXISTS "\1" CASCADE;'),
        
        # 3. 转换 CREATE TABLE
        (r'CREATE TABLE `([^`]+)`', r'CREATE TABLE IF NOT EXISTS "\1"'),
        
        # 4. 转换反引号为双引号
        (r'`([^`]+)`', r'"\1"'),
        
        # 5. 转换数据类型
        (r'\btinyint\b', 'smallint'),
        (r'\bdatetime\b', 'timestamp'),
        (r'\bdecimal\b', 'numeric'),
        (r'\btext\b', 'text'),
        (r'\bbit\(1\)\s+DEFAULT\s+b\'0\'', 'boolean DEFAULT false'),
        (r'\bbit\(1\)\s+DEFAULT\s+b\'1\'', 'boolean DEFAULT true'),
        (r'\bbit\(1\)\b', 'boolean'),
        
        # 6. 转换 AUTO_INCREMENT
        (r'\bAUTO_INCREMENT\b', ''),
        (r'\bauto_increment\b', ''),
        
        # 7. 转换 COMMENT
        (r'COMMENT\s*=\s*\'[^\']*\'', ''),
        (r'COMMENT\s+\'[^\']*\'', ''),
        
        # 8. 转换 ENGINE 和 CHARSET
        (r')\s*ENGINE\s*=\s*\w+\s*DEFAULT\s*CHARSET\s*=\s*\w+\s*COLLATE\s*=\s*\w+', r')'),
        (r')\s*ENGINE\s*=\s*\w+\s*DEFAULT\s*CHARSET\s*=\s*\w+', r')'),
        (r')\s*ENGINE\s*=\s*\w+', r')'),
        
        # 9. 转换 KEY/INDEX
        (r'\bKEY\s+`([^`]+)`\s*\(([^)]+)\)', r'CREATE INDEX IF NOT EXISTS "\1" ON "{table_name}" (\2);'),
        (r'\bUNIQUE\s+KEY\s+`([^`]+)`\s*\(([^)]+)\)', r'CONSTRAINT "\1" UNIQUE (\2)'),
        
        # 10. 转换 CURRENT_TIMESTAMP
        (r'CURRENT_TIMESTAMP\(\)', 'CURRENT_TIMESTAMP'),
        (r'current_timestamp\(\)', 'CURRENT_TIMESTAMP'),
        
        # 11. 移除多余的逗号
        (r',\s*\)', ')'),
        
        # 12. 转换 DEFAULT 值
        (r"DEFAULT\s+b'0'", 'DEFAULT false'),
        (r"DEFAULT\s+b'1'", 'DEFAULT true'),
        
        # 13. 清理多余空格
        (r'\n\s*\n\s*\n', '\n\n'),
    ]
    
    result = content
    
    # 应用转换规则
    for pattern, replacement in conversions:
        result = re.sub(pattern, replacement, result, flags=re.IGNORECASE)
    
    # 添加 PostgreSQL 头部注释
    header = """-- ============================================================
-- EBS 业务数据库表结构 PostgreSQL 版本
-- 转换来源: 34_EBS业务数据库表结构_SQL脚本.sql
-- 转换日期: 自动生成
-- 说明: 自动从 MySQL 转换为 PostgreSQL 语法
-- ============================================================

-- 设置搜索路径
SET search_path TO public;

"""
    
    result = header + result
    
    # 添加验证语句
    footer = """

-- ============================================================
-- 验证表创建结果
-- ============================================================

-- 统计创建的表数量
SELECT 
    'EBS 业务表创建完成' AS message,
    COUNT(*) AS total_tables
FROM information_schema.tables 
WHERE table_schema = 'public';

-- 列出所有表
SELECT 
    table_name,
    CASE 
        WHEN table_name LIKE 'customer%' THEN '客户模块'
        WHEN table_name LIKE 'vendor%' THEN '供应商模块'
        WHEN table_name LIKE 'sell%' THEN '销售模块'
        WHEN table_name LIKE 'purchase%' THEN '采购模块'
        WHEN table_name LIKE 'material%' THEN '物料模块'
        WHEN table_name LIKE 'stock%' THEN '库存模块'
        WHEN table_name LIKE 'finance%' THEN '财务模块'
        ELSE '其他'
    END AS module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;
"""
    
    result = result + footer
    
    # 保存转换后的文件
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(result)
    
    print(f"✅ 转换完成，已保存到: {output_file}")
    print(f"输出文件大小: {len(result)} 字符")
    
    # 统计表数量
    table_count = len(re.findall(r'CREATE TABLE IF NOT EXISTS', result))
    print(f"包含 {table_count} 个 CREATE TABLE 语句")
    
    return output_file

def main():
    """主函数"""
    
    print("=" * 60)
    print("MySQL 到 PostgreSQL 转换工具")
    print("=" * 60)
    
    # 输入输出文件路径
    input_file = "文档/34_EBS业务数据库表结构_SQL脚本.sql"
    output_file = "ebs_all_tables_postgres.sql"
    
    # 检查输入文件是否存在
    if not os.path.exists(input_file):
        print(f"❌ 输入文件不存在: {input_file}")
        print(f"当前工作目录: {os.getcwd()}")
        return False
    
    # 执行转换
    try:
        output_path = convert_mysql_to_postgresql(input_file, output_file)
        print(f"\n✅ 转换成功!")
        print(f"输出文件: {output_path}")
        
        print("\n" + "=" * 60)
        print("下一步操作:")
        print("=" * 60)
        print("1. 使用 pgAdmin 打开 ebs_all_tables_postgres.sql")
        print("2. 连接到 localhost:5432 FrontCRM 数据库")
        print("3. 执行 SQL 脚本创建所有表")
        print("\n或使用命令行（如果已安装 psql）:")
        print("   set PGPASSWORD=1234")
        print("   psql -h localhost -p 5432 -U postgres -d FrontCRM -f ebs_all_tables_postgres.sql")
        
        return True
        
    except Exception as e:
        print(f"❌ 转换失败: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
