import sys
print("测试 psycopg2 模块...")

try:
    import psycopg2
    print("✅ psycopg2 模块已安装")
    print(f"版本: {psycopg2.__version__ if hasattr(psycopg2, '__version__') else '未知'}")
except ImportError as e:
    print("❌ psycopg2 模块未安装")
    print(f"错误: {e}")
    print("\n请运行以下命令安装:")
    print("pip install psycopg2-binary")