#!/usr/bin/env python3
"""
测试本地 PostgreSQL 连接并创建 FrontCRM 数据库
"""

import subprocess
import sys
import os

def run_command(cmd):
    """运行命令并返回结果"""
    try:
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return -1, "", str(e)

def main():
    print("=" * 60)
    print("FrontCRM 本地 PostgreSQL 配置工具")
    print("=" * 60)
    print()
    
    # 1. 检查 PostgreSQL 服务
    print("1. 检查 PostgreSQL 服务状态...")
    returncode, stdout, stderr = run_command('sc query postgresql-x64-16')
    
    if "RUNNING" in stdout:
        print("   ✅ PostgreSQL 服务正在运行")
    else:
        print("   ❌ PostgreSQL 服务未运行或未找到")
        print("   请启动 PostgreSQL 服务后再继续")
        return
    
    print()
    
    # 2. 测试连接
    print("2. 测试 PostgreSQL 连接...")
    print("   使用密码: 1234")
    
    # 尝试连接并创建数据库
    create_db_sql = """
    SELECT '检查连接...';
    
    -- 检查 FrontCRM 数据库是否存在
    DO $$
    BEGIN
        IF EXISTS (SELECT 1 FROM pg_database WHERE datname = 'FrontCRM') THEN
            RAISE NOTICE 'FrontCRM 数据库已存在';
        ELSE
            -- 创建数据库
            CREATE DATABASE "FrontCRM"
                WITH 
                OWNER = postgres
                ENCODING = 'UTF8'
                LC_COLLATE = 'Chinese (Simplified)_China.936'
                LC_CTYPE = 'Chinese (Simplified)_China.936'
                TABLESPACE = pg_default
                CONNECTION LIMIT = -1
                IS_TEMPLATE = False;
            RAISE NOTICE 'FrontCRM 数据库创建成功';
        END IF;
    END $$;
    """
    
    # 将 SQL 保存到临时文件
    with open("temp_create_db.sql", "w", encoding="utf-8") as f:
        f.write(create_db_sql)
    
    # 尝试使用 psql 执行（如果可用）
    psql_paths = [
        r"C:\Program Files\PostgreSQL\16\bin\psql.exe",
        r"C:\Program Files\PostgreSQL\15\bin\psql.exe",
        r"C:\Program Files\PostgreSQL\14\bin\psql.exe",
        r"C:\Program Files\PostgreSQL\13\bin\psql.exe",
        "psql.exe"
    ]
    
    psql_found = None
    for path in psql_paths:
        if os.path.exists(path):
            psql_found = path
            break
    
    if psql_found:
        print(f"   找到 psql: {psql_found}")
        
        # 设置环境变量
        os.environ['PGPASSWORD'] = '1234'
        
        # 尝试连接并执行 SQL
        cmd = f'"{psql_found}" -h localhost -p 5432 -U postgres -d postgres -f temp_create_db.sql'
        returncode, stdout, stderr = run_command(cmd)
        
        if returncode == 0:
            print("   ✅ 连接成功!")
            if "FrontCRM 数据库已存在" in stdout:
                print("   ✅ FrontCRM 数据库已存在")
            elif "FrontCRM 数据库创建成功" in stdout:
                print("   ✅ FrontCRM 数据库创建成功")
            print(f"   输出: {stdout}")
        else:
            print(f"   ❌ 连接失败")
            print(f"   错误: {stderr}")
            
            # 尝试其他常见密码
            print()
            print("   尝试其他常见密码...")
            common_passwords = ['postgres', 'postgres123', '123456', 'password', '']
            
            for pwd in common_passwords:
                os.environ['PGPASSWORD'] = pwd
                cmd = f'"{psql_found}" -h localhost -p 5432 -U postgres -d postgres -c "SELECT 1;"'
                returncode, stdout, stderr = run_command(cmd)
                
                if returncode == 0:
                    print(f"   ✅ 成功! 密码是: '{pwd if pwd else '[空密码]'}'")
                    
                    # 更新 appsettings.json
                    print()
                    print("   更新项目连接字符串...")
                    
                    # 更新 appsettings.json
                    appsettings_path = "CRM.API/appsettings.json"
                    if os.path.exists(appsettings_path):
                        with open(appsettings_path, "r", encoding="utf-8") as f:
                            content = f.read()
                        
                        # 更新密码
                        new_content = content.replace(
                            'Password=your_password',
                            f'Password={pwd}'
                        ).replace(
                            'Password=1234',
                            f'Password={pwd}'
                        )
                        
                        with open(appsettings_path, "w", encoding="utf-8") as f:
                            f.write(new_content)
                        
                        print(f"   ✅ {appsettings_path} 已更新，密码: '{pwd}'")
                    
                    # 使用正确的密码创建数据库
                    os.environ['PGPASSWORD'] = pwd
                    cmd = f'"{psql_found}" -h localhost -p 5432 -U postgres -d postgres -c "CREATE DATABASE \\"FrontCRM\\";"'
                    returncode, stdout, stderr = run_command(cmd)
                    
                    if returncode == 0:
                        print("   ✅ FrontCRM 数据库创建成功")
                    else:
                        # 数据库可能已存在
                        print("   ⚠️  数据库可能已存在或创建失败")
                    
                    break
                else:
                    print(f"   ❌ 密码 '{pwd if pwd else '[空密码]'}' 失败")
    else:
        print("   ⚠️  未找到 psql 命令")
        print("   请安装 PostgreSQL 客户端或手动执行以下操作:")
        print("   1. 使用 pgAdmin 连接")
        print("   2. 执行 SQL: CREATE DATABASE \"FrontCRM\";")
        print("   3. 验证密码是否为 '1234'")
    
    # 清理临时文件
    if os.path.exists("temp_create_db.sql"):
        os.remove("temp_create_db.sql")
    
    print()
    print("=" * 60)
    print("操作完成")
    print("=" * 60)
    print()
    print("下一步:")
    print("1. 测试连接: 使用 pgAdmin 或 psql 连接")
    print("2. 连接字符串: Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234")
    print("3. 如果需要，手动修改密码")

if __name__ == "__main__":
    main()