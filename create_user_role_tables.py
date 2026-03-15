#!/usr/bin/env python3
import psycopg2

try:
    with open('ebs_user_role_tables.sql', 'r', encoding='utf-8') as f:
        sql = f.read()
    
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='FrontCRM',
        user='postgres',
        password='1234'
    )
    cursor = conn.cursor()
    
    # 执行 SQL
    cursor.execute(sql)
    conn.commit()
    
    print('[OK] 用户角色权限表创建成功!')
    
    # 验证
    cursor.execute('''
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name IN ('user', 'role', 'permission', 'userrole', 'rolepermission', 'department')
        ORDER BY table_name;
    ''')
    
    tables = cursor.fetchall()
    print(f'\n创建了 {len(tables)} 个表:')
    for table in tables:
        print(f'  - {table[0]}')
    
    cursor.close()
    conn.close()
    
except Exception as e:
    print(f'[ERROR] {e}')
    import traceback
    traceback.print_exc()
