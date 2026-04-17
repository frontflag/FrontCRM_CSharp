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
#   .\deploy_full_to_server.ps1 -AllowPasswordPrompt   # 使用密码登录（否则会 BatchMode，卡住/失败）
#   .\deploy_full_to_server.ps1 -RequestTtyForSudo    # 非 Docker 部署时远程 sudo 要手输密码（分配伪终端，勿与静默管道一起用）
#   .\deploy_full_to_server.ps1 -SshKeyPath "$env:USERPROFILE\.ssh\id_ed25519"
#   .\deploy_full_to_server.ps1 -ApiHealthWaitSeconds 120   # 非 Docker：systemctl start 后等健康检查的最长秒数（默认 90）

param(
    [switch]$SkipBuild,
    # 默认非交互：若未配置密钥而只用密码，SSH 会一直卡住；此时请改用 -AllowPasswordPrompt
    [switch]$AllowPasswordPrompt,
    # 远程 sudo 无 NOPASSWD 且无 TTY 时会一直等密码（看起来像卡住）。默认用 sudo -n 秒失败；需交互输密码时加本开关（会 ssh -t）
    [switch]$RequestTtyForSudo,
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
    [int]$BackendPort = 5000,
    # 冷启动 + 连库 + Kestrel 绑定略慢于 systemctl start 返回；原先 40s 且强依赖 is-active=active 易误报失败
    [int]$ApiHealthWaitSeconds = 90,
    # Optional: ssh/scp -i (PowerShell 5.1: save this script as UTF-8 with BOM if you use non-ASCII in messages)
    [string]$SshKeyPath = ""
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
        Write-Host ('ERROR: build_with_temp_path.ps1 failed (exit {0})' -f $LASTEXITCODE) -ForegroundColor Red
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

# PS 5.1：双引号内勿写 ${User}@${Host}（会误解析）；与路径用冒号拼接时用 $($x):$($y)
$SshTarget = "$ServerUser@$ServerIP"

Write-Host ">>> Step 2/2: upload to $($SshTarget):$($RemoteDeployPath)/" -ForegroundColor Cyan
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

# 避免「Creating remote directory...」后长时间无输出：TCP 超时 + 禁止静默等密码（BatchMode）
# 无密钥时 BatchMode 会失败；若未加 -AllowPasswordPrompt 却长时间卡住，多为在尝试键盘交互/GSSAPI，故 BatchMode 下限定公钥并快速失败。
$SshOpts = @(
    "-o", "ConnectTimeout=30",
    "-o", "ServerAliveInterval=10",
    "-o", "ServerAliveCountMax=3",
    "-o", "StrictHostKeyChecking=no"
)
if (-not $AllowPasswordPrompt) {
    $SshOpts += @("-o", "BatchMode=yes")
    $SshOpts += @("-o", "NumberOfPasswordPrompts=0")
    $SshOpts += @("-o", "PreferredAuthentications=publickey")
    $SshOpts += @("-o", "PubkeyAuthentication=yes")
}
$ScpOpts = @(
    "-o", "ConnectTimeout=30",
    "-o", "StrictHostKeyChecking=no"
)
if (-not $AllowPasswordPrompt) {
    $ScpOpts += @("-o", "BatchMode=yes")
    $ScpOpts += @("-o", "NumberOfPasswordPrompts=0")
    $ScpOpts += @("-o", "PreferredAuthentications=publickey")
    $ScpOpts += @("-o", "PubkeyAuthentication=yes")
}

if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) {
    $keyResolved = (Resolve-Path -LiteralPath $SshKeyPath -ErrorAction SilentlyContinue).Path
    if (-not $keyResolved) {
        Write-Host "ERROR: SshKeyPath not found: $SshKeyPath" -ForegroundColor Red
        exit 1
    }
    $SshOpts += @("-i", $keyResolved)
    $ScpOpts += @("-i", $keyResolved)
}

# 无伪终端时远程 sudo 索要密码会无限阻塞；管道到 Out-Null 时连提示都看不到
$SudoCmd = if ($RequestTtyForSudo) { "sudo" } else { "sudo -n" }
$SshTty = if ($RequestTtyForSudo) { @("-t") } else { @() }

$localResolved = (Resolve-Path $deployPackage).Path
$uploadSource = $localResolved
$stagingDir = Join-Path $env:TEMP ("frontcrm_upload_" + [Guid]::NewGuid().ToString("N"))

