import psycopg2

conn = psycopg2.connect(host='localhost', port='5432', database='FrontCRM', user='postgres', password='1234')
conn.autocommit = True
cursor = conn.cursor()

# 获取所有迁移文件
migrations = [
    ('20260316063302_InitialCreate', '8.0.2'),
    ('20260316072003_AddSerialNumberAndErrorLogAndSoftDelete', '8.0.2'),
    ('20260316095624_AddComponentCache', '8.0.2'),
    ('20260316100000_AddSysTables', '8.0.2'),
    ('20260317000000_AddVendorAndInventoryTables', '8.0.2'),
    ('20260317024250_AddContactHistoryFields', '8.0.2'),
    ('20260317030000_AddCustomerBlacklistFields', '8.0.2'),
    ('20260317145231_AddRFQTables', '8.0.2'),
    ('20260317162715_AddQuoteTables', '8.0.2'),
    ('20260317170015_AddSalesAndPurchaseOrderTables', '8.0.2'),
    ('20260318000000_AddDocumentTables', '8.0.2'),
]

for migration_id, version in migrations:
    try:
        cursor.execute(
            'INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (%s, %s) ON CONFLICT DO NOTHING',
            (migration_id, version)
        )
        print(f'[OK] {migration_id}')
    except Exception as e:
        print(f'[ERROR] {migration_id}: {e}')

# 删除刚创建的迁移
try:
    cursor.execute('DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = %s', ('20260318010024_UpdateAfterMerge',))
    print('[OK] 删除 UpdateAfterMerge 迁移记录')
except Exception as e:
    print(f'[WARN] {e}')

conn.close()
print('\n[OK] 迁移历史修复完成')
