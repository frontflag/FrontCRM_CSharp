# Deploy FrontCRM to Hong Kong Server
#
# 将本地 frontcrm_deploy 目录「内容」直接同步到服务器：/home/ubuntu/frontcrm/
# （不是上传到 frontcrm_deploy 子目录；本地仍由 build_with_temp_path.ps1 生成 frontcrm_deploy）
#
# Configuration
$serverIP = "129.226.161.3"
$serverUser = "ubuntu"
$deployPackage = "frontcrm_deploy"
$remoteDeployPath = "/home/ubuntu/frontcrm"

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

if (-not (Test-Path "$deployPackage/CRM.Web/dist/index.html")) {
    Write-Host "ERROR: '$deployPackage/CRM.Web/dist/index.html' missing — package has no frontend build." -ForegroundColor Red
    Write-Host "Run build_with_temp_path.ps1 first (it runs npm run build and fills CRM.Web/dist)." -ForegroundColor Yellow
    exit 1
}

Write-Host "Server Configuration:" -ForegroundColor Cyan
Write-Host "  IP Address: $serverIP" -ForegroundColor Gray
Write-Host "  User: $serverUser" -ForegroundColor Gray
Write-Host "  Remote directory (contents of local $deployPackage go here): $remoteDeployPath/" -ForegroundColor Gray
Write-Host ""

# Get deployment package size
$packageSize = (Get-ChildItem -Path $deployPackage -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Deployment Package Size: $([Math]::Round($packageSize, 2)) MB" -ForegroundColor Yellow
Write-Host ""

Write-Host "Steps to deploy:" -ForegroundColor Yellow
Write-Host "1. Ensure remote directory exists (ssh mkdir -p)" -ForegroundColor Gray
Write-Host "2. Upload via scp (folder contents -> $remoteDeployPath)" -ForegroundColor Gray
Write-Host "3. On server: rebuild all images (prebuilt Dockerfiles) and up -d (see below)" -ForegroundColor Gray
Write-Host ""

$scpPath = Get-Command scp -ErrorAction SilentlyContinue
if (-not $scpPath) {
    Write-Host "ERROR: scp command not found!" -ForegroundColor Red
    Write-Host "Please install OpenSSH or Git Bash to use this script." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative: Use a GUI SCP tool like WinSCP or FileZilla" -ForegroundColor Cyan
    exit 1
}

$sshPath = Get-Command ssh -ErrorAction SilentlyContinue
if (-not $sshPath) {
    Write-Host "ERROR: ssh command not found (needed to mkdir on server before scp)." -ForegroundColor Red
    exit 1
}

$localResolved = (Resolve-Path $deployPackage).Path
$localDot = Join-Path $localResolved "."

Write-Host "Creating remote directory if missing..." -ForegroundColor Yellow
ssh -p 22 "${serverUser}@${serverIP}" "mkdir -p '$remoteDeployPath'"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ssh mkdir failed. Check keys and access." -ForegroundColor Red
    exit 1
}

Write-Host "Uploading (recursive: local $deployPackage/* -> ${remoteDeployPath}/)..." -ForegroundColor Yellow
Write-Host ""

# Copy contents of frontcrm_deploy into /home/ubuntu/frontcrm/ (not a nested frontcrm_deploy folder)
scp -r -P 22 "$localDot" "${serverUser}@${serverIP}:${remoteDeployPath}/"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Upload successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps on the server:" -ForegroundColor Cyan
    Write-Host "1. SSH:" -ForegroundColor Gray
    Write-Host "   ssh ${serverUser}@${serverIP}" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Project root (docker-compose.yml is here):" -ForegroundColor Gray
    Write-Host "   cd $remoteDeployPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   首次或更换数据库：复制 deploy.env.example 为 .env，填写腾讯云 CONNECTION_STRING" -ForegroundColor DarkYellow
    Write-Host "   （compose 已不含 Postgres 容器，API 只连外部库）" -ForegroundColor DarkYellow
    Write-Host ""
    Write-Host "3. Rebuild images (包内无源码，须用 Dockerfile.*.prebuilt) 并启动:" -ForegroundColor Gray
    Write-Host "   docker compose build --no-cache" -ForegroundColor Gray
    Write-Host "   docker compose up -d" -ForegroundColor Gray
    Write-Host "   (docker-compose v1: docker-compose build --no-cache frontend && docker-compose up -d)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "4. Verify:" -ForegroundColor Gray
    Write-Host "   docker compose ps" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. URLs:" -ForegroundColor Gray
    Write-Host "   Frontend: http://${serverIP}" -ForegroundColor Gray
    Write-Host "   Backend API: http://${serverIP}:5000/api/v1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. Logs:" -ForegroundColor Gray
    Write-Host "   docker compose logs -f" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "ERROR: Upload failed!" -ForegroundColor Red
    Write-Host "Please check your connection and credentials." -ForegroundColor Yellow
    exit 1
}

Write-Host "Deployment script completed!" -ForegroundColor Green
