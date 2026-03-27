# Build Frontend and Backend for FrontCRM
# This script uses a temporary directory to avoid issues with # character in path

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Build (Temp Path Fix)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$startTime = Get-Date

# Create temporary directory
$tempPath = "D:\temp_frontcrm_build"
if (Test-Path $tempPath) {
    Write-Host "Removing existing temp directory..." -ForegroundColor Yellow
    Remove-Item -Path $tempPath -Recurse -Force
}

Write-Host "Creating temporary build directory: $tempPath" -ForegroundColor Yellow
mkdir $tempPath -Force | Out-Null

# Copy project to temp path（复制根目录下各子项；避免 Copy-Item . 在目标下再套一层项目文件夹导致找不到 CRM.Web）
Write-Host "Copying project to temporary directory..." -ForegroundColor Yellow
Copy-Item -Path (Join-Path (Get-Location).Path '*') -Destination $tempPath -Recurse -Force

# Navigate to temp directory
cd $tempPath

# Step 1: Build Frontend
Write-Host ""
Write-Host "STEP 1: Building Frontend (Vue 3)" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

cd "CRM.Web"

Write-Host "Building production bundle..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: npm build failed" -ForegroundColor Red
    Write-Host "Cleaning up temp directory..." -ForegroundColor Yellow
    cd D:\
    Remove-Item -Path $tempPath -Recurse -Force
    exit 1
}

Write-Host ""
Write-Host "Frontend build completed successfully!" -ForegroundColor Green
$frontendSize = (Get-ChildItem -Path "dist" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Distribution size: $([Math]::Round($frontendSize, 2)) MB" -ForegroundColor Yellow

cd ..

# Step 2: Build Backend
Write-Host ""
Write-Host "STEP 2: Building Backend (ASP.NET Core)" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

cd "CRM.API"

Write-Host "Building in Release mode..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet build failed" -ForegroundColor Red
    cd D:\
    Remove-Item -Path $tempPath -Recurse -Force
    exit 1
}

Write-Host ""
Write-Host "Publishing release build..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed" -ForegroundColor Red
    cd D:\
    Remove-Item -Path $tempPath -Recurse -Force
    exit 1
}

Write-Host ""
Write-Host "Backend build completed successfully!" -ForegroundColor Green
$backendSize = (Get-ChildItem -Path "publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Published size: $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Yellow

cd ..

# Step 3: Create deployment package structure
Write-Host ""
Write-Host "STEP 3: Creating Deployment Package" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

$deployDir = "frontcrm_deploy"

if (Test-Path $deployDir) {
    Write-Host "Removing existing deployment directory..." -ForegroundColor Yellow
    Remove-Item -Path $deployDir -Recurse -Force
}

Write-Host "Creating deployment directory structure..." -ForegroundColor Yellow
mkdir -Force "$deployDir/CRM.Web/dist" | Out-Null
mkdir -Force "$deployDir/CRM.API/publish" | Out-Null

Write-Host "Copying frontend files..." -ForegroundColor Yellow
Copy-Item -Path "CRM.Web/dist/*" -Destination "$deployDir/CRM.Web/dist" -Recurse -ErrorAction SilentlyContinue

Write-Host "Copying backend files..." -ForegroundColor Yellow
Copy-Item -Path "CRM.API/publish/*" -Destination "$deployDir/CRM.API/publish" -Recurse -ErrorAction SilentlyContinue

Write-Host "Copying configuration files (prebuilt deploy: compose + Dockerfiles, no source in package)..." -ForegroundColor Yellow
Copy-Item -Path "docker-compose.prebuilt.yml" -Destination "$deployDir/docker-compose.yml" -ErrorAction SilentlyContinue
Copy-Item -Path "Dockerfile.backend.prebuilt" -Destination "$deployDir/" -ErrorAction SilentlyContinue
Copy-Item -Path "CRM.Web/Dockerfile.prebuilt" -Destination "$deployDir/CRM.Web/" -ErrorAction SilentlyContinue
Copy-Item -Path "CRM.Web/nginx.conf" -Destination "$deployDir/CRM.Web/" -ErrorAction SilentlyContinue
Copy-Item -Path "deploy.env.example" -Destination "$deployDir/" -ErrorAction SilentlyContinue

Write-Host "Deployment package created: $deployDir/" -ForegroundColor Green

# Copy deployment package back to original location
Write-Host ""
Write-Host "Copying deployment package back to original location..." -ForegroundColor Yellow
$originalPath = "D:\MyProject\FrontCRM_CSharp"
if (Test-Path "$originalPath\frontcrm_deploy") {
    Remove-Item -Path "$originalPath\frontcrm_deploy" -Recurse -Force
}
Copy-Item -Path "$deployDir" -Destination "$originalPath\" -Recurse

# Step 4: Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "BUILD SUMMARY" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "Frontend:" -ForegroundColor Yellow
Write-Host "  Size: $([Math]::Round($frontendSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

Write-Host "Backend:" -ForegroundColor Yellow
Write-Host "  Size: $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

Write-Host "Build Time: $([Math]::Round($duration.TotalSeconds, 1)) seconds" -ForegroundColor Yellow
Write-Host ""

Write-Host "Deployment Package:" -ForegroundColor Yellow
Write-Host "  Location: $originalPath\frontcrm_deploy" -ForegroundColor Green
Write-Host ""

# Cleanup temp directory
Write-Host "Cleaning up temporary directory..." -ForegroundColor Yellow
cd D:\
Remove-Item -Path $tempPath -Recurse -Force

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "NEXT STEPS:" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

Write-Host "1. Upload deployment package (e.g. deploy_to_server.ps1 -> /home/ubuntu/frontcrm):" -ForegroundColor Cyan
Write-Host ""

Write-Host "2. On server, rebuild and start (prebuilt 镜像使用包内 publish + dist):" -ForegroundColor Cyan
Write-Host "   cd /home/ubuntu/frontcrm" -ForegroundColor Gray
Write-Host "   docker compose build --no-cache" -ForegroundColor Gray
Write-Host "   docker compose up -d" -ForegroundColor Gray
Write-Host ""

Write-Host "3. Access the application:" -ForegroundColor Cyan
Write-Host "   Frontend: http://129.226.161.3" -ForegroundColor Gray
Write-Host "   Backend: http://129.226.161.3:5000" -ForegroundColor Gray
Write-Host ""

Write-Host "BUILD COMPLETE!" -ForegroundColor Green
Write-Host ""
