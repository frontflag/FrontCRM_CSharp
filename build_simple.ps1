# Simple Build Script - Frontend and Backend

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Build Script" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$startTime = Get-Date

# Step 1: Build Frontend
Write-Host "STEP 1: Building Frontend (Vue 3)" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

cd "CRM.Web"

Write-Host "Running: npm run build..." -ForegroundColor Yellow
npm run build 2>&1 > ..\frontend_build.log
$frontendExitCode = $LASTEXITCODE

if ($frontendExitCode -eq 0) {
    Write-Host "Frontend build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Frontend build failed (exit code: $frontendExitCode)" -ForegroundColor Yellow
    Write-Host "Check frontend_build.log for details" -ForegroundColor Yellow
}

cd ..

# Step 2: Build Backend
Write-Host ""
Write-Host "STEP 2: Building Backend (ASP.NET Core)" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

cd "CRM.API"

Write-Host "Running: dotnet publish -c Release..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish 2>&1 > ..\backend_build.log
$backendExitCode = $LASTEXITCODE

if ($backendExitCode -eq 0) {
    Write-Host "Backend build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Backend build failed (exit code: $backendExitCode)" -ForegroundColor Yellow
    Write-Host "Check backend_build.log for details" -ForegroundColor Yellow
}

cd ..

# Step 3: Create deployment package
Write-Host ""
Write-Host "STEP 3: Creating Deployment Package" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

$deployDir = "frontcrm_deploy"

if (Test-Path $deployDir) {
    Write-Host "Removing existing deployment directory..." -ForegroundColor Yellow
    Remove-Item -Path $deployDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Creating deployment structure..." -ForegroundColor Yellow
@('CRM.Web/dist', 'CRM.API/publish') | ForEach-Object {
    $dir = "$deployDir/$_"
    if (-not (Test-Path $dir)) {
        mkdir -Force $dir | Out-Null
    }
}

Write-Host "Copying frontend distribution..." -ForegroundColor Yellow
if (Test-Path "CRM.Web/dist") {
    Copy-Item -Path "CRM.Web/dist/*" -Destination "$deployDir/CRM.Web/dist" -Recurse -Force
    Write-Host "  ✓ Frontend files copied" -ForegroundColor Gray
} else {
    Write-Host "  ⚠ Frontend dist not found" -ForegroundColor Yellow
}

Write-Host "Copying backend binaries..." -ForegroundColor Yellow
if (Test-Path "CRM.API/publish") {
    Copy-Item -Path "CRM.API/publish/*" -Destination "$deployDir/CRM.API/publish" -Recurse -Force
    Write-Host "  ✓ Backend files copied" -ForegroundColor Gray
} else {
    Write-Host "  ⚠ Backend publish not found" -ForegroundColor Yellow
}

Write-Host "Copying configuration files..." -ForegroundColor Yellow
@('docker-compose.yml', 'Dockerfile.backend') | ForEach-Object {
    if (Test-Path $_) {
        Copy-Item -Path $_ -Destination "$deployDir/" -Force
        Write-Host "  ✓ $_" -ForegroundColor Gray
    }
}

if (Test-Path "CRM.Web/Dockerfile") {
    Copy-Item -Path "CRM.Web/Dockerfile" -Destination "$deployDir/CRM.Web/" -Force
    Write-Host "  ✓ CRM.Web/Dockerfile" -ForegroundColor Gray
}

if (Test-Path "CRM.Web/nginx.conf") {
    Copy-Item -Path "CRM.Web/nginx.conf" -Destination "$deployDir/CRM.Web/" -Force
    Write-Host "  ✓ CRM.Web/nginx.conf" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✓ Deployment package created: $deployDir/" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "BUILD SUMMARY" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "Build Time: $([Math]::Round($duration.TotalSeconds, 1)) seconds" -ForegroundColor Yellow
Write-Host ""

Write-Host "Frontend Status:" -ForegroundColor Cyan
if ($frontendExitCode -eq 0) {
    Write-Host "  ✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "  ✗ Build failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "Backend Status:" -ForegroundColor Cyan
if ($backendExitCode -eq 0) {
    Write-Host "  ✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "  ✗ Build failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "Deployment Package:" -ForegroundColor Cyan
if (Test-Path $deployDir) {
    $packageSize = (Get-ChildItem -Path $deployDir -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "  Location: $((Get-Location).Path)\$deployDir" -ForegroundColor Gray
    Write-Host "  Size: $([Math]::Round($packageSize, 2)) MB" -ForegroundColor Gray
} else {
    Write-Host "  Package not created" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "NEXT STEP:" -ForegroundColor Green
Write-Host "Run: .\deploy_to_server.ps1" -ForegroundColor Yellow
Write-Host ""
