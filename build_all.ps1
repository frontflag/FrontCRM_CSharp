# Build Frontend and Backend for FrontCRM

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Local Build and Deployment Package" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

# Get start time
$startTime = Get-Date

# Step 1: Build Frontend
Write-Host "STEP 1: Building Frontend (Vue 3)" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

cd "CRM.Web"

Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: npm install failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Building production bundle..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: npm build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Frontend build completed successfully!" -ForegroundColor Green
$frontendSize = (Get-ChildItem -Path "dist" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
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
    exit 1
}

Write-Host ""
Write-Host "Publishing release build..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Backend build completed successfully!" -ForegroundColor Green
$backendSize = (Get-ChildItem -Path "publish" -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
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
Copy-Item -Path "CRM.Web/dist/*" -Destination "$deployDir/CRM.Web/dist" -Recurse

Write-Host "Copying backend files..." -ForegroundColor Yellow
Copy-Item -Path "CRM.API/publish/*" -Destination "$deployDir/CRM.API/publish" -Recurse

Write-Host "Copying configuration files..." -ForegroundColor Yellow
Copy-Item -Path "docker-compose.yml" -Destination "$deployDir/"
Copy-Item -Path "Dockerfile.backend" -Destination "$deployDir/"
Copy-Item -Path "CRM.Web/Dockerfile" -Destination "$deployDir/CRM.Web/"
Copy-Item -Path "CRM.Web/nginx.conf" -Destination "$deployDir/CRM.Web/" -ErrorAction SilentlyContinue

Write-Host "Deployment package created: $deployDir/" -ForegroundColor Green

# Step 4: Summary
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "BUILD SUMMARY" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host "Frontend:" -ForegroundColor Yellow
Write-Host "  Location: $((Get-Location).Path)\CRM.Web\dist" -ForegroundColor Gray
Write-Host "  Size: $([Math]::Round($frontendSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

Write-Host "Backend:" -ForegroundColor Yellow
Write-Host "  Location: $((Get-Location).Path)\CRM.API\publish" -ForegroundColor Gray
Write-Host "  Size: $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

Write-Host "Deployment Package:" -ForegroundColor Yellow
Write-Host "  Location: $((Get-Location).Path)\$deployDir" -ForegroundColor Gray
Write-Host "  Contents:" -ForegroundColor Gray
Write-Host "    - CRM.Web/dist/" -ForegroundColor Gray
Write-Host "    - CRM.Web/Dockerfile" -ForegroundColor Gray
Write-Host "    - CRM.Web/nginx.conf" -ForegroundColor Gray
Write-Host "    - CRM.API/publish/" -ForegroundColor Gray
Write-Host "    - Dockerfile.backend" -ForegroundColor Gray
Write-Host "    - docker-compose.yml" -ForegroundColor Gray
Write-Host ""

Write-Host "Build Time: $([Math]::Round($duration.TotalSeconds, 1)) seconds" -ForegroundColor Yellow
Write-Host ""

Write-Host "============================================" -ForegroundColor Green
Write-Host "NEXT STEPS:" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "1. Upload deployment package to server:" -ForegroundColor Cyan
Write-Host "   scp -r frontcrm_deploy ubuntu@129.226.161.3:/home/ubuntu/" -ForegroundColor Gray
Write-Host ""
Write-Host "2. On server, start services:" -ForegroundColor Cyan
Write-Host "   cd /home/ubuntu/frontcrm_deploy" -ForegroundColor Gray
Write-Host "   docker-compose up -d" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Access the application:" -ForegroundColor Cyan
Write-Host "   Frontend: http://129.226.161.3" -ForegroundColor Gray
Write-Host "   Backend: http://129.226.161.3:5000" -ForegroundColor Gray
Write-Host ""

Write-Host "BUILD COMPLETE!" -ForegroundColor Green
Write-Host ""