Write-Host "Preparing upload files (exclude .md)..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# 使用 robocopy 保留目录结构并排除 .md
& robocopy $localResolved $stagingDir /E /NFL /NDL /NJH /NJS /NP /XF *.md | Out-Null
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -gt 7) {
    Write-Host ('ERROR: failed to prepare upload files (robocopy exit {0})' -f $robocopyExitCode) -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

$localDot = Join-Path $stagingDir "."

Write-Host "Creating remote directory (SSH ConnectTimeout=30s)..." -ForegroundColor Yellow
if (-not $AllowPasswordPrompt) {
    Write-Host "  BatchMode (key-based): no SSH key for $SshTarget -> fail in ~30s." -ForegroundColor DarkGray
    Write-Host "  For password upload: .\deploy_full_to_server.ps1 -SkipBuild -AllowPasswordPrompt" -ForegroundColor Cyan
    Write-Host "  Or add your .pub to server ~/.ssh/authorized_keys, or use -SshKeyPath." -ForegroundColor DarkGray
}
& ssh @SshOpts -p $SshPort "$SshTarget" "mkdir -p '$RemoteDeployPath'"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: ssh mkdir failed. Check: network/firewall port 22; ssh key for $SshTarget; or use -AllowPasswordPrompt for password login." -ForegroundColor Red
    exit 1
}

Write-Host "Uploading $DeployPackageName/* -> $RemoteDeployPath/ ..." -ForegroundColor Yellow
& scp @ScpOpts -r -P $SshPort "$localDot" "$($SshTarget):$($RemoteDeployPath)/"

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
    # PS 5.1：双引号内 2>&1、> 会被当成重定向，须用单引号整段 bash
    & ssh @SshOpts -p $SshPort "$SshTarget" 'command -v docker >/dev/null 2>&1 && docker compose ps >/dev/null 2>&1'
    if ($LASTEXITCODE -eq 0) { $useDocker = $true }
}

