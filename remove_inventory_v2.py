import psycopg2

try:
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        dbname='FrontCRM',
        user='postgres',
        password='1234'
    )
    cur = conn.cursor()
    
    # 查询当前 Inventory 记录
    print('=== 删除前 ===')
    cur.execute("SELECT \"Id\", \"ModuleCode\", \"ModuleName\", \"Prefix\" FROM sys_serial_number WHERE \"ModuleCode\" = 'Inventory'")
    row = cur.fetchone()
    if row:
        print(f'找到记录: ID={row[0]}, Code={row[1]}, Name={row[2]}, Prefix={row[3]}')
        
        # 删除 Inventory 记录
        cur.execute("DELETE FROM sys_serial_number WHERE \"ModuleCode\" = 'Inventory'")
        conn.commit()
        print('')
        print('[OK] 已删除 Inventory 记录')
    else:
        print('未找到 Inventory 记录')
    
    # 查询剩余记录
    print('')
    print('=== 删除后 ===')
    cur.execute('SELECT "Id", "ModuleCode", "ModuleName", "Prefix" FROM sys_serial_number ORDER BY "Id"')
    rows = cur.fetchall()
    print(f'剩余 {len(rows)} 条编码规则:')
    for row in rows:
        print(f'  {row[0]}. {row[1]} ({row[2]}) - {row[3]}')
    
    cur.close()
    conn.close()
    print('')
    print('[OK] 数据库操作完成')
except Exception as e:
    print(f'错误: {e}')
