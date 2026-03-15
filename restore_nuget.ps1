# Restore NuGet packages for FrontCRM

Write-Host "=== NuGet Package Restore ===" -ForegroundColor Green
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host ".NET version: $dotnetVersion" -ForegroundColor Green

# Go to CRM.API directory
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
Write-Host ""

cd "CRM.API"

# Method 1: Using Tencent Cloud mirror
Write-Host "Method 1: Using Tencent Cloud mirror..." -ForegroundColor Cyan
dotnet restore --source https://mirrors.cloud.tencent.com/nuget/ --no-cache

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS! NuGet packages restored." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next step: dotnet build" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "Method 1 failed. Trying method 2..." -ForegroundColor Red
    Write-Host ""
    
    # Method 2: Try without specific source
    Write-Host "Method 2: Using default sources..." -ForegroundColor Cyan
    dotnet restore --no-cache
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "SUCCESS! NuGet packages restored." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "FAILED: Could not restore NuGet packages." -ForegroundColor Red
        Write-Host ""
        Write-Host "Possible solutions:" -ForegroundColor Yellow
        Write-Host "1. Check internet connection"
        Write-Host "2. Try using a proxy"
        Write-Host "3. Download packages manually from nuget.org"
        Write-Host ""
    }
}

cd ..
Write-Host "Done." -ForegroundColor Green