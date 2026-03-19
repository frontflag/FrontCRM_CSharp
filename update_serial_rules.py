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
    
    print('=== 更新编码规则 ===\n')
    
    # 1. 修改客户 Prefix: Cus -> CUS
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'CUS' WHERE \"ModuleCode\" = 'Customer'")
    print(f'[OK] Customer: Cus -> CUS')
    
    # 2. 修改供应商 Prefix: Ven -> VEN
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'VEN' WHERE \"ModuleCode\" = 'Vendor'")
    print(f'[OK] Vendor: Ven -> VEN')
    
    # 3. 修改询价/需求: ModuleCode Inquiry -> RFQ, Prefix INQ -> RFQ
    cur.execute("UPDATE sys_serial_number SET \"ModuleCode\" = 'RFQ', \"Prefix\" = 'RFQ' WHERE \"ModuleCode\" = 'Inquiry'")
    print(f'[OK] Inquiry: ModuleCode Inquiry->RFQ, Prefix INQ->RFQ')
    
    # 4. 修改入库 Prefix: SIN -> STI
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'STI' WHERE \"ModuleCode\" = 'StockIn'")
    print(f'[OK] StockIn: SIN -> STI')
    
    # 5. 修改出库 Prefix: SOUT -> STO
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'STO' WHERE \"ModuleCode\" = 'StockOut'")
    print(f'[OK] StockOut: SOUT -> STO')
    
    # 6. 修改进项发票 Prefix: VINV -> INVI
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'INVI' WHERE \"ModuleCode\" = 'InputInvoice'")
    print(f'[OK] InputInvoice: VINV -> INVI')
    
    # 7. 修改销项发票 Prefix: SINV -> INVO
    cur.execute("UPDATE sys_serial_number SET \"Prefix\" = 'INVO' WHERE \"ModuleCode\" = 'OutputInvoice'")
    print(f'[OK] OutputInvoice: SINV -> INVO')
    
    # 8. 新增库存记录 (如果存在则更新，不存在则插入)
    cur.execute("SELECT \"Id\" FROM sys_serial_number WHERE \"ModuleCode\" = 'Stock'")
    row = cur.fetchone()
    if row:
        cur.execute("UPDATE sys_serial_number SET \"ModuleName\" = '库存', \"Prefix\" = 'STK' WHERE \"ModuleCode\" = 'Stock'")
        print(f'[OK] Stock: 更新已存在记录')
    else:
        # 找到最大ID
        cur.execute("SELECT MAX(\"Id\") FROM sys_serial_number")
        max_id = cur.fetchone()[0] or 0
        new_id = max_id + 1
        cur.execute("""
            INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
            VALUES (%s, 'Stock', '库存', 'STK', 4, 0, FALSE, FALSE, NOW())
        """, (new_id,))
        print(f'[OK] Stock: 新增记录 Id={new_id}')
    
    conn.commit()
    
    # 查询更新后的结果
    print('\n=== 更新后的编码规则 ===')
    cur.execute('SELECT "Id", "ModuleCode", "ModuleName", "Prefix" FROM sys_serial_number ORDER BY "Id"')
    rows = cur.fetchall()
    for row in rows:
        print(f'  {row[0]}. {row[1]} ({row[2]}) - {row[3]}')
    
    cur.close()
    conn.close()
    print(f'\n[OK] 共 {len(rows)} 条编码规则')
except Exception as e:
    print(f'错误: {e}')
