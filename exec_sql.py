import psycopg2

with open('create_serial_number_table.sql', 'r', encoding='utf-8') as f:
    sql = f.read()

conn = psycopg2.connect(
    host='localhost',
    port=5432,
    dbname='FrontCRM',
    user='postgres',
    password='1234'
)
cur = conn.cursor()
cur.execute(sql)
conn.commit()

# 查询结果
cur.execute('SELECT * FROM sys_serial_number ORDER BY "Id"')
rows = cur.fetchall()
print('=== sys_serial_number 表数据 ===')
for row in rows:
    print(f'ID={row[0]}, Code={row[1]}, Name={row[2]}, Prefix={row[3]}, Seq={row[5]}')

cur.close()
conn.close()
print(f'\n共 {len(rows)} 条记录')
