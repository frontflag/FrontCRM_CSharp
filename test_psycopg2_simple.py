import sys
print("测试 psycopg2 模块...")

try:
    import psycopg2
    print("OK: psycopg2 模块已安装")
    if hasattr(psycopg2, '__version__'):
        print("版本:", psycopg2.__version__)
except ImportError as e:
    print("ERROR: psycopg2 模块未安装")
    print("错误:", e)
    print("\n请运行以下命令安装:")
    print("pip install psycopg2-binary")