@echo off
echo ========================================
echo PostgreSQL 连接测试
echo ========================================
echo.

echo 1. 检查 PostgreSQL 服务状态...
netstat -an | findstr :5432 > nul
if %errorlevel% equ 0 (
    echo   ✅ PostgreSQL 端口 5432 正在监听
) else (
    echo   ❌ PostgreSQL 端口未监听
    echo   请确保 PostgreSQL 服务已启动
    pause
    exit /b 1
)

echo.
echo 2. 检查数据库连接...
echo   使用参数:
echo     Host: localhost
echo     Port: 5432
echo     User: postgres
echo     Password: 1234

echo.
echo 3. 尝试连接...
echo   这可能需要安装 psycopg2-binary 模块
echo   如果失败，请运行: pip install psycopg2-binary
echo.

python -c "import psycopg2; print('正在连接到 PostgreSQL...')" 2>nul
if %errorlevel% neq 0 (
    echo   ❌ psycopg2 模块未安装
    echo   请运行: pip install psycopg2-binary
    pause
    exit /b 1
)

echo   ✅ psycopg2 模块已安装

echo.
echo 4. 测试实际连接...
python -c "
import psycopg2
try:
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='postgres',
        user='postgres',
        password='1234'
    )
    cursor = conn.cursor()
    cursor.execute('SELECT version()')
    version = cursor.fetchone()[0]
    print('   ✅ 连接成功!')
    print('   PostgreSQL 版本:', version)
    cursor.close()
    conn.close()
except psycopg2.OperationalError as e:
    print('   ❌ 连接失败')
    print('   错误信息:', str(e))
    print()
    print('   可能的原因:')
    print('   1. 密码错误 (当前使用: 1234)')
    print('   2. PostgreSQL 配置问题')
    print('   3. 权限不足')
except Exception as e:
    print('   ❌ 未知错误:', str(e))
"

echo.
echo ========================================
echo 测试完成
echo ========================================
pause