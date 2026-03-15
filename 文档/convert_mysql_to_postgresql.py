#!/usr/bin/env python3
"""
将 MySQL SQL 文件转换为 PostgreSQL 语法
"""

import re
import sys

def convert_mysql_to_postgresql(sql_content):
    """转换 MySQL SQL 到 PostgreSQL"""
    
    lines = sql_content.split('\n')
    converted_lines = []
    
    for line in lines:
        # 移除 MySQL 注释
        if line.strip().startswith('--'):
            converted_lines.append(line)
            continue
            
        # 替换反引号为双引号
        line = line.replace('`', '"')
        
        # 移除 MySQL 特定选项
        line = re.sub(r'ENGINE=InnoDB\s*', '', line)
        line = re.sub(r'DEFAULT CHARSET=utf8mb4\s*', '', line)
        line = re.sub(r'DEFAULT CHARSET=utf8\s*', '', line)
        
        # 替换数据类型
        line = re.sub(r'\btinyint\b', 'smallint', line)
        line = re.sub(r'\bbit\(1\)\b', 'boolean', line)
        line = re.sub(r'\bdatetime\b', 'timestamp', line)
        line = re.sub(r'\btext\b', 'text', line)
        
        # 替换布尔值
        line = line.replace("b'0'", 'false')
        line = line.replace("b'1'", 'true')
        
        # 替换 CURRENT_TIMESTAMP
        line = line.replace('CURRENT_TIMESTAMP', 'NOW()')
        
        # 处理 COMMENT
        if 'COMMENT' in line:
            # 将 COMMENT 转换为 PostgreSQL 注释
            comment_match = re.search(r"COMMENT\s+'([^']+)'", line)
            if comment_match:
                comment = comment_match.group(1)
                # 移除 COMMENT 部分
                line = re.sub(r"COMMENT\s+'[^']+'", '', line)
                # 添加注释
                if line.strip().endswith(','):
                    line = line.rstrip(',')
                converted_lines.append(line)
                # 添加 PostgreSQL 注释
                table_match = re.search(r'CREATE TABLE\s+"([^"]+)"', line)
                if table_match:
                    table_name = table_match.group(1)
                    converted_lines.append(f'COMMENT ON TABLE "{table_name}" IS \'{comment}\';')
                else:
                    # 可能是列注释
                    col_match = re.search(r'"([^"]+)"\s+', line)
                    if col_match:
                        col_name = col_match.group(1)
                        # 需要知道表名，这里简化处理
                        converted_lines.append(f'-- Column comment: {col_name}: {comment}')
                continue
        
        # 处理外键检查（PostgreSQL 不需要）
        if 'SET FOREIGN_KEY_CHECKS' in line:
            continue
            
        converted_lines.append(line)
    
    return '\n'.join(converted_lines)

def main():
    input_file = "34_EBS业务数据库表结构_SQL脚本.sql"
    output_file = "34_EBS业务数据库表结构_PostgreSQL.sql"
    
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            mysql_sql = f.read()
        
        postgres_sql = convert_mysql_to_postgresql(mysql_sql)
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(postgres_sql)
        
        print(f"转换完成！输出文件: {output_file}")
        print(f"原始行数: {len(mysql_sql.split('\\n'))}")
        print(f"转换后行数: {len(postgres_sql.split('\\n'))}")
        
    except FileNotFoundError:
        print(f"错误: 找不到文件 {input_file}")
        print("请确保文件在当前目录下")
    except Exception as e:
        print(f"转换过程中出错: {str(e)}")

if __name__ == "__main__":
    main()