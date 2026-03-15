# Build Frontend in temporary directory (no # in path)

$tempDir = "D:\temp_crm_web_build"
$sourceDir = "d:\MyProject\FrontCRM_CSharp\CRM.Web"
$currentDir = (Get-Location).Path

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "Frontend Build in Temp Directory" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

Write-Host "Creating temporary build directory..." -ForegroundColor Yellow
if (Test-Path $tempDir) {
    Write-Host "Removing existing temp directory..." -ForegroundColor Yellow
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

Write-Host "Copying frontend source to temp directory..." -ForegroundColor Yellow
Copy-Item -Path $sourceDir -Destination $tempDir -Recurse -Force

Write-Host "Navigating to temp directory..." -ForegroundColor Yellow
cd $tempDir

Write-Host "Running npm build..." -ForegroundColor Yellow
npm run build
$buildResult = $LASTEXITCODE

if ($buildResult -eq 0) {
    Write-Host ""
    Write-Host "Build succeeded!" -ForegroundColor Green
    Write-Host "Copying dist back to original location..." -ForegroundColor Yellow
    
    if (Test-Path "dist") {
        if (Test-Path "$sourceDir\dist") {
            Remove-Item -Path "$sourceDir\dist" -Recurse -Force
        }
        Copy-Item -Path "dist" -Destination "$sourceDir\dist" -Recurse -Force
        Write-Host "Success! Dist copied to $sourceDir\dist" -ForegroundColor Green
    } else {
        Write-Host "ERROR: dist folder not found after build!" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "Build failed (exit code: $buildResult)!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Cleaning up temporary directory..." -ForegroundColor Yellow
cd $currentDir
Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Frontend build process completed!" -ForegroundColor Green
