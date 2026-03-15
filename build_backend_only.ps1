# Build Backend Only (ASP.NET Core)

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Backend Build" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

$startTime = Get-Date

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
$backendSize = (Get-ChildItem -Path "publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Published size: $([Math]::Round($backendSize, 2)) MB" -ForegroundColor Yellow

cd ..

$endTime = Get-Date
$duration = $endTime - $startTime

Write-Host ""
Write-Host "Build Time: $([Math]::Round($duration.TotalSeconds, 1)) seconds" -ForegroundColor Yellow
Write-Host "Location: $((Get-Location).Path)\CRM.API\publish" -ForegroundColor Green
Write-Host ""
Write-Host "BUILD COMPLETE!" -ForegroundColor Green
