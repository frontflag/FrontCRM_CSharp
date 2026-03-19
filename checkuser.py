import psycopg2
conn = psycopg2.connect(host='localhost', database='frontcrm', user='postgres', password='123456')
cur = conn.cursor()
cur.execute('SELECT "UserName", "Email", "Status" FROM "user"')
for r in cur.fetchall():
    print(r[0], r[1], r[2])
cur.close()
conn.close()
