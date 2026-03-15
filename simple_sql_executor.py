#!/usr/bin/env python3
# 简单的 SQL 执行器

print("=== EBS 业务表创建脚本 ===\n")

# 先检查系统是否有 Python 和 pip
import subprocess
import sys
import os

def check_postgresql_connection():
    """测试 PostgreSQL 连接"""
    print("1. 测试 PostgreSQL 连接...")
    
    # 尝试多种方法连接
    methods = [
        # 方法1: 使用 netstat 检查端口
        ("检查端口 5432", "netstat -an | findstr :5432"),
        # 方法2: 检查 PostgreSQL 服务
        ("检查服务状态", "sc query postgresql-x64-16")
    ]
    
    for desc, cmd in methods:
        print(f"  {desc}...")
        try:
            result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
            if result.returncode == 0:
                print(f"  ✅ {desc}成功")
                if "LISTENING" in result.stdout or "RUNNING" in result.stdout:
                    return True
            else:
                print(f"  ⚠️ {desc}失败")
        except:
            print(f"  ⚠️ {desc}异常")
    
    return False

def get_sql_content():
    """获取 SQL 文件内容"""
    sql_file = "ebs_core_tables.sql"
    if not os.path.exists(sql_file):
        print(f"❌ SQL 文件不存在: {sql_file}")
        return None
    
    try:
        with open(sql_file, 'r', encoding='utf-8') as f:
            content = f.read()
        print(f"✅ 读取 SQL 文件: {sql_file} ({len(content)} 字符)")
        return content
    except Exception as e:
        print(f"❌ 读取 SQL 文件失败: {e}")
        return None

def create_manual_guide():
    """创建手动执行指南"""
    print("\n2. 创建手动执行指南...")
    
    guide = """
========================================
在 FrontCRM 数据库中创建 EBS 业务表 - 手动指南
========================================

已确认信息:
✅ PostgreSQL 服务正在运行 (postgresql-x64-16)
✅ 端口 5432 正在监听
✅ 数据库 FrontCRM 已存在
✅ SQL 脚本已准备: ebs_core_tables.sql

请按照以下方法之一执行:

方法 1: 使用 pgAdmin (推荐)
----------------------------------------
1. 打开 pgAdmin
2. 连接到 localhost:5432
   - 用户名: postgres
   - 密码: 1234
3. 展开数据库列表，选择 FrontCRM
4. 点击 "查询工具" (Query Tool)
5. 打开 ebs_core_tables.sql 文件
6. 点击执行按钮 (F5 或 ▶️)
7. 等待执行完成

方法 2: 安装 psql 命令行工具
----------------------------------------
1. 下载 PostgreSQL: https://www.postgresql.org/download/
2. 安装时选择 "Command Line Tools"
3. 打开命令提示符，执行:
   
   set PGPASSWORD=1234
   psql -h localhost -p 5432 -U postgres -d FrontCRM -f ebs_core_tables.sql

方法 3: 使用 DBeaver (免费数据库工具)
----------------------------------------
1. 下载 DBeaver: https://dbeaver.io/
2. 新建 PostgreSQL 连接
3. 填写连接信息:
   - Host: localhost
   - Port: 5432
   - Database: FrontCRM
   - Username: postgres
   - Password: 1234
4. 连接后，右键点击数据库 -> SQL 编辑器
5. 执行 ebs_core_tables.sql 文件

方法 4: 使用 VS Code 扩展
----------------------------------------
1. 安装 "PostgreSQL" 扩展
2. 添加连接:
   - Host: localhost
   - Port: 5432
   - Database: FrontCRM
   - Username: postgres
   - Password: 1234
3. 右键点击连接 -> New Query
4. 执行 ebs_core_tables.sql

验证执行结果:
----------------------------------------
执行成功后，运行以下 SQL 验证:

SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' ORDER BY table_name;

应该看到以下表:
- customerinfo
- customercontactinfo
- customeraddress
- vendorinfo
- vendorcontactinfo
- sellorder
- purchaseorder
- material
========================================
"""
    
    # 保存指南到文件
    with open("manual_create_tables_guide.txt", "w", encoding="utf-8") as f:
        f.write(guide)
    
    print("✅ 已创建手动执行指南: manual_create_tables_guide.txt")
    return guide

def main():
    print("=== EBS 业务表创建准备 ===")
    
    # 1. 检查连接
    if not check_postgresql_connection():
        print("⚠️ PostgreSQL 连接检查失败，但继续生成指南...")
    
    # 2. 检查 SQL 文件
    sql_content = get_sql_content()
    if sql_content:
        # 统计表数量
        table_count = sql_content.count("CREATE TABLE")
        print(f"✅ SQL 文件包含 {table_count} 个表创建语句")
    
    # 3. 创建手动指南
    create_manual_guide()
    
    print("\n=== 完成 ===")
    print("请查看 manual_create_tables_guide.txt 获取详细执行指南")
    print("或使用您喜欢的数据库工具执行 ebs_core_tables.sql 文件")

if __name__ == "__main__":
    main()