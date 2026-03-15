#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import psycopg2
import sys

def verify_tables():
    """验证 EBS 业务表是否已正确创建"""
    
    print("=== 验证 FrontCRM 数据库中的 EBS 业务表 ===")
    
    # 数据库连接参数
    db_params = {
        'host': 'localhost',
        'port': '5432',
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    try:
        print("正在连接到 PostgreSQL...")
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()
        print("✅ 数据库连接成功")
        
        # 1. 验证表是否存在
        print("\n1. 验证表是否存在:")
        cursor.execute("""
            SELECT table_name, table_type 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name;
        """)
        
        tables = cursor.fetchall()
        print(f"数据库中共有 {len(tables)} 个表:")
        
        ebs_tables = [
            'customerinfo', 'customercontactinfo', 'customeraddress',
            'vendorinfo', 'vendorcontactinfo',
            'sellorder', 'purchaseorder', 'material'
        ]
        
        created_tables = []
        for table_name, table_type in tables:
            status = "✅" if table_name in ebs_tables else "  "
            print(f"  {status} {table_name} ({table_type})")
            if table_name in ebs_tables:
                created_tables.append(table_name)
        
        # 2. 验证 EBS 核心表
        print(f"\n2. EBS 核心业务表 ({len(created_tables)}/{len(ebs_tables)}):")
        for table in ebs_tables:
            if table in created_tables:
                print(f"  ✅ {table}")
            else:
                print(f"  ❌ {table} (未找到)")
        
        # 3. 检查表结构
        print("\n3. 检查表结构:")
        for table in created_tables[:3]:  # 只检查前3个表的结构
            print(f"\n  📋 {table} 表结构:")
            cursor.execute(f"""
                SELECT column_name, data_type, is_nullable, column_default
                FROM information_schema.columns 
                WHERE table_schema = 'public' AND table_name = '{table}'
                ORDER BY ordinal_position;
            """)
            
            columns = cursor.fetchall()
            for col_name, data_type, is_nullable, col_default in columns[:5]:  # 只显示前5列
                nullable = "NOT NULL" if is_nullable == 'NO' else "NULL"
                default = f"DEFAULT {col_default}" if col_default else ""
                print(f"    - {col_name}: {data_type} {nullable} {default}")
            
            if len(columns) > 5:
                print(f"    ... 还有 {len(columns)-5} 列")
        
        # 4. 检查索引
        print("\n4. 检查索引:")
        cursor.execute("""
            SELECT tablename, indexname, indexdef 
            FROM pg_indexes 
            WHERE schemaname = 'public' 
                AND tablename IN %s
            ORDER BY tablename, indexname;
        """, (tuple(created_tables),))
        
        indexes = cursor.fetchall()
        print(f"找到 {len(indexes)} 个索引:")
        for tablename, indexname, indexdef in indexes:
            print(f"  📊 {tablename}: {indexname}")
        
        # 5. 统计信息
        print("\n5. 统计信息:")
        for table in created_tables:
            try:
                cursor.execute(f'SELECT COUNT(*) FROM "{table}";')
                count = cursor.fetchone()[0]
                print(f"  📈 {table}: {count} 条记录")
            except:
                print(f"  📈 {table}: 无法统计记录数")
        
        cursor.close()
        conn.close()
        
        # 总结
        print("\n=== 验证结果 ===")
        if len(created_tables) == len(ebs_tables):
            print("✅ 所有 EBS 业务表已成功创建！")
            print("🎉 数据库结构准备就绪，可以开始使用 CRM 系统。")
        else:
            missing = set(ebs_tables) - set(created_tables)
            print(f"⚠️  缺少 {len(missing)} 个表: {', '.join(missing)}")
            print("请检查 SQL 执行日志或重新执行创建语句。")
        
        return len(created_tables) == len(ebs_tables)
        
    except psycopg2.OperationalError as e:
        print(f"❌ 连接数据库失败: {e}")
        return False
    except Exception as e:
        print(f"❌ 验证过程中出错: {e}")
        return False

if __name__ == "__main__":
    # 检查 psycopg2
    try:
        import psycopg2
    except ImportError:
        print("正在安装 psycopg2-binary...")
        import subprocess
        subprocess.check_call([sys.executable, "-m", "pip", "install", "psycopg2-binary"])
        import psycopg2
    
    verify_tables()