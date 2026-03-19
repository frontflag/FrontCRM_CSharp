#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import psycopg2
import sys
import os

def execute_sql_file():
    """执行 SQL 文件在远程 PostgreSQL 数据库中更新表结构"""
    
    print("=== 更新远程 FrontCRM 数据库 (129.226.161.3) ===")
    
    # 远程数据库连接参数
    db_params = {
        'host': '129.226.161.3',
        'port': '5432',
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': 'adm@FF#1720'
    }
    
    # SQL 文件路径
    sql_file = 'update_remote_database.sql'
    
    if not os.path.exists(sql_file):
        print(f"❌ SQL 文件不存在: {sql_file}")
        return False
    
    try:
        print(f"正在连接到 PostgreSQL: {db_params['host']}:{db_params['port']}")
        conn = psycopg2.connect(**db_params)
        conn.autocommit = False
        
        cursor = conn.cursor()
        print("✅ 数据库连接成功")
        
        # 读取 SQL 文件
        print(f"正在读取 SQL 文件: {sql_file}")
        with open(sql_file, 'r', encoding='utf-8') as f:
            sql_content = f.read()
        
        print(f"SQL 文件大小: {len(sql_content)} 字符")
        
        # 执行整个 SQL 脚本
        try:
            print("开始执行 SQL 脚本...")
            cursor.execute(sql_content)
            conn.commit()
            print("✅ SQL 脚本执行成功")
        except Exception as e:
            conn.rollback()
            print(f"❌ SQL 执行失败: {e}")
            return False
        
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
        
        # 重点检查新表
        new_tables = [
            'component_cache', 'sys_serial_number', 'sys_error_log',
            'vendor', 'vendor_address', 'vendorcontactinfo', 'vendorbankinfo',
            'stock', 'stockin', 'stockinitem', 'stockout', 'stockoutitem', 'stockoutrequest',
            'rfq', 'rfqitem', 'quote', 'quoteitem'
        ]
        
        table_list = [t[0] for t in tables]
        
        for table_name in new_tables:
            if table_name in table_list:
                print(f"  ✅ {table_name}")
            else:
                print(f"  ❌ {table_name} (缺失)")
        
        # 验证迁移历史
        print("\n=== 验证迁移历史 ===")
        try:
            cursor.execute('SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId"')
            migrations = cursor.fetchall()
            print(f"已应用的迁移 ({len(migrations)} 个):")
            for m in migrations:
                print(f"  - {m[0]}")
        except Exception as e:
            print(f"⚠️  无法读取迁移历史: {e}")
        
        cursor.close()
        conn.close()
        
        print("\n🎉 远程数据库更新完成！")
        return True
            
    except psycopg2.OperationalError as e:
        print(f"❌ 连接数据库失败: {e}")
        print("请检查:")
        print("  1. 远程服务器 129.226.161.3 是否可访问")
        print("  2. 数据库名、用户名、密码是否正确")
        print("  3. 端口 5432 是否开放")
        print("  4. Docker 容器是否运行")
        return False
    except Exception as e:
        print(f"❌ 执行过程中出错: {e}")
        import traceback
        traceback.print_exc()
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
