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
    
    print('=== stockinfo 表结构 ===')
    cur.execute("""
        SELECT column_name, data_type, is_nullable 
        FROM information_schema.columns 
        WHERE table_name = 'stockinfo' 
        ORDER BY ordinal_position
    """)
    columns = cur.fetchall()
    for col in columns:
        nullable = "NULL" if col[2] == 'YES' else "NOT NULL"
        print(f'  {col[0]:<25} {col[1]:<20} {nullable}')
    
    print('')
    print('=== stockinfo 表数据量 ===')
    cur.execute('SELECT COUNT(*) FROM stockinfo')
    count = cur.fetchone()[0]
    print(f'  总记录数: {count}')
    
    print('')
    print('=== 字段对比 ===')
    print('stock 表特有字段:')
    stock_cols = ['Quantity', 'AvailableQuantity', 'LockedQuantity']
    for col in stock_cols:
        print(f'  - {col}')
    
    print('')
    print('stockinfo 表特有字段 (Qty系列):')
    info_cols = ['Qty', 'QtyStockOut', 'QtyOccupy', 'QtySales', 'QtyRepertory', 'QtyRepertoryAvailable']
    for col in info_cols:
        print(f'  - {col}')
    
    cur.close()
    conn.close()
except Exception as e:
    print(f'错误: {e}')
