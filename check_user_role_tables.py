#!/usr/bin/env python3
import psycopg2

try:
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='FrontCRM',
        user='postgres',
        password='1234'
    )
    cursor = conn.cursor()
    
    # 查找用户和角色相关表
    cursor.execute('''
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
        AND (
            table_name LIKE '%%user%%' 
            OR table_name LIKE '%%role%%'
            OR table_name LIKE '%%permission%%'
            OR table_name LIKE '%%auth%%'
            OR table_name LIKE '%%login%%'
            OR table_name LIKE '%%account%%'
        )
        ORDER BY table_name;
    ''')
    
    tables = cursor.fetchall()
    
    print('='*60)
    print('用户和角色相关表检查')
    print('='*60)
    print()
    
    if tables:
        print(f'找到 {len(tables)} 个相关表:')
        for table in tables:
            print(f'  - {table[0]}')
    else:
        print('未找到用户和角色相关表')
        print()
        print('建议创建以下表:')
        print('  - user (用户表)')
        print('  - role (角色表)')
        print('  - permission (权限表)')
        print('  - userrole (用户角色关联表)')
        print('  - rolepermission (角色权限关联表)')
    
    print()
    print('='*60)
    print('数据库所有表:')
    print('='*60)
    
    cursor.execute('''
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
        ORDER BY table_name;
    ''')
    
    all_tables = cursor.fetchall()
    for i, table in enumerate(all_tables, 1):
        print(f'  {i:2d}. {table[0]}')
    
    print()
    print(f'总表数: {len(all_tables)}')
    print('='*60)
    
    cursor.close()
    conn.close()
    
except Exception as e:
    print(f'[ERROR] {e}')
    import traceback
    traceback.print_exc()
