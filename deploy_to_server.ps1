# Deploy FrontCRM to Hong Kong Server
# Configuration
$serverIP = "129.226.161.3"
$serverUser = "ubuntu"
$deployPackage = "frontcrm_deploy"
$remoteDeployPath = "/home/ubuntu"

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM Deployment Script" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

# Check if deployment package exists
if (-not (Test-Path $deployPackage)) {
    Write-Host "ERROR: Deployment package '$deployPackage' not found!" -ForegroundColor Red
    Write-Host "Please run build_with_temp_path.ps1 first to generate the package." -ForegroundColor Yellow
    exit 1
}

Write-Host "Server Configuration:" -ForegroundColor Cyan
Write-Host "  IP Address: $serverIP" -ForegroundColor Gray
Write-Host "  User: $serverUser" -ForegroundColor Gray
Write-Host "  Remote Path: $remoteDeployPath" -ForegroundColor Gray
Write-Host ""

# Get deployment package size
$packageSize = (Get-ChildItem -Path $deployPackage -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Deployment Package Size: $([Math]::Round($packageSize, 2)) MB" -ForegroundColor Yellow
Write-Host ""

Write-Host "Steps to deploy:" -ForegroundColor Yellow
Write-Host "1. Upload deployment package via scp" -ForegroundColor Gray
Write-Host "2. Extract and setup on server" -ForegroundColor Gray
Write-Host "3. Start Docker containers" -ForegroundColor Gray
Write-Host ""

# Check if scp is available
$scpPath = Get-Command scp -ErrorAction SilentlyContinue
if (-not $scpPath) {
    Write-Host "ERROR: scp command not found!" -ForegroundColor Red
    Write-Host "Please install OpenSSH or Git Bash to use this script." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: Use a GUI SCP tool like WinSCP or FileZilla" -ForegroundColor Cyan
    exit 1
}

Write-Host "Uploading deployment package to server..." -ForegroundColor Yellow
Write-Host ""

# Upload using scp
scp -r -P 22 "$deployPackage" "${serverUser}@${serverIP}:${remoteDeployPath}/"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Upload successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps to complete on the server:" -ForegroundColor Cyan
    Write-Host "1. SSH to the server:" -ForegroundColor Gray
    Write-Host "   ssh ${serverUser}@${serverIP}" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Navigate to deployment directory:" -ForegroundColor Gray
    Write-Host "   cd ${remoteDeployPath}/${deployPackage}" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Start Docker containers:" -ForegroundColor Gray
    Write-Host "   docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Verify deployment:" -ForegroundColor Gray
    Write-Host "   docker-compose ps" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. Access the application:" -ForegroundColor Gray
    Write-Host "   Frontend: http://${serverIP}" -ForegroundColor Gray
    Write-Host "   Backend API: http://${serverIP}:5000/api/v1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. View logs:" -ForegroundColor Gray
    Write-Host "   docker-compose logs -f" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "ERROR: Upload failed!" -ForegroundColor Red
    Write-Host "Please check your connection and credentials." -ForegroundColor Yellow
    exit 1
}

Write-Host "Deployment script completed!" -ForegroundColor Green
