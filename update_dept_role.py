import psycopg2

def main():
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='FrontCRM',
        user='postgres',
        password='1234'
    )
    cursor = conn.cursor()

    print('='*60)
    print('Update Department and Role Data')
    print('='*60)

    # 1. 更新部门
    cursor.execute("DELETE FROM department;")
    
    depts = [
        ('d1', 'SALE', 'Business Dept', None, None, 1, 1, 1),
        ('d2', 'PUR', 'Purchase Dept', None, None, 1, 2, 1),
        ('d3', 'LOG', 'Logistics Dept', None, None, 1, 3, 1),
        ('d4', 'FIN', 'Finance Dept', None, None, 1, 4, 1),
    ]
    
    for dept in depts:
        cursor.execute('''
            INSERT INTO "department" ("DepartmentId", "DepartmentCode", "DepartmentName", 
                "ParentId", "ManagerId", "Level", "SortOrder", "Status", "CreateTime")
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, CURRENT_TIMESTAMP);
        ''', dept)
    
    print('[OK] Departments updated: 4')

    # 2. 更新角色
    cursor.execute('DELETE FROM "role";')
    
    roles = [
        ('r0', 'admin', 'System Admin', 1, 'Full system access', 0, 1, True),
        ('r1', 'sale_mgr', 'Sales Manager', 2, 'Business dept manager', 1, 1, False),
        ('r2', 'sale_staff', 'Sales Staff', 2, 'Business dept staff', 2, 1, False),
        ('r3', 'pur_mgr', 'Purchase Manager', 2, 'Purchase dept manager', 3, 1, False),
        ('r4', 'pur_staff', 'Purchase Staff', 2, 'Purchase dept staff', 4, 1, False),
        ('r5', 'log_mgr', 'Logistics Manager', 2, 'Logistics dept manager', 5, 1, False),
        ('r6', 'log_staff', 'Logistics Staff', 2, 'Logistics dept staff', 6, 1, False),
        ('r7', 'fin_mgr', 'Finance Manager', 2, 'Finance dept manager', 7, 1, False),
        ('r8', 'fin_staff', 'Finance Staff', 2, 'Finance dept staff', 8, 1, False),
    ]
    
    for role in roles:
        cursor.execute('''
            INSERT INTO "role" ("RoleId", "RoleCode", "RoleName", "RoleType", 
                "Description", "SortOrder", "Status", "IsSystem", "CreateTime")
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, CURRENT_TIMESTAMP);
        ''', role)
    
    print('[OK] Roles updated: 9')

    conn.commit()

    # 3. 显示结果
    print()
    print('Departments:')
    cursor.execute('SELECT "DepartmentCode", "DepartmentName" FROM department ORDER BY "SortOrder";')
    for row in cursor.fetchall():
        print(f'  - {row[0]}: {row[1]}')

    print()
    print('Roles:')
    cursor.execute('SELECT "RoleCode", "RoleName" FROM "role" ORDER BY "SortOrder";')
    for row in cursor.fetchall():
        print(f'  - {row[0]}: {row[1]}')

    print()
    print('='*60)
    print('[OK] Update completed!')
    print('='*60)

    cursor.close()
    conn.close()

if __name__ == '__main__':
    main()
