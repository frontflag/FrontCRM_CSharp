# 直接查询数据库
$connectionString = "Host=localhost;Port=5432;Database=FrontCRM_Dev;Username=postgres;Password=your_password"

try {
    # 使用psql查询
    $env:PGPASSWORD = "your_password"
    $result = psql -h localhost -p 5432 -U postgres -d FrontCRM_Dev -c "SELECT id, customer_code, official_name, level, type, status, create_time FROM customerinfo LIMIT 5;" 2>&1
    Write-Host "查询结果:" -ForegroundColor Cyan
    $result
} catch {
    Write-Host "查询错误: $_" -ForegroundColor Red
    Write-Host "尝试使用.NET方式查询..."
}
