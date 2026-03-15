# PostgreSQL 连接和数据库创建脚本

Write-Host "=== PostgreSQL 数据库创建 ===" -ForegroundColor Cyan
Write-Host ""

# 检查 PostgreSQL 服务状态
Write-Host "1. 检查 PostgreSQL 服务状态..." -ForegroundColor Yellow
$service = Get-Service -Name "postgresql-x64-16" -ErrorAction SilentlyContinue

if ($service -and $service.Status -eq "Running") {
    Write-Host "   ✅ PostgreSQL 服务正在运行" -ForegroundColor Green
} else {
    Write-Host "   ❌ PostgreSQL 服务未运行" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 尝试使用不同的密码连接
Write-Host "2. 尝试连接 PostgreSQL..." -ForegroundColor Yellow

$passwords = @("postgres", "postgres123", "123456", "password", "1234", "")

$success = $false
$correctPassword = $null

foreach ($pwd in $passwords) {
    Write-Host "   尝试密码: '$pwd'..." -NoNewline
    
    # 使用 pg_ctl 测试连接（更可靠的方法）
    $env:PGPASSWORD = $pwd
    $result = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d postgres -c "SELECT 1;" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✅ 成功!" -ForegroundColor Green
        $success = $true
        $correctPassword = $pwd
        break
    } else {
        Write-Host " ❌ 失败" -ForegroundColor Red
    }
}

if (-not $success) {
    Write-Host "   ❌ 所有密码尝试都失败了" -ForegroundColor Red
    Write-Host "   请手动检查 PostgreSQL 密码:" -ForegroundColor Yellow
    Write-Host "   1. 打开 pgAdmin" -ForegroundColor Gray
    Write-Host "   2. 检查服务器属性" -ForegroundColor Gray
    Write-Host "   3. 或者使用空密码尝试" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "   找到正确密码: '$correctPassword'" -ForegroundColor Green

Write-Host ""

# 3. 检查 FrontCRM 数据库是否存在
Write-Host "3. 检查 FrontCRM 数据库..." -ForegroundColor Yellow

$env:PGPASSWORD = $correctPassword
$checkDb = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d postgres -c "SELECT 1 FROM pg_database WHERE datname = 'FrontCRM';" 2>&1

if ($LASTEXITCODE -eq 0) {
    # 检查是否返回了结果
    if ($checkDb -match "1 row") {
        Write-Host "   ✅ FrontCRM 数据库已存在" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  FrontCRM 数据库不存在，将创建..." -ForegroundColor Yellow
        $createDb = $true
    }
} else {
    Write-Host "   ⚠️  无法检查数据库，将尝试创建..." -ForegroundColor Yellow
    $createDb = $true
}

Write-Host ""

# 4. 创建数据库（如果需要）
if ($createDb) {
    Write-Host "4. 创建 FrontCRM 数据库..." -ForegroundColor Yellow
    
    $createCmd = @"
CREATE DATABASE "FrontCRM"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Chinese (Simplified)_China.936'
    LC_CTYPE = 'Chinese (Simplified)_China.936'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
"@
    
    $result = & "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d postgres -c $createCmd 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ FrontCRM 数据库创建成功!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ 数据库创建失败:" -ForegroundColor Red
        Write-Host "   $result" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# 5. 更新项目连接字符串
Write-Host "5. 更新项目连接字符串..." -ForegroundColor Yellow

# 构建正确的连接字符串
$connectionString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=$correctPassword"

Write-Host "   正确的连接字符串:" -ForegroundColor Gray
Write-Host "   $connectionString" -ForegroundColor White

# 更新 appsettings.json
$appSettingsPath = "CRM.API\appsettings.json"
if (Test-Path $appSettingsPath) {
    Write-Host "   找到 appsettings.json，将更新连接字符串..." -ForegroundColor Gray
    
    $content = Get-Content $appSettingsPath -Raw
    $newContent = $content -replace '"DefaultConnection": "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=.*"', "`"DefaultConnection`": `"$connectionString`""
    
    Set-Content -Path $appSettingsPath -Value $newContent -Encoding UTF8
    Write-Host "   ✅ appsettings.json 已更新" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  未找到 appsettings.json" -ForegroundColor Yellow
}

# 更新 docker-compose.yml 中的连接字符串
$dockerComposePath = "frontcrm_deploy\docker-compose.yml"
if (Test-Path $dockerComposePath) {
    Write-Host "   更新 docker-compose.yml..." -ForegroundColor Gray
    
    $content = Get-Content $dockerComposePath -Raw
    $newContent = $content -replace 'ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=FrontCRM;Username=postgres;Password=.*', "ConnectionStrings__DefaultConnection: $connectionString"
    
    Set-Content -Path $dockerComposePath -Value $newContent -Encoding UTF8
    Write-Host "   ✅ docker-compose.yml 已更新" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== 操作完成 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "总结:" -ForegroundColor Yellow
Write-Host "1. PostgreSQL 密码: '$correctPassword'" -ForegroundColor Gray
Write-Host "2. FrontCRM 数据库: " -NoNewline -ForegroundColor Gray
if ($createDb) {
    Write-Host "已创建" -ForegroundColor Green
} else {
    Write-Host "已存在" -ForegroundColor Green
}
Write-Host "3. 连接字符串已更新" -ForegroundColor Gray
Write-Host ""
Write-Host "现在可以测试连接了!" -ForegroundColor Green