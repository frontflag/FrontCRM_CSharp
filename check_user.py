#!/usr/bin/env python3
import psycopg2
import os

# 设置环境变量
os.environ['PGCLIENTENCODING'] = 'UTF8'

try:
    conn = psycopg2.connect(
        host='localhost', 
        database='frontcrm', 
        user='postgres', 
        password='123456',
        options='-c client_encoding=utf8'
    )
    cur = conn.cursor()
    
    # 查询指定用户
    cur.execute('SELECT "Id", "UserName", "Email", "Status", "PasswordPlain" FROM "user" WHERE "Email" = %s', ('3161798@qq.com',))
    row = cur.fetchone()
    
    if row:
        print('User found:')
        print(f'  ID: {row[0]}')
        print(f'  Username: {row[1]}')
        print(f'  Email: {row[2]}')
        print(f'  Status: {row[3]} (1=Active, 0=Inactive)')
        print(f'  Plain Password: {row[4]}')
    else:
        print('User not found!')
        print('\nExisting users:')
        cur.execute('SELECT "UserName", "Email", "Status" FROM "user" LIMIT 10')
        rows = cur.fetchall()
        if rows:
            for r in rows:
                status_str = 'Active' if r[2] == 1 else 'Inactive'
                print(f'  Username: {r[0]}, Email: {r[1]}, Status: {status_str}')
        else:
            print('  No users in database')
    
    cur.close()
    conn.close()
except Exception as e:
    print(f'Error: {e}')
