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
    
    print('=== 库存相关表 ===')
    print('')
    
    # 查询库存相关表
    stock_tables = ['stock', 'stockin', 'stockinitem', 'stockout', 'stockoutitem', 'stockinfo']
    
    for table in stock_tables:
        cur.execute(f"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '{table}')")
        exists = cur.fetchone()[0]
        status = "[OK] 存在" if exists else "[NO] 不存在"
        print(f'  {table:<15} {status}')
    
    print('')
    print('=== stock 表结构 ===')
    cur.execute("""
        SELECT column_name, data_type, is_nullable 
        FROM information_schema.columns 
        WHERE table_name = 'stock' 
        ORDER BY ordinal_position
    """)
    columns = cur.fetchall()
    for col in columns:
        nullable = "NULL" if col[2] == 'YES' else "NOT NULL"
        print(f'  {col[0]:<25} {col[1]:<20} {nullable}')
    
    print('')
    print('=== stock 表数据量 ===')
    cur.execute('SELECT COUNT(*) FROM stock')
    count = cur.fetchone()[0]
    print(f'  总记录数: {count}')
    
    if count > 0:
        print('')
        print('=== 前5条库存记录 ===')
        cur.execute('SELECT "StockId", "MaterialId", "WarehouseId", "Qty", "QtyRepertory", "QtyRepertoryAvailable" FROM stock LIMIT 5')
        rows = cur.fetchall()
        for row in rows:
            print(f'  {row}')
    
    cur.close()
    conn.close()
except Exception as e:
    print(f'错误: {e}')
