# Deploy FrontCRM — 前端 + 后端 一并构建并上传到服务器
# 本文件须保存为「UTF-8 带 BOM」；否则 Windows PowerShell 5.1 会按 ANSI 误读中文与引号，出现 UnexpectedToken `}`。
#
# 流程：
#   1) 调用 build_with_temp_path.ps1：npm build + dotnet publish，生成 frontcrm_deploy（含 CRM.Web/dist、CRM.API/publish、docker-compose 等）
#   2) 将 frontcrm_deploy 目录「内容」同步到服务器（与 deploy_to_server.ps1 相同）
#
# 若已本地构建好 frontcrm_deploy，可传 -SkipBuild 只上传。
#
# 用法（在项目根目录）：
#   .\deploy_full_to_server.ps1
#   .\deploy_full_to_server.ps1 -SkipBuild
#   .\deploy_full_to_server.ps1 -ServerIP "x.x.x.x" -ServerUser "ubuntu"

param(
    [switch]$SkipBuild,
    [string]$ServerIP = "129.226.161.3",
    [int]$SshPort = 22,
    [string]$ServerUser = "ubuntu",
    [string]$DeployPackageName = "frontcrm_deploy",
    [string]$RemoteDeployPath = "/home/ubuntu/frontcrm",
    [ValidateSet("auto", "docker", "nonDocker")]
    [string]$DeploymentMode = "auto",
    # Non-Docker 默认目录（与你们 DEPLOY_OPERATION_MANUAL.md 一致）
    [string]$NonDockerFrontendRoot = "/opt/frontcrm/frontend",
    [string]$NonDockerBackendRoot = "/opt/frontcrm/backend",
    [int]$BackendPort = 5000
)

$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Get-Location).Path
}

$deployPackage = Join-Path $RepoRoot $DeployPackageName

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM full deploy (frontend + backend)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Repo root: $RepoRoot" -ForegroundColor Gray
Write-Host ""

# SkipBuild 分支拆成独立 if，避免 PS 5.1 将注释内的花括号误当作代码解析。
if (-not $SkipBuild) {
    $buildScript = Join-Path $RepoRoot "build_with_temp_path.ps1"
    if (-not (Test-Path $buildScript)) {
        Write-Host "ERROR: build_with_temp_path.ps1 not found: $buildScript" -ForegroundColor Red
        exit 1
    }

    Write-Host ">>> Step 1/2: local build (dist + publish + package)" -ForegroundColor Cyan
    Write-Host ""

    Set-Location $RepoRoot
    & $buildScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: build_with_temp_path.ps1 failed (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Set-Location $RepoRoot
    Write-Host ""
}

if ($SkipBuild) {
    Write-Host ">>> Build skipped (-SkipBuild), using existing $DeployPackageName" -ForegroundColor Yellow
    Write-Host ""
}

# 与 deploy_to_server.ps1 一致的校验
if (-not (Test-Path $deployPackage)) {
    Write-Host "ERROR: deployment package folder not found: $deployPackage" -ForegroundColor Red
    if (-not $SkipBuild) {
        Write-Host "build_with_temp_path.ps1 should have created this folder, check build logs." -ForegroundColor Yellow
    } else {
        Write-Host "Run build_with_temp_path.ps1 first, or remove -SkipBuild." -ForegroundColor Yellow
    }
    exit 1
}

if (-not (Test-Path (Join-Path $deployPackage "CRM.Web/dist/index.html"))) {
    Write-Host "ERROR: frontend build missing: $DeployPackageName/CRM.Web/dist/index.html" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path (Join-Path $deployPackage "CRM.API/publish/CRM.API.dll"))) {
    Write-Host "ERROR: backend publish missing: $DeployPackageName/CRM.API/publish/CRM.API.dll" -ForegroundColor Red
    exit 1
}

Write-Host ">>> Step 2/2: upload to ${ServerUser}@${ServerIP}:${RemoteDeployPath}/" -ForegroundColor Cyan
Write-Host ""

$packageSize = (Get-ChildItem -Path $deployPackage -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "Package size: $([Math]::Round($packageSize, 2)) MB" -ForegroundColor Yellow
Write-Host ""

$scpPath = Get-Command scp -ErrorAction SilentlyContinue
if (-not $scpPath) {
    Write-Host "ERROR: scp not found, install OpenSSH client." -ForegroundColor Red
    exit 1
}

$sshPath = Get-Command ssh -ErrorAction SilentlyContinue
if (-not $sshPath) {
    Write-Host "ERROR: ssh not found." -ForegroundColor Red
    exit 1
}

$localResolved = (Resolve-Path $deployPackage).Path
$uploadSource = $localResolved
$stagingDir = Join-Path $env:TEMP ("frontcrm_upload_" + [Guid]::NewGuid().ToString("N"))

Write-Host "Preparing upload files (exclude .md)..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# 使用 robocopy 保留目录结构并排除 .md
& robocopy $localResolved $stagingDir /E /NFL /NDL /NJH /NJS /NP /XF *.md | Out-Null
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -gt 7) {
    Write-Host "ERROR: failed to prepare upload files (robocopy exit $robocopyExitCode)" -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

$localDot = Join-Path $stagingDir "."

Write-Host "Creating remote directory..." -ForegroundColor Yellow
& ssh -p $SshPort "${ServerUser}@${ServerIP}" "mkdir -p '$RemoteDeployPath'"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ssh mkdir failed, check key and network." -ForegroundColor Red
    exit 1
}

