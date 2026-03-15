$inputFile = '34_EBS业务数据库表结构_SQL脚本.sql'
$outputFile = '34_EBS业务数据库表结构_PostgreSQL.sql'

if (Test-Path $inputFile) {
    $content = Get-Content $inputFile -Raw -Encoding UTF8
    
    # 基本转换
    $content = $content -replace '`', '"'
    $content = $content -replace 'ENGINE=InnoDB\s*', ''
    $content = $content -replace 'DEFAULT CHARSET=utf8mb4\s*', ''
    $content = $content -replace 'tinyint', 'smallint'
    $content = $content -replace 'bit\(1\)', 'boolean'
    $content = $content -replace 'datetime', 'timestamp'
    $content = $content -replace "b'0'", 'false'
    $content = $content -replace "b'1'", 'true'
    $content = $content -replace 'CURRENT_TIMESTAMP', 'NOW()'
    
    # 移除 MySQL 外键检查
    $content = $content -replace 'SET FOREIGN_KEY_CHECKS = 1;', ''
    
    Set-Content -Path $outputFile -Value $content -Encoding UTF8
    Write-Host "转换完成！输出文件: $outputFile"
} else {
    Write-Host "错误: 找不到输入文件 $inputFile"
}