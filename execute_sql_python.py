#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import psycopg2
import sys
import os

def execute_sql_file():
    """执行 SQL 文件在 PostgreSQL 数据库中创建表"""
    
    print("=== 在 FrontCRM 数据库中创建 EBS 业务表 ===")
    
    # 数据库连接参数
    db_params = {
        'host': 'localhost',
        'port': '5432',
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    # SQL 文件路径
    sql_file = 'ebs_core_tables.sql'
    
    if not os.path.exists(sql_file):
        print(f"❌ SQL 文件不存在: {sql_file}")
        return False
    
    try:
        print(f"正在连接到 PostgreSQL: {db_params['host']}:{db_params['port']}")
        conn = psycopg2.connect(**db_params)
        conn.autocommit = False
        
        cursor = conn.cursor()
        print("✅ 数据库连接成功")
        
        # 读取并执行 SQL 文件
        print(f"正在读取 SQL 文件: {sql_file}")
        with open(sql_file, 'r', encoding='utf-8') as f:
            sql_content = f.read()
        
        # 分割 SQL 语句（按分号分割）
        sql_statements = sql_content.split(';')
        
        print(f"开始执行 {len(sql_statements)} 条 SQL 语句...")
        
        success_count = 0
        error_count = 0
        
        for i, sql in enumerate(sql_statements, 1):
            sql = sql.strip()
            if not sql:
                continue
                
            try:
                # 移除注释
                lines = sql.split('\n')
                sql_lines = []
                for line in lines:
                    line = line.strip()
                    if line and not line.startswith('--'):
                        sql_lines.append(line)
                
                clean_sql = ' '.join(sql_lines)
                if not clean_sql:
                    continue
                    
                # 执行 SQL
                cursor.execute(clean_sql)
                success_count += 1
                
                # 显示表名
                if 'CREATE TABLE' in clean_sql.upper():
                    table_name = clean_sql.split()[2].replace('"', '')
                    print(f"  ✅ 创建表: {table_name}")
                
            except Exception as e:
                error_count += 1
                print(f"  ❌ 第 {i} 条语句执行失败: {e}")
                print(f"    语句: {sql[:100]}...")
        
        # 提交事务
        conn.commit()
        
        print("\n=== 执行结果 ===")
        print(f"✅ 成功执行: {success_count} 条")
        print(f"❌ 执行失败: {error_count} 条")
        
        # 验证表是否创建成功
        print("\n=== 验证已创建的表 ===")
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name;
        """)
        
        tables = cursor.fetchall()
        print(f"数据库中共有 {len(tables)} 个表:")
        for i, (table_name,) in enumerate(tables, 1):
            print(f"  {i:2d}. {table_name}")
        
        cursor.close()
        conn.close()
        
        if error_count == 0:
            print("\n🎉 所有表创建成功！")
            return True
        else:
            print("\n⚠️  部分表创建失败，请检查错误信息")
            return False
            
    except psycopg2.OperationalError as e:
        print(f"❌ 连接数据库失败: {e}")
        print("请检查:")
        print("  1. PostgreSQL 服务是否运行")
        print("  2. 数据库名、用户名、密码是否正确")
        print("  3. 端口 5432 是否可访问")
        return False
    except Exception as e:
        print(f"❌ 执行过程中出错: {e}")
        return False

if __name__ == "__main__":
    # 先检查 psycopg2 是否安装
    try:
        import psycopg2
        print("✅ psycopg2 模块已安装")
    except ImportError:
        print("❌ psycopg2 未安装，正在安装...")
        import subprocess
        subprocess.check_call([sys.executable, "-m", "pip", "install", "psycopg2-binary"])
        print("✅ psycopg2 安装成功")
        import psycopg2
    
    execute_sql_file()