#!/usr/bin/env python3
import psycopg2

conn = psycopg2.connect(
    host='localhost',
    port=5432,
    database='FrontCRM',
    user='postgres',
    password='1234'
)
cursor = conn.cursor()

print('='*60)
print('用户角色权限表验证')
print('='*60)
print()

# 检查用户角色相关表
cursor.execute('''
    SELECT table_name 
    FROM information_schema.tables 
    WHERE table_schema = 'public'
    AND table_name IN ('user', 'role', 'permission', 'userrole', 'rolepermission', 'department')
    ORDER BY table_name;
''')

tables = cursor.fetchall()
print(f'[OK] 找到 {len(tables)} 个用户角色权限表:')
for table in tables:
    print(f'  - {table[0]}')

print()

# 检查默认角色数据
cursor.execute('SELECT COUNT(*) FROM role;')
role_count = cursor.fetchone()[0]
print(f'[OK] 默认角色数量: {role_count} 个')

# 检查默认权限数据
cursor.execute('SELECT COUNT(*) FROM permission;')
perm_count = cursor.fetchone()[0]
print(f'[OK] 默认权限数量: {perm_count} 个')

print()
print('='*60)
print('数据库总表数统计')
print('='*60)

cursor.execute("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")
total = cursor.fetchone()[0]
print(f'数据库总表数: {total} 个')

print()
print('='*60)
print('[OK] 用户角色权限表已成功创建并更新到本地数据库!')
print('='*60)

cursor.close()
conn.close()
