# 简单转换脚本
$inputPath = 'd:\MyProject\FrontCRM_CSharp\文档\34_EBS业务数据库表结构_SQL脚本.sql'
$outputPath = 'd:\MyProject\FrontCRM_CSharp\文档\34_EBS业务数据库表结构_PostgreSQL.sql'

$content = Get-Content $inputPath -Raw -Encoding UTF8
$content = $content -replace '`', '"'
$content = $content -replace 'ENGINE=InnoDB\s*', ''
$content = $content -replace 'DEFAULT CHARSET=utf8mb4\s*', ''
$content = $content -replace 'tinyint', 'smallint'
$content = $content -replace 'bit\(1\)', 'boolean'
$content = $content -replace 'datetime', 'timestamp'
$content = $content -replace 'b''0''', 'false'
$content = $content -replace 'b''1''', 'true'
$content = $content -replace 'CURRENT_TIMESTAMP', 'NOW()'
$content = $content -replace 'SET FOREIGN_KEY_CHECKS = 1;', ''

Set-Content -Path $outputPath -Value $content -Encoding UTF8
Write-Host "转换完成: $outputPath"