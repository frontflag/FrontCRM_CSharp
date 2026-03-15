Write-Host "=== PostgreSQL 连接状态检查 ===" -ForegroundColor Cyan
Write-Host ""

# 检查 PostgreSQL 服务
Write-Host "1. 检查 PostgreSQL 服务状态..." -ForegroundColor Yellow
$postgresServices = Get-Service -Name *postgres* -ErrorAction SilentlyContinue

if ($postgresServices) {
    Write-Host "   ✅ 找到 PostgreSQL 服务:" -ForegroundColor Green
    foreach ($service in $postgresServices) {
        $status = if ($service.Status -eq "Running") { "✅ 运行中" } else { "❌ 已停止" }
        Write-Host "      - $($service.Name): $($service.DisplayName) - $status" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Red" })
    }
} else {
    Write-Host "   ❌ 未找到 PostgreSQL 服务" -ForegroundColor Red
}

Write-Host ""

# 检查端口
Write-Host "2. 检查端口 5432..." -ForegroundColor Yellow
$port5432 = Get-NetTCPConnection -LocalPort 5432 -ErrorAction SilentlyContinue

if ($port5432) {
    Write-Host "   ✅ 端口 5432 正在监听:" -ForegroundColor Green
    foreach ($conn in $port5432) {
        Write-Host "      - 状态: $($conn.State) | 本地地址: $($conn.LocalAddress):$($conn.LocalPort)" -ForegroundColor Green
    }
} else {
    Write-Host "   ⚠️  端口 5432 未找到监听" -ForegroundColor Yellow
}

Write-Host ""

# 检查是否可以连接到数据库
Write-Host "3. 测试数据库连接..." -ForegroundColor Yellow
Write-Host "   尝试连接参数:"
Write-Host "   - Server: localhost" -ForegroundColor Gray
Write-Host "   - Port: 5432" -ForegroundColor Gray
Write-Host "   - Username: postgres" -ForegroundColor Gray
Write-Host "   - Password: 1234" -ForegroundColor Gray

# 尝试使用 .NET 连接
try {
    # 检查 Npgsql 是否可用
    Add-Type -Path "C:\Program Files\PostgreSQL\*\lib\Npgsql.dll" -ErrorAction SilentlyContinue
    Add-Type -Path "C:\Program Files\Npgsql\*\Npgsql.dll" -ErrorAction SilentlyContinue
    
    $npgsqlFound = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.FullName -like "*Npgsql*" }
    
    if ($npgsqlFound) {
        Write-Host "   ✅ Npgsql 连接库已加载" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Npgsql 连接库未找到" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Npgsql 连接库加载失败" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "4. 建议的下一步操作:" -ForegroundColor Cyan

if ($postgresServices -and ($postgresServices.Status -contains "Running")) {
    Write-Host "   ✅ PostgreSQL 服务正在运行" -ForegroundColor Green
    Write-Host "   请使用以下工具测试连接:" -ForegroundColor Gray
    Write-Host "   1. pgAdmin (如果已安装)" -ForegroundColor Gray
    Write-Host "   2. psql 命令行工具" -ForegroundColor Gray
    Write-Host "   3. DBeaver 或其他数据库客户端" -ForegroundColor Gray
    Write-Host "" 
    Write-Host "   连接字符串参考:" -ForegroundColor Gray
    Write-Host "   Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=1234" -ForegroundColor Gray
} else {
    Write-Host "   ❌ PostgreSQL 服务可能未运行" -ForegroundColor Red
    Write-Host "   请执行以下操作:" -ForegroundColor Gray
    Write-Host "   1. 启动 PostgreSQL 服务" -ForegroundColor Gray
    Write-Host "   2. 检查 PostgreSQL 是否已安装" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== 检查完成 ===" -ForegroundColor Cyan