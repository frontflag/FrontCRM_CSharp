# 批量构建：后端 publish 一次 + 按 buildMode 分别构建前端，生成多套部署包
# 供 deploy-tenant-production.ps1 -Tenant all 使用
#
# 输出（在仓库根目录）：
#   frontcrm_deploy_semicore  （semicore、fz 共用）
#   frontcrm_deploy_idesemi
#   frontcrm_deploy_ecoinf
#
# 用法：
#   .\scripts\build-batch-tenants.ps1

param(
    [string[]]$BatchTenants = @('semicore', 'idesemi', 'ecoinf', 'fz')
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$tenantsFile = Join-Path $RepoRoot "deploy\tenants.json"
if (-not (Test-Path $tenantsFile)) {
    throw "tenants.json not found: $tenantsFile"
}

$tenantsCfg = Get-Content $tenantsFile -Raw | ConvertFrom-Json

function Get-BuildModeSuffix {
    param([string]$BuildMode)
    if ([string]::IsNullOrWhiteSpace($BuildMode)) { return 'default' }
    if ($BuildMode -match '^production\.(.+)$') { return $Matches[1] }
    return ($BuildMode -replace '[^\w\-]', '_')
}

$buildModes = [ordered]@{}
foreach ($tenantId in $BatchTenants) {
    $tenantCfg = $tenantsCfg.$tenantId
    if (-not $tenantCfg) {
        throw "Unknown tenant in tenants.json: $tenantId"
    }
    $buildMode = if ($tenantCfg.buildMode) { [string]$tenantCfg.buildMode } else { 'production' }
    $suffix = Get-BuildModeSuffix -BuildMode $buildMode
    if (-not $buildModes.Contains($buildMode)) {
        $buildModes[$buildMode] = $suffix
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Batch Build (all tenants)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Repo root: $RepoRoot" -ForegroundColor Gray
Write-Host "Frontend build modes: $($buildModes.Keys -join ', ')" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

$tempPath = "D:\temp_frontcrm_build"
if (Test-Path $tempPath) {
    Write-Host "Removing existing temp directory..." -ForegroundColor Yellow
    Remove-Item -Path $tempPath -Recurse -Force
}

Write-Host "Creating temporary build directory: $tempPath" -ForegroundColor Yellow
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null

Write-Host "Copying project to temporary directory..." -ForegroundColor Yellow
foreach ($item in Get-ChildItem -LiteralPath $RepoRoot -Force) {
    if ($item.Name -eq '.vs') { continue }
    if ($item.Name -eq 'data') { continue }
    if ($item.Name -like 'frontcrm_deploy*') { continue }
    Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $tempPath $item.Name) -Recurse -Force
}

Set-Location $tempPath

Write-Host ""
Write-Host "STEP 1: Building Backend (once)" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

Set-Location (Join-Path $tempPath "CRM.API")

Write-Host "Building in Release mode..." -ForegroundColor Yellow
& dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet build failed" -ForegroundColor Red
    Set-Location $RepoRoot
    exit 1
}

Write-Host "Publishing release build..." -ForegroundColor Yellow
& dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed" -ForegroundColor Red
    Set-Location $RepoRoot
    exit 1
}

$backendSize = (Get-ChildItem -Path "publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Backend published: $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Green

$distCacheRoot = Join-Path $tempPath "dist_cache"
New-Item -ItemType Directory -Path $distCacheRoot -Force | Out-Null

Write-Host ""
Write-Host "STEP 2: Building Frontend (per buildMode)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location (Join-Path $tempPath "CRM.Web")
$frontendSizes = @{}

$modeIndex = 0
foreach ($entry in $buildModes.GetEnumerator()) {
    $modeIndex++
    $buildMode = [string]$entry.Key
    $suffix = [string]$entry.Value

    Write-Host ">>> Frontend $modeIndex/$($buildModes.Count): mode=$buildMode -> frontcrm_deploy_$suffix" -ForegroundColor Magenta
    & npm run build -- --mode $buildMode
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: npm build failed (mode=$buildMode)" -ForegroundColor Red
        Write-Host "  Diagnose locally: cd CRM.Web; npm run build -- --mode $buildMode" -ForegroundColor Yellow
        Write-Host "  Temp copy kept for inspection: $tempPath\CRM.Web" -ForegroundColor Yellow
        Set-Location $RepoRoot
        exit 1
    }

    $cacheDir = Join-Path $distCacheRoot $suffix
    if (Test-Path $cacheDir) {
        Remove-Item -Path $cacheDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    Copy-Item -Path "dist\*" -Destination $cacheDir -Recurse -Force
    $frontendSizes[$suffix] = (Get-ChildItem -Path $cacheDir -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "  Cached dist: $([Math]::Round($frontendSizes[$suffix], 2)) MB" -ForegroundColor Gray
    Write-Host ""
}

Set-Location $tempPath

Write-Host ""
Write-Host "STEP 3: Creating deployment packages" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$createdPackages = New-Object System.Collections.Generic.List[string]

foreach ($entry in $buildModes.GetEnumerator()) {
    $suffix = [string]$entry.Value
    $packageName = "frontcrm_deploy_$suffix"
    $deployDir = Join-Path $tempPath $packageName
    $targetDir = Join-Path $RepoRoot $packageName

    if (Test-Path $deployDir) {
        Remove-Item -Path $deployDir -Recurse -Force
    }
    if (Test-Path $targetDir) {
        Remove-Item -Path $targetDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path "$deployDir/CRM.Web/dist" -Force | Out-Null
    New-Item -ItemType Directory -Path "$deployDir/CRM.API/publish" -Force | Out-Null

    Copy-Item -Path (Join-Path $distCacheRoot "$suffix\*") -Destination "$deployDir/CRM.Web/dist" -Recurse -Force
    Copy-Item -Path "CRM.API/publish/*" -Destination "$deployDir/CRM.API/publish" -Recurse -Force
    Copy-Item -Path "docker-compose.prebuilt.yml" -Destination "$deployDir/docker-compose.yml" -ErrorAction SilentlyContinue
    Copy-Item -Path "Dockerfile.backend.prebuilt" -Destination "$deployDir/" -ErrorAction SilentlyContinue
    Copy-Item -Path "CRM.Web/Dockerfile.prebuilt" -Destination "$deployDir/CRM.Web/" -ErrorAction SilentlyContinue
    Copy-Item -Path "CRM.Web/nginx.conf" -Destination "$deployDir/CRM.Web/" -ErrorAction SilentlyContinue
    Copy-Item -Path "deploy.env.example" -Destination "$deployDir/" -ErrorAction SilentlyContinue

    Copy-Item -Path $deployDir -Destination $RepoRoot -Recurse -Force
    $createdPackages.Add($packageName) | Out-Null
    Write-Host "Created: $targetDir" -ForegroundColor Green
}

Set-Location $RepoRoot

Write-Host "Cleaning up temporary directory..." -ForegroundColor Yellow
Set-Location "D:\"
Remove-Item -Path $tempPath -Recurse -Force
Set-Location $RepoRoot

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "BATCH BUILD SUMMARY" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "Backend (shared): $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Gray
foreach ($entry in $frontendSizes.GetEnumerator()) {
    Write-Host "Frontend ($($entry.Key)): $([Math]::Round($entry.Value, 2)) MB" -ForegroundColor Gray
}
Write-Host "Packages: $($createdPackages -join ', ')" -ForegroundColor Green
Write-Host "Build time: $([Math]::Round($duration.TotalSeconds, 1)) seconds" -ForegroundColor Yellow
Write-Host ""

exit 0
