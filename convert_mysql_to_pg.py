#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MySQL 到 PostgreSQL 转换脚本
"""

import re
import os

def convert_sql():
    input_file = "文档/34_EBS业务数据库表结构_SQL脚本.sql"
    output_file = "ebs_all_tables_postgres.sql"
    
    print("Reading MySQL SQL file...")
    
    with open(input_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    print(f"File size: {len(content)} characters")
    
    # 简单的字符串替换转换
    result = content
    
    # 1. 移除 MySQL 特定设置
    result = re.sub(r'SET FOREIGN_KEY_CHECKS = \d+;', '', result, flags=re.IGNORECASE)
    result = re.sub(r'SET NAMES \w+;', '', result, flags=re.IGNORECASE)
    
    # 2. 转换 DROP TABLE
    result = re.sub(r'DROP TABLE IF EXISTS `([^`]+)`;', r'DROP TABLE IF EXISTS "\1" CASCADE;', result)
    
    # 3. 转换 CREATE TABLE
    result = re.sub(r'CREATE TABLE `([^`]+)`', r'CREATE TABLE IF NOT EXISTS "\1"', result)
    
    # 4. 转换反引号为双引号
    result = result.replace('`', '"')
    
    # 5. 转换数据类型
    result = re.sub(r'\btinyint\b', 'smallint', result, flags=re.IGNORECASE)
    result = re.sub(r'\bdatetime\b', 'timestamp', result, flags=re.IGNORECASE)
    result = re.sub(r'\bdecimal\b', 'numeric', result, flags=re.IGNORECASE)
    
    # 6. 转换 bit(1) 为 boolean
    result = re.sub(r'\bbit\(1\)\s+DEFAULT\s+b\'0\'', 'boolean DEFAULT false', result, flags=re.IGNORECASE)
    result = re.sub(r'\bbit\(1\)\s+DEFAULT\s+b\'1\'', 'boolean DEFAULT true', result, flags=re.IGNORECASE)
    result = re.sub(r'\bbit\(1\)\b', 'boolean', result, flags=re.IGNORECASE)
    
    # 7. 移除 AUTO_INCREMENT
    result = re.sub(r'\s*AUTO_INCREMENT\b', '', result, flags=re.IGNORECASE)
    
    # 8. 移除 COMMENT
    result = re.sub(r'COMMENT\s*=\s*\'[^\']*\'', '', result, flags=re.IGNORECASE)
    result = re.sub(r'COMMENT\s+\'[^\']*\'', '', result, flags=re.IGNORECASE)
    
    # 9. 移除 ENGINE 和 CHARSET
    result = re.sub(r'\)\s*ENGINE\s*=\s*\w+\s*DEFAULT\s*CHARSET\s*=\s*\w+\s*COLLATE\s*=\s*\w+', ')', result, flags=re.IGNORECASE)
    result = re.sub(r'\)\s*ENGINE\s*=\s*\w+\s*DEFAULT\s*CHARSET\s*=\s*\w+', ')', result, flags=re.IGNORECASE)
    result = re.sub(r'\)\s*ENGINE\s*=\s*\w+', ')', result, flags=re.IGNORECASE)
    
    # 10. 转换 KEY 为 CREATE INDEX
    # 先保存表名，然后转换 KEY
    lines = result.split('\n')
    new_lines = []
    current_table = None
    indexes = []
    
    for line in lines:
        # 检测当前表名
        match = re.search(r'CREATE TABLE IF NOT EXISTS "([^"]+)"', line)
        if match:
            current_table = match.group(1)
        
        # 转换 UNIQUE KEY
        unique_match = re.search(r'\s*UNIQUE\s+KEY\s+"([^"]+)"\s*\(([^)]+)\)', line, re.IGNORECASE)
        if unique_match and current_table:
            idx_name = unique_match.group(1)
            cols = unique_match.group(2)
            indexes.append(f'CREATE UNIQUE INDEX IF NOT EXISTS "{idx_name}" ON "{current_table}" ({cols});')
            continue
        
        # 转换普通 KEY
        key_match = re.search(r'\s*KEY\s+"([^"]+)"\s*\(([^)]+)\)', line, re.IGNORECASE)
        if key_match and current_table:
            idx_name = key_match.group(1)
            cols = key_match.group(2)
            indexes.append(f'CREATE INDEX IF NOT EXISTS "{idx_name}" ON "{current_table}" ({cols});')
            continue
        
        # 转换 PRIMARY KEY 行内的 KEY 定义
        if 'PRIMARY KEY' in line.upper():
            new_lines.append(line)
            continue
        
        # 移除以 KEY 开头的行（非约束）
        if re.match(r'\s*KEY\s+', line, re.IGNORECASE) and 'PRIMARY' not in line.upper() and 'UNIQUE' not in line.upper():
            continue
        
        new_lines.append(line)
    
    result = '\n'.join(new_lines)
    
    # 11. 移除多余的逗号在括号前
    result = re.sub(r',\s*\)', ')', result)
    
    # 12. 转换 DEFAULT b'0' 和 b'1'
    result = result.replace("DEFAULT b'0'", "DEFAULT false")
    result = result.replace("DEFAULT b'1'", "DEFAULT true")
    result = result.replace("DEFAULT B'0'", "DEFAULT false")
    result = result.replace("DEFAULT B'1'", "DEFAULT true")
    
    # 13. 清理多余空行
    result = re.sub(r'\n{3,}', '\n\n', result)
    
    # 添加头部
    header = """-- ============================================================
-- EBS Business Database Tables - PostgreSQL Version
-- Converted from MySQL
-- ============================================================

SET search_path TO public;

"""
    
    # 添加索引到文件末尾
    if indexes:
        index_section = "\n-- ============================================================\n-- Indexes\n-- ============================================================\n\n"
        index_section += '\n'.join(indexes)
        result = result + index_section
    
    # 添加验证
    footer = """

-- ============================================================
-- Verification
-- ============================================================

SELECT 
    'EBS Tables Created Successfully' AS status,
    COUNT(*) AS total_tables
FROM information_schema.tables 
WHERE table_schema = 'public';

SELECT 
    table_name AS table_name,
    CASE 
        WHEN table_name LIKE 'customer%' THEN 'Customer Module'
        WHEN table_name LIKE 'vendor%' THEN 'Vendor Module'
        WHEN table_name LIKE 'sell%' THEN 'Sales Module'
        WHEN table_name LIKE 'purchase%' THEN 'Purchase Module'
        WHEN table_name LIKE 'material%' THEN 'Material Module'
        WHEN table_name LIKE 'stock%' THEN 'Stock Module'
        WHEN table_name LIKE 'finance%' THEN 'Finance Module'
        ELSE 'Other'
    END AS module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;
"""
    
    result = header + result + footer
    
    # 保存
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(result)
    
    print(f"Conversion complete!")
    print(f"Output file: {output_file}")
    print(f"Output size: {len(result)} characters")
    
    # 统计
    table_count = len(re.findall(r'CREATE TABLE IF NOT EXISTS', result))
    index_count = len(re.findall(r'CREATE INDEX', result))
    print(f"Tables: {table_count}")
    print(f"Indexes: {index_count}")
    
    return True

if __name__ == "__main__":
    try:
        convert_sql()
        print("\n[SUCCESS] Conversion completed!")
    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