Write-Host "Uploading ($DeployPackageName/* -> ${RemoteDeployPath}/)..." -ForegroundColor Yellow
& scp -r -P $SshPort "$localDot" "${ServerUser}@${ServerIP}:${RemoteDeployPath}/"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: scp upload failed." -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Upload completed." -ForegroundColor Green
Write-Host ""

# Decide deployment mode on server
$useDocker = $false
if ($DeploymentMode -eq "docker") {
    $useDocker = $true
}
elseif ($DeploymentMode -eq "nonDocker") {
    $useDocker = $false
}
else {
    # auto: if docker compose ps succeeds, treat as docker mode
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "command -v docker >/dev/null 2>&1 && docker compose ps >/dev/null 2>&1"
    if ($LASTEXITCODE -eq 0) { $useDocker = $true }
}

if ($useDocker) {
    Write-Host "Docker deployment mode detected: rebuilding and starting compose." -ForegroundColor Cyan
    Write-Host ""
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "cd '$RemoteDeployPath'; docker compose build --no-cache" | Out-Null
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "cd '$RemoteDeployPath'; docker compose up -d" | Out-Null
}
else {
    Write-Host "Non-Docker mode detected: applying Nginx + dotnet flow." -ForegroundColor Cyan
    Write-Host ""

    # 1) 准备目录 + 覆盖前端 dist
    $srcFront = "${RemoteDeployPath}/CRM.Web/dist"
    $srcBack = "${RemoteDeployPath}/CRM.API/publish"

    Write-Host ">>> Non-Docker: prepare directories..." -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "sudo mkdir -p $NonDockerFrontendRoot $NonDockerBackendRoot && sudo chown -R ${ServerUser}:${ServerUser} $NonDockerFrontendRoot $NonDockerBackendRoot" | Out-Null

    Write-Host ">>> Non-Docker: sync frontend dist ($srcFront -> $NonDockerFrontendRoot)" -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "sudo rm -rf $NonDockerFrontendRoot/*; sudo cp -r $srcFront/* $NonDockerFrontendRoot/; sudo chown -R ${ServerUser}:${ServerUser} $NonDockerFrontendRoot; sudo find $NonDockerFrontendRoot -type d -exec chmod 755 {} \;; sudo find $NonDockerFrontendRoot -type f -exec chmod 644 {} \;" | Out-Null

    # 2) 覆盖后端 publish
    Write-Host ">>> Non-Docker: sync backend publish ($srcBack -> $NonDockerBackendRoot)" -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "sudo rm -rf $NonDockerBackendRoot/*; sudo cp -r $srcBack/* $NonDockerBackendRoot/; sudo chown -R ${ServerUser}:${ServerUser} $NonDockerBackendRoot; sudo find $NonDockerBackendRoot -type d -exec chmod 755 {} \;; sudo find $NonDockerBackendRoot -type f -exec chmod 644 {} \;" | Out-Null

    # 3) reload nginx（如果 nginx 存在）
    Write-Host ">>> Non-Docker: nginx reload (if nginx exists)..." -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "if command -v nginx >/dev/null 2>&1; then sudo nginx -t && sudo systemctl reload nginx; fi" | Out-Null

    # 4) restart api service via systemd (fallback to nohup if service missing)
    Write-Host ">>> Non-Docker: restart crm-api service..." -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "if sudo systemctl list-unit-files | grep -q '^crm-api\.service'; then sudo systemctl daemon-reload; sudo systemctl restart crm-api; else cd $NonDockerBackendRoot; pkill -f 'CRM.API.dll' 2>/dev/null || true; export ASPNETCORE_ENVIRONMENT=Production; export ASPNETCORE_URLS=http://0.0.0.0:$BackendPort; nohup dotnet CRM.API.dll > api.log 2>&1 & sleep 2; fi" | Out-Null

    Write-Host ">>> Non-Docker: crm-api status (is-active)" -ForegroundColor Gray
    & ssh -p $SshPort "${ServerUser}@${ServerIP}" "if sudo systemctl list-unit-files | grep -q '^crm-api\.service'; then sudo systemctl is-active crm-api; else pgrep -f 'dotnet CRM.API.dll' >/dev/null && echo active || echo inactive; fi"
}

Write-Host ""
Write-Host "Frontend: http://${ServerIP}" -ForegroundColor Gray
Write-Host "API:      http://${ServerIP}:$BackendPort/api/v1" -ForegroundColor Gray
Write-Host ""
Write-Host "deploy_full_to_server.ps1 done." -ForegroundColor Green
