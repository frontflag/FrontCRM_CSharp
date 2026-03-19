@echo off
set PGPASSWORD=123456
cd /d "d:\MyProject\FrontCRM_CSharp"

python -c "import psycopg2; conn = psycopg2.connect(host='localhost', database='frontcrm', user='postgres', password='123456'); cur = conn.cursor(); cur.execute(\"SELECT \"UserName\", \"Email\", \"Status\", \"PasswordPlain\" FROM \"user\" WHERE \"Email\"='3161798@qq.com'\"); row = cur.fetchone(); print('FOUND' if row else 'NOT FOUND'); cur.close(); conn.close()"

pause
