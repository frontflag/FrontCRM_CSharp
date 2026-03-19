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
    
    # 查询所有表
    cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name")
    tables = cur.fetchall()
    
    print('=== 数据库 FrontCRM 中的表 ===')
    print(f'总共 {len(tables)} 个表\n')
    
    for t in tables:
        print(f'  - {t[0]}')
    
    # 特别检查 sys_serial_number
    print('\n=== 检查 sys_serial_number 表 ===')
    cur.execute("SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'sys_serial_number')")
    exists = cur.fetchone()[0]
    print(f'表存在: {exists}')
    
    if exists:
        cur.execute('SELECT * FROM "sys_serial_number"')
        rows = cur.fetchall()
        print(f'\n表中数据: {len(rows)} 条')
        for row in rows:
            print(f'  {row}')
    
    cur.close()
    conn.close()
except Exception as e:
    print(f'错误: {e}')
