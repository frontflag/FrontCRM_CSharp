#!/bin/bash
# 转换 MySQL SQL 到 PostgreSQL

input_file="34_EBS业务数据库表结构_SQL脚本.sql"
output_file="34_EBS业务数据库表结构_PostgreSQL.sql"

# 清理旧文件
rm -f "$output_file"

# 逐行处理
while IFS= read -r line; do
    # 移除 MySQL 外键检查
    if [[ "$line" == *"SET FOREIGN_KEY_CHECKS"* ]]; then
        continue
    fi
    
    # 替换反引号为双引号
    line="${line//\`/\"}"
    
    # 移除 MySQL 引擎选项
    line="${line//ENGINE=InnoDB/}"
    line="${line//DEFAULT CHARSET=utf8mb4/}"
    
    # 替换数据类型
    line="${line//tinyint/smallint}"
    line="${line//bit(1)/boolean}"
    line="${line//datetime/timestamp}"
    
    # 替换布尔值
    line="${line//b'0'/false}"
    line="${line//b'1'/true}"
    
    # 替换时间戳
    line="${line//CURRENT_TIMESTAMP/NOW()}"
    
    # 写入输出文件
    echo "$line" >> "$output_file"
done < "$input_file"

echo "转换完成！输出文件: $output_file"