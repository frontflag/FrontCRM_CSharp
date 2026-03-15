Write-Host "=== 执行 EBS 表创建 SQL ===" -ForegroundColor Cyan
Write-Host ""

# 检查 PostgreSQL 服务
$service = Get-Service -Name "postgresql-x64-16" -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq "Running") {
    Write-Host "✅ PostgreSQL 服务正在运行" -ForegroundColor Green
} else {
    Write-Host "❌ PostgreSQL 服务未运行" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 检查 SQL 文件
$sqlFile = "ebs_core_tables.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ SQL 文件不存在: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "✅ SQL 文件存在: $sqlFile" -ForegroundColor Green
Write-Host ""

# 显示 SQL 文件内容预览
Write-Host "SQL 文件内容预览:" -ForegroundColor Yellow
Get-Content $sqlFile -TotalCount 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
Write-Host "  ..." -ForegroundColor Gray
Write-Host ""

# 执行命令
Write-Host "执行以下命令创建表:" -ForegroundColor Yellow
$command = "psql -h localhost -p 5432 -U postgres -d FrontCRM -f `"$sqlFile`""
Write-Host "  $command" -ForegroundColor White
Write-Host ""

Write-Host "或者使用以下步骤:" -ForegroundColor Cyan
Write-Host "1. 设置环境变量:" -ForegroundColor Gray
Write-Host "   $env:PGPASSWORD='1234'" -ForegroundColor White
Write-Host "2. 执行命令:" -ForegroundColor Gray
Write-Host "   psql -h localhost -p 5432 -U postgres -d FrontCRM -f `"$sqlFile`"" -ForegroundColor White
Write-Host ""

Write-Host "如果 psql 命令不存在，请:" -ForegroundColor Yellow
Write-Host "1. 安装 PostgreSQL 客户端" -ForegroundColor Gray
Write-Host "2. 或将 PostgreSQL bin 目录添加到 PATH" -ForegroundColor Gray
Write-Host "3. 或使用 pgAdmin 手动执行 SQL" -ForegroundColor Gray
Write-Host ""

Write-Host "PostgreSQL 默认安装路径:" -ForegroundColor Gray
Write-Host "  C:\Program Files\PostgreSQL\16\bin\psql.exe" -ForegroundColor White
Write-Host ""

Write-Host "=== 完成 ===" -ForegroundColor Cyan