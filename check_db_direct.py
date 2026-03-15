#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
直接连接本地数据库检查新表
"""

import subprocess
import sys

def check_database():
    try:
        import psycopg2
        print('psycopg2 模块已安装')
        
        # 连接数据库
        conn = psycopg2.connect(
            host='localhost',
            port=5432,
            database='FrontCRM',
            user='postgres',
            password='1234'
        )
        print('数据库连接成功!')
        
        cursor = conn.cursor()
        
        # 检查新表
        cursor.execute('''
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public'
            AND table_name IN ('invoice', 'invoiceitem', 'payment', 'paymentitem', 
                               'receipt', 'receiptitem', 'businesslog', 'businesslogdetail')
            ORDER BY table_name;
        ''')
        
        tables = cursor.fetchall()
        print(f'\n找到 {len(tables)} 个新表:')
        for table in tables:
            print(f'  - {table[0]}')
        
        # 检查总表数
        cursor.execute('''
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema = 'public';
        ''')
        total = cursor.fetchone()[0]
        print(f'\n数据库总表数: {total}')
        
        # 检查索引
        cursor.execute('''
            SELECT tablename, COUNT(*) as idx_count
            FROM pg_indexes
            WHERE schemaname = 'public'
            AND tablename IN ('invoice', 'invoiceitem', 'payment', 'paymentitem', 
                              'receipt', 'receiptitem', 'businesslog', 'businesslogdetail')
            GROUP BY tablename
            ORDER BY tablename;
        ''')
        indexes = cursor.fetchall()
        print(f'\n索引统计:')
        for idx in indexes:
            print(f'  - {idx[0]}: {idx[1]} 个索引')
        
        cursor.close()
        conn.close()
        
        print('\n' + '='*60)
        if len(tables) == 8:
            print('[OK] 验证通过: 所有 8 个新表已创建!')
            print('[OK] 数据库更新成功!')
        else:
            print(f'[WARNING] 验证结果: 期望 8 个新表，实际找到 {len(tables)} 个')
        print('='*60)
        
    except ImportError:
        print('psycopg2 模块未安装，正在安装...')
        subprocess.check_call([sys.executable, '-m', 'pip', 'install', 'psycopg2-binary'])
        print('安装完成，请重新运行此脚本')
    except Exception as e:
        print(f'[ERROR] 错误: {e}')
        import traceback
        traceback.print_exc()

if __name__ == '__main__':
    check_database()
