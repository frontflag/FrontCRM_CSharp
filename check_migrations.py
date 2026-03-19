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
    
    # 查询已应用的迁移
    print('=== 已应用的 EF Core 迁移 ===')
    cur.execute('SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId"')
    migrations = cur.fetchall()
    
    for m in migrations:
        print(f'  - {m[0]}')
    
    print(f'\n总共 {len(migrations)} 个迁移已应用')
    
    # 检查是否包含 SerialNumber 迁移
    serial_migration = [m for m in migrations if 'SerialNumber' in m[0]]
    print(f'\n包含 SerialNumber 的迁移: {len(serial_migration)} 个')
    for m in serial_migration:
        print(f'  ✓ {m[0]}')
    
    cur.close()
    conn.close()
except Exception as e:
    print(f'错误: {e}')