if ($useDocker) {
    Write-Host "Docker deployment mode detected: rebuilding and starting compose." -ForegroundColor Cyan
    Write-Host ""
    & ssh @SshOpts -p $SshPort "$SshTarget" "cd '$RemoteDeployPath'; docker compose build --no-cache" | Out-Null
    & ssh @SshOpts -p $SshPort "$SshTarget" "cd '$RemoteDeployPath'; docker compose up -d" | Out-Null
}
else {
    Write-Host "Non-Docker mode detected: applying Nginx + dotnet flow." -ForegroundColor Cyan
    Write-Host ""

    # 1) 准备目录 + 覆盖前端 dist
    $srcFront = "${RemoteDeployPath}/CRM.Web/dist"
    $srcBack = "${RemoteDeployPath}/CRM.API/publish"

    Write-Host ">>> Non-Docker: prepare directories..." -ForegroundColor Gray
    if (-not $RequestTtyForSudo) {
        Write-Host ('    Using {0} (non-interactive sudo; use -RequestTtyForSudo to type sudo password).' -f $SudoCmd) -ForegroundColor DarkGray
    }
    $rPrepareDirs = $SudoCmd + ' mkdir -p ' + $NonDockerFrontendRoot + ' ' + $NonDockerBackendRoot + ' && ' + $SudoCmd + ' chown -R ' + $ServerUser + ':' + $ServerUser + ' ' + $NonDockerFrontendRoot + ' ' + $NonDockerBackendRoot
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rPrepareDirs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: prepare directories failed (remote sudo needs password or NOPASSWD)." -ForegroundColor Red
        Write-Host "  Fix A: on server, grant NOPASSWD for $ServerUser for deploy commands under /opt/frontcrm" -ForegroundColor Yellow
        Write-Host "  Fix B: re-run with -RequestTtyForSudo and enter sudo password when prompted" -ForegroundColor Yellow
        exit 1
    }

    Write-Host ('>>> Non-Docker: sync frontend dist ({0} -> {1})' -f $srcFront, $NonDockerFrontendRoot) -ForegroundColor Gray
    $rSyncFront = $SudoCmd + ' rm -rf ' + $NonDockerFrontendRoot + '/*; ' + $SudoCmd + ' cp -r ' + $srcFront + '/* ' + $NonDockerFrontendRoot + '/; ' + $SudoCmd + ' chown -R ' + $ServerUser + ':' + $ServerUser + ' ' + $NonDockerFrontendRoot + '; ' + $SudoCmd + ' find ' + $NonDockerFrontendRoot + ' -type d -exec chmod 755 {} \;; ' + $SudoCmd + ' find ' + $NonDockerFrontendRoot + ' -type f -exec chmod 644 {} \;'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rSyncFront
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: sync frontend failed." -ForegroundColor Red; exit 1 }

    # 2) 覆盖后端 publish
    Write-Host ('>>> Non-Docker: sync backend publish ({0} -> {1})' -f $srcBack, $NonDockerBackendRoot) -ForegroundColor Gray
    $rSyncBack = $SudoCmd + ' rm -rf ' + $NonDockerBackendRoot + '/*; ' + $SudoCmd + ' cp -r ' + $srcBack + '/* ' + $NonDockerBackendRoot + '/; ' + $SudoCmd + ' chown -R ' + $ServerUser + ':' + $ServerUser + ' ' + $NonDockerBackendRoot + '; ' + $SudoCmd + ' find ' + $NonDockerBackendRoot + ' -type d -exec chmod 755 {} \;; ' + $SudoCmd + ' find ' + $NonDockerBackendRoot + ' -type f -exec chmod 644 {} \;'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rSyncBack
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: sync backend failed." -ForegroundColor Red; exit 1 }

    # 3) reload nginx（如果 nginx 存在）
    Write-Host ">>> Non-Docker: nginx reload (if nginx exists)..." -ForegroundColor Gray
    $rNginxReload = 'if command -v nginx >/dev/null 2>&1; then ' + $SudoCmd + ' nginx -t && ' + $SudoCmd + ' systemctl reload nginx; fi'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rNginxReload
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: nginx reload / nginx -t failed." -ForegroundColor Red; exit 1 }

    # 4) restart api：须先释放端口；多行脚本经 PowerShell→ssh 易拆坏（remote bash: syntax error near 'fi'），改为单行
    Write-Host ('>>> Non-Docker: restart crm-api (free port {0} first)...' -f $BackendPort) -ForegroundColor Gray
    $nd = $NonDockerBackendRoot
    $bp = $BackendPort
    # systemd stop 后若仍有 nohup/孤儿 dotnet 占 5000，新实例会 AddressInUse 起不来；须 sudo pkill 清干净再 start
    $restartApiOneLine = 'if ' + $SudoCmd + ' systemctl list-unit-files 2>/dev/null | grep -qF ''crm-api.service''; then ' + $SudoCmd + ' systemctl daemon-reload; ' + $SudoCmd + ' systemctl stop crm-api 2>/dev/null || true; ' + $SudoCmd + ' pkill -f CRM.API.dll 2>/dev/null || true; sleep 3; ' + $SudoCmd + ' systemctl start crm-api || exit 1; sleep 2; else cd ' + $nd + ' || exit 1; pkill -f CRM.API.dll 2>/dev/null || true; sleep 3; export ASPNETCORE_ENVIRONMENT=Production; export ASPNETCORE_URLS=http://0.0.0.0:' + $bp + '; nohup dotnet CRM.API.dll > api.log 2>&1 & sleep 2; fi'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$restartApiOneLine"
    $restartExit = $LASTEXITCODE

    # start 后 Kestrel 就绪晚于 systemctl；仅以 curl /api/v1/health 为准（避免 is-active=activating 时永远失败）
    Write-Host ('>>> Non-Docker: wait for crm-api (GET /api/v1/health on port {0}, up to ~{1}s)...' -f $BackendPort, $ApiHealthWaitSeconds) -ForegroundColor Gray
    $verifyTail = 'test "$ok" = 1 || exit 1'
    $seqMax = [string]$ApiHealthWaitSeconds
    $verifyApiOneLine = 'ok=0; sleep 2; for i in $(seq 1 ' + $seqMax + '); do if curl -sf http://127.0.0.1:' + $bp + '/api/v1/health >/dev/null 2>&1; then ok=1; break; fi; sleep 1; done; ' + $verifyTail
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$verifyApiOneLine"
    $verifyExit = $LASTEXITCODE
    if ($verifyExit -eq 0) {
        Write-Host ('>>> Non-Docker: crm-api health check passed (http://127.0.0.1:{0}/api/v1/health).' -f $BackendPort) -ForegroundColor Green
    }

    if ($restartExit -ne 0 -or $verifyExit -ne 0) {
        Write-Host ('ERROR: restart API failed (restart exit={0}, health-wait exit={1}).' -f $restartExit, $verifyExit) -ForegroundColor Red
        Write-Host '>>> Remote diagnostics (run on server if empty: ssh then see DEPLOY_OPERATION_MANUAL.md §4):' -ForegroundColor Yellow
        $diagGrep5000 = [char]39 + ':5000\b' + [char]39
        $diagApi = 'if ' + $SudoCmd + ' systemctl list-unit-files 2>/dev/null | grep -qF ''crm-api.service''; then ' + $SudoCmd + ' systemctl status crm-api --no-pager -l 2>&1 || true; echo ''--- journal (crm-api) ---''; ' + $SudoCmd + ' journalctl -u crm-api -n 50 --no-pager 2>&1 || true; else echo ''--- no crm-api.service; nohup log ---''; tail -n 80 ' + $nd + '/api.log 2>&1 || true; echo ''--- listen 5000 ---''; ss -ltnp 2>/dev/null | grep -E ' + $diagGrep5000 + ' || true; fi'
        & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$diagApi"
        exit 1
    }

    Write-Host ">>> Non-Docker: crm-api status (is-active)" -ForegroundColor Gray
    $rApiIsActive = 'if ' + $SudoCmd + ' systemctl list-unit-files 2>/dev/null | grep -qF ''crm-api.service''; then ' + $SudoCmd + ' systemctl is-active crm-api; else pgrep -f ''dotnet CRM.API.dll'' >/dev/null && echo active || echo inactive; fi'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rApiIsActive
}

Write-Host ""
Write-Host "Frontend: http://$ServerIP" -ForegroundColor Gray
Write-Host ('API:      http://{0}:{1}/api/v1' -f $ServerIP, $BackendPort) -ForegroundColor Gray
Write-Host ""
Write-Host "deploy_full_to_server.ps1 done." -ForegroundColor Green
