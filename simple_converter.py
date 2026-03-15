#!/usr/bin/env python3
"""
简单转换 MySQL 到 PostgreSQL
"""

import re
import os

def simple_convert(mysql_sql):
    """简单转换 MySQL SQL 到 PostgreSQL"""
    
    # 基本转换规则
    conversions = [
        (r'`', '"'),  # 反引号转双引号
        (r'bit\(1\)', 'boolean'),
        (r"b'0'", 'false'),
        (r"b'1'", 'true'),
        (r'datetime', 'timestamp'),
        (r'tinyint', 'smallint'),
        (r'ENGINE=InnoDB', ''),
        (r'DEFAULT CHARSET=utf8mb4', ''),
        (r'COMMENT\s+\'[^\']*\'', ''),  # 移除 COMMENT
        (r'KEY\s+"[^"]+"\s*\([^)]+\)', ''),  # 移除普通索引
        (r'UNIQUE KEY', 'CONSTRAINT'),
    ]
    
    result = mysql_sql
    
    for pattern, replacement in conversions:
        result = re.sub(pattern, replacement, result)
    
    # 移除 MySQL 特定语句
    lines = result.split('\n')
    filtered_lines = []
    
    for line in lines:
        if line.strip().startswith('SET FOREIGN_KEY_CHECKS'):
            continue
        if line.strip().startswith('-- 开启外键检查'):
            continue
        filtered_lines.append(line)
    
    return '\n'.join(filtered_lines)

def main():
    # 读取原始文件
    input_file = "文档/34_EBS业务数据库表结构_SQL脚本.sql"
    
    if not os.path.exists(input_file):
        print(f"文件不存在: {input_file}")
        return
    
    with open(input_file, 'r', encoding='utf-8') as f:
        mysql_sql = f.read()
    
    print(f"读取文件: {input_file}")
    print(f"原始大小: {len(mysql_sql)} 字符")
    
    # 转换
    postgres_sql = simple_convert(mysql_sql)
    
    # 保存
    output_file = "文档/ebs_tables_postgres_ready.sql"
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(postgres_sql)
    
    print(f"转换完成!")
    print(f"输出文件: {output_file}")
    print(f"转换后大小: {len(postgres_sql)} 字符")
    
    # 创建导入脚本
    create_import_script()

def create_import_script():
    """创建导入脚本"""
    
    script = """#!/usr/bin/env python3
import psycopg2
import sys
import os

def import_tables():
    # 连接参数
    conn_params = {
        'host': 'localhost',
        'port': 5432,
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    sql_file = "文档/ebs_tables_postgres_ready.sql"
    
    if not os.path.exists(sql_file):
        print(f"错误: SQL文件不存在 {sql_file}")
        return False
    
    try:
        print("连接到 PostgreSQL...")
        conn = psycopg2.connect(**conn_params)
        conn.autocommit = True
        cursor = conn.cursor()
        
        print("连接成功!")
        
        # 读取SQL文件
        with open(sql_file, 'r', encoding='utf-8') as f:
            sql_content = f.read()
        
        print(f"读取SQL文件: {len(sql_content)} 字符")
        
        # 分割并执行SQL语句
        statements = [s.strip() for s in sql_content.split(';') if s.strip()]
        
        print(f"正在执行 {len(statements)} 条SQL语句...")
        
        success_count = 0
        error_count = 0
        
        for i, stmt in enumerate(statements):
            if not stmt.endswith(';'):
                stmt += ';'
            
            try:
                cursor.execute(stmt)
                success_count += 1
                if i % 10 == 0:
                    print(f"  进度: {i+1}/{len(statements)}")
            except Exception as e:
                error_count += 1
                print(f"  错误[{i+1}]: {str(e)[:100]}")
        
        print(f"\\n执行结果: 成功 {success_count}, 失败 {error_count}")
        
        # 验证表
        cursor.execute("""
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema = 'public'
        """)
        
        table_count = cursor.fetchone()[0]
        print(f"\\n数据库中的表总数: {table_count}")
        
        cursor.close()
        conn.close()
        
        if error_count == 0:
            print("\\n✅ 所有表创建成功!")
            return True
        else:
            print("\\n⚠️  部分表创建失败")
            return False
            
    except Exception as e:
        print(f"❌ 连接失败: {e}")
        return False

if __name__ == "__main__":
    print("=== EBS 数据库表导入 ===")
    print("目标数据库: FrontCRM")
    print("用户名: postgres")
    print("密码: 1234")
    print()
    
    # 测试连接
    try:
        test_conn = psycopg2.connect(
            host='localhost',
            port=5432,
            database='FrontCRM',
            user='postgres',
            password='1234'
        )
        test_conn.close()
        print("✅ 数据库连接正常")
    except Exception as e:
        print(f"❌ 连接测试失败: {e}")
        sys.exit(1)
    
    print("\\n即将导入 34 个业务表...")
    response = input("是否继续? (y/n): ")
    
    if response.lower() != 'y':
        print("取消导入")
        sys.exit(0)
    
    if import_tables():
        print("\\n🎉 EBS 业务数据库表创建完成!")
    else:
        print("\\n❌ 导入失败")
        sys.exit(1)
"""
    
    with open("import_ebs.py", 'w', encoding='utf-8') as f:
        f.write(script)
    
    print(f"\\n导入脚本已创建: import_ebs.py")
    print("使用方法:")
    print("1. 确保 PostgreSQL 服务运行")
    print("2. 确保 FrontCRM 数据库存在")
    print("3. 运行: python import_ebs.py")

if __name__ == "__main__":
    main()