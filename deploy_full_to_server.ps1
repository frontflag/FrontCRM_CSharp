# Deploy FrontCRM — 前端 + 后端 一并构建并上传到服务器
# 本文件须保存为「UTF-8 带 BOM」；否则 Windows PowerShell 5.1 会按 ANSI 误读中文与引号，出现 UnexpectedToken `}`。
#
# 流程：
#   1) 调用 build_with_temp_path.ps1：npm build + dotnet publish，生成 frontcrm_deploy（含 CRM.Web/dist、CRM.API/publish、docker-compose 等）
#   2) 打包为 tar.gz 后 scp 单文件上传（带百分比进度），远端解压到部署目录
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
#   .\deploy_full_to_server.ps1 -ApiHealthWaitSeconds 60   # 非 Docker：systemctl start 后等健康检查的最长秒数（默认 30）

#   .\deploy_full_to_server.ps1 -Tenant idesemi -SkipLoginPage
#   .\deploy_full_to_server.ps1 -Tenant semicore -IncludeLoginPage

param(
    [switch]$SkipBuild,
    # 默认非交互：若未配置密钥而只用密码，SSH 会一直卡住；此时请改用 -AllowPasswordPrompt
    [switch]$AllowPasswordPrompt,
    # 远程 sudo 无 NOPASSWD 且无 TTY 时会一直等密码（看起来像卡住）。默认用 sudo -n 秒失败；需交互输密码时加本开关（会 ssh -t）
    [switch]$RequestTtyForSudo,
    [ValidateSet('semicore', 'idesemi', 'fz', 'ecoinf', '')]
    [string]$Tenant = '',
    # 默认上传登录页主题（dist/tenant）；加 -SkipLoginPage 保留服务器上现有 tenant 目录
    [switch]$IncludeLoginPage,
    [switch]$SkipLoginPage,
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
    [int]$ApiHealthWaitSeconds = 30,
    # Optional: ssh/scp -i (PowerShell 5.1: save this script as UTF-8 with BOM if you use non-ASCII in messages)
    [string]$SshKeyPath = ""
)

$ErrorActionPreference = "Stop"

# Guard: this script must be saved as UTF-8 with BOM for Windows PowerShell 5.1.
try {
    $selfPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($selfPath) -and (Test-Path -LiteralPath $selfPath)) {
        $bytes = [System.IO.File]::ReadAllBytes($selfPath)
        $hasUtf8Bom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        if (-not $hasUtf8Bom) {
            Write-Host "ERROR: deploy_full_to_server.ps1 must be UTF-8 with BOM." -ForegroundColor Red
            Write-Host "Fix: re-save this file as UTF-8 with BOM, then re-run." -ForegroundColor Yellow
            exit 1
        }
    }
}
catch {
    Write-Host ("WARN: encoding self-check skipped: {0}" -f $_.Exception.Message) -ForegroundColor DarkYellow
}

$RepoRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Get-Location).Path
}

$uploadLoginTheme = -not $SkipLoginPage
if ($IncludeLoginPage) { $uploadLoginTheme = $true }
if ($SkipLoginPage) { $uploadLoginTheme = $false }

if ([string]::IsNullOrWhiteSpace($Tenant)) {
    $Tenant = 'semicore'
}

# -SkipBuild 时优先使用按租户批量构建的包（含完整 dist/help）；避免误用陈旧的 frontcrm_deploy。
if ($DeployPackageName -eq 'frontcrm_deploy' -and $SkipBuild) {
    $tenantPackage = switch ($Tenant) {
        'semicore' { 'frontcrm_deploy_semicore' }
        'idesemi' { 'frontcrm_deploy_idesemi' }
        'ecoinf' { 'frontcrm_deploy_ecoinf' }
        'fz' { 'frontcrm_deploy_semicore' }
        default { $null }
    }
    if ($tenantPackage) {
        $tenantPackagePath = Join-Path $RepoRoot $tenantPackage
        $tenantIndex = Join-Path $tenantPackagePath 'CRM.Web/dist/index.html'
        if (Test-Path $tenantIndex) {
            Write-Host ">>> -SkipBuild: using tenant package $tenantPackage" -ForegroundColor Cyan
            $DeployPackageName = $tenantPackage
        }
    }
}

$deployPackage = Join-Path $RepoRoot $DeployPackageName

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM full deploy (frontend + backend)" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Repo root: $RepoRoot" -ForegroundColor Gray
Write-Host "Tenant: $Tenant | Upload login theme: $uploadLoginTheme" -ForegroundColor Gray
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
    & $buildScript -Tenant $Tenant
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

$verifyHelpScript = Join-Path $RepoRoot "scripts\verify-deploy-help.ps1"
if (Test-Path $verifyHelpScript) {
    Write-Host ">>> Verify help docs in deploy package..." -ForegroundColor Gray
    & $verifyHelpScript -DeployPackagePath $deployPackage -RepoRoot $RepoRoot
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: deploy package help is stale or incomplete." -ForegroundColor Red
        Write-Host "  Fix: cd CRM.Web; npm run build" -ForegroundColor Yellow
        Write-Host "  Or: .\scripts\build-batch-tenants.ps1 then redeploy without stale -SkipBuild package." -ForegroundColor Yellow
        exit $LASTEXITCODE
    }
    Write-Host ""
}
else {
    Write-Host "WARN: verify-deploy-help.ps1 not found, skipping help check." -ForegroundColor DarkYellow
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
# ServerAlive 勿过紧：Non-Docker 同步大目录时远端 I/O 可能短暂无回包，10s×3 易 Connection reset
$SshOpts = @(
    "-o", "ConnectTimeout=30",
    "-o", "ServerAliveInterval=30",
    "-o", "ServerAliveCountMax=20",
    "-o", "StrictHostKeyChecking=accept-new",
    "-o", "LogLevel=ERROR",
    "-o", "GSSAPIAuthentication=no",
    "-o", "KbdInteractiveAuthentication=no"
)
if (-not $AllowPasswordPrompt) {
    $SshOpts += @("-o", "BatchMode=yes")
    $SshOpts += @("-o", "NumberOfPasswordPrompts=0")
    $SshOpts += @("-o", "PreferredAuthentications=publickey")
    $SshOpts += @("-o", "PubkeyAuthentication=yes")
}
$ScpOpts = @(
    "-o", "ConnectTimeout=30",
    "-o", "ServerAliveInterval=30",
    "-o", "ServerAliveCountMax=20",
    "-o", "StrictHostKeyChecking=accept-new",
    "-o", "LogLevel=ERROR",
    "-o", "GSSAPIAuthentication=no",
    "-o", "KbdInteractiveAuthentication=no"
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
$stagingDir = Join-Path $env:TEMP ("frontcrm_upload_" + [Guid]::NewGuid().ToString("N"))

Write-Host "Preparing upload files..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# 须包含 CRM.Web/dist/help/**/*.md（右侧帮助文档）；勿排除 *.md
& robocopy $localResolved $stagingDir /E /NFL /NDL /NJH /NJS /NP | Out-Null
$robocopyExitCode = $LASTEXITCODE
if ($robocopyExitCode -gt 7) {
    Write-Host ('ERROR: failed to prepare upload files (robocopy exit {0})' -f $robocopyExitCode) -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

$stagingHelpPages = Join-Path $stagingDir "CRM.Web\dist\help\pages"
if (-not (Test-Path $stagingHelpPages)) {
    Write-Host "ERROR: upload staging missing CRM.Web/dist/help/pages" -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}
$stagingHelpCount = (Get-ChildItem -Path $stagingHelpPages -File -ErrorAction SilentlyContinue | Measure-Object).Count
if ($stagingHelpCount -lt 50) {
    Write-Host "ERROR: upload staging has only $stagingHelpCount help pages (expected >= 50). .md files may have been excluded." -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}
Write-Host "  help pages in staging: $stagingHelpCount" -ForegroundColor DarkGray

Write-Host "Creating remote directory (SSH ConnectTimeout=30s)..." -ForegroundColor Yellow
if (-not $AllowPasswordPrompt) {
    Write-Host "  BatchMode (key-based): need SSH pubkey for $SshTarget, or use -AllowPasswordPrompt / -SshKeyPath." -ForegroundColor DarkGray
}
$mkdirJob = Start-Job -ScriptBlock {
    param($SshExe, $Opts, $Port, $Target, $RemotePath)
    # 勿让 ssh stderr（如 known_hosts Warning）变成 ErrorRecord，否则父进程 $ErrorActionPreference=Stop 会在 Receive-Job 处崩
    $ErrorActionPreference = 'Continue'
    $out = & $SshExe @Opts -p $Port $Target "mkdir -p '$RemotePath'" 2>&1
    $code = $LASTEXITCODE
    if ($null -eq $code) { $code = 1 }
    return @{ ExitCode = [int]$code; Output = ($out | Out-String) }
} -ArgumentList $sshPath.Source, $SshOpts, $SshPort, $SshTarget, $RemoteDeployPath
$mkdirWait = Wait-Job -Job $mkdirJob -Timeout 60
if (-not $mkdirWait -or $mkdirJob.State -eq 'Running') {
    Stop-Job -Job $mkdirJob -ErrorAction SilentlyContinue
    Remove-Job -Job $mkdirJob -Force -ErrorAction SilentlyContinue
    Write-Host "ERROR: ssh mkdir timed out after 60s (Windows SSH hang). Press Ctrl+C if still stuck, then re-run the same command." -ForegroundColor Red
    Write-Host "  Tip: close hung ssh.exe in Task Manager, or run: Get-Process ssh | Stop-Process -Force" -ForegroundColor Yellow
    exit 1
}
$mkdirResult = $null
try {
    $mkdirResult = Receive-Job -Job $mkdirJob -ErrorAction SilentlyContinue
} catch {
    Write-Host ("WARNING: ssh mkdir job noise ignored: {0}" -f $_.Exception.Message) -ForegroundColor DarkYellow
}
Remove-Job -Job $mkdirJob -Force -ErrorAction SilentlyContinue
$mkdirExit = 1
if ($mkdirResult -is [hashtable] -and $mkdirResult.ContainsKey('ExitCode')) {
    $mkdirExit = [int]$mkdirResult.ExitCode
} elseif ($mkdirResult -is [int]) {
    $mkdirExit = $mkdirResult
}
if ($mkdirExit -ne 0) {
    Write-Host "ERROR: ssh mkdir failed (exit $mkdirExit). Check: network/firewall port 22; ssh key for $SshTarget; or:" -ForegroundColor Red
    Write-Host "  .\deploy_full_to_server.ps1 -SkipBuild -AllowPasswordPrompt -ServerIP `"$ServerIP`"" -ForegroundColor Cyan
    if ($mkdirResult -is [hashtable] -and $mkdirResult.Output) {
        Write-Host ($mkdirResult.Output.Trim()) -ForegroundColor DarkGray
    }
    exit 1
}

# 先清远端 Vite/publish 旧树，再传单文件归档（scp 对单文件有百分比进度；-r 目录上传无总进度且易残留旧 hash）
Write-Host "Cleaning remote stale frontend/backend build trees before upload..." -ForegroundColor Yellow
$rCleanStale = "rm -rf '$RemoteDeployPath/CRM.Web/dist' '$RemoteDeployPath/CRM.API/publish'"
& ssh @SshOpts -p $SshPort "$SshTarget" $rCleanStale
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: failed to clean remote dist/publish before upload." -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

$tarPath = Get-Command tar -ErrorAction SilentlyContinue
if (-not $tarPath) {
    Write-Host "ERROR: tar not found (Windows 10+ provides tar.exe). Cannot pack upload archive." -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    exit 1
}

$archiveName = "frontcrm_upload_" + [Guid]::NewGuid().ToString("N") + ".tar.gz"
$archiveLocal = Join-Path $env:TEMP $archiveName
$archiveRemote = "/tmp/$archiveName"

Write-Host "Packing upload archive (tar.gz)..." -ForegroundColor Yellow
# bsdtar/Windows tar：-C staging 后打 .，远端 GNU tar 可解压
& tar -czf $archiveLocal -C $stagingDir .
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archiveLocal)) {
    Write-Host "ERROR: failed to create upload archive." -ForegroundColor Red
    Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $archiveLocal -Force -ErrorAction SilentlyContinue
    exit 1
}
$archiveMb = [Math]::Round((Get-Item -LiteralPath $archiveLocal).Length / 1MB, 2)
Write-Host ("  Archive size: {0} MB (source package ~{1} MB)" -f $archiveMb, [Math]::Round($packageSize, 0)) -ForegroundColor DarkGray

# staging 已打进归档，可先删以省磁盘
Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ("Uploading archive with progress -> {0}:{1}" -f $SshTarget, $archiveRemote) -ForegroundColor Yellow
Write-Host "NOTE: 仓库 data/ 不在部署包内；解压不会删除远程仅有、本地没有的目录。生产 data/ip2region 请在服务器单独维护。" -ForegroundColor DarkGray
# 单文件 scp 会在控制台打印 name / % / 速度 / ETA（勿重定向输出，否则看不到进度）
& scp @ScpOpts -P $SshPort $archiveLocal "${SshTarget}:${archiveRemote}"
$scpExit = $LASTEXITCODE
if ($scpExit -ne 0) {
    Write-Host "ERROR: scp upload failed." -ForegroundColor Red
    Remove-Item -Path $archiveLocal -Force -ErrorAction SilentlyContinue
    & ssh @SshOpts -p $SshPort "$SshTarget" "rm -f '$archiveRemote'" 2>$null | Out-Null
    exit 1
}

Write-Host "Extracting archive on server -> $RemoteDeployPath/ ..." -ForegroundColor Yellow
$rExtract = "mkdir -p '$RemoteDeployPath' && tar -xzf '$archiveRemote' -C '$RemoteDeployPath' && rm -f '$archiveRemote'"
& ssh @SshOpts -p $SshPort "$SshTarget" $rExtract
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: remote extract failed." -ForegroundColor Red
    Remove-Item -Path $archiveLocal -Force -ErrorAction SilentlyContinue
    & ssh @SshOpts -p $SshPort "$SshTarget" "rm -f '$archiveRemote'" 2>$null | Out-Null
    exit 1
}

Remove-Item -Path $archiveLocal -Force -ErrorAction SilentlyContinue

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
    Write-Host "Detecting remote deployment mode (docker vs non-docker, timeout 15s)..." -ForegroundColor Yellow
    # timeout 避免远程 docker compose ps 卡死；路径由 PowerShell 展开后传入 bash
    $remotePath = $RemoteDeployPath.Replace("'", "'\''")
    $detectDockerCmd = 'timeout 15 sh -c ''command -v docker >/dev/null 2>&1 && test -f ''' + $remotePath + '/docker-compose.yml'' && cd ''' + $remotePath + ''' && docker compose ps -q 2>/dev/null | head -1 | grep -q .'' 2>/dev/null'
    & ssh @SshOpts -p $SshPort "$SshTarget" $detectDockerCmd
    if ($LASTEXITCODE -eq 0) { $useDocker = $true }
    if ($useDocker) {
        Write-Host "  -> Docker (compose available on server)" -ForegroundColor Gray
    } else {
        Write-Host "  -> Non-Docker (Nginx + systemd crm-api)" -ForegroundColor Gray
    }
    Write-Host ""
}

if ($useDocker) {
    Write-Host "Docker deployment: docker compose build --no-cache (may take several minutes, output below)..." -ForegroundColor Cyan
    & ssh @SshOpts -p $SshPort "$SshTarget" "cd '$RemoteDeployPath'; docker compose build --no-cache"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: docker compose build failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "Docker deployment: docker compose up -d ..." -ForegroundColor Cyan
    & ssh @SshOpts -p $SshPort "$SshTarget" "cd '$RemoteDeployPath'; docker compose up -d"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: docker compose up failed." -ForegroundColor Red
        exit 1
    }
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
    if (-not $uploadLoginTheme) {
        Write-Host '    SkipLoginPage: server dist/tenant will be preserved' -ForegroundColor DarkYellow
    }
    $skipLoginFlag = if ($uploadLoginTheme) { '0' } else { '1' }
    # 必须单行传给 ssh：PS 5.1 对未引用的多行字符串会按换行拆成多个参数，远程 bash 会报 syntax error (unexpected EOF)
    $rSyncFrontParts = @()
    if ($skipLoginFlag -eq '1') {
        $rSyncFrontParts += (
            $SudoCmd + ' rm -rf /tmp/frontcrm_tenant_bak 2>/dev/null || true; ' +
            'if [ -d ''' + $NonDockerFrontendRoot + '/tenant'' ]; then ' + $SudoCmd + ' cp -a ''' + $NonDockerFrontendRoot + '/tenant'' /tmp/frontcrm_tenant_bak; fi'
        )
    }
    $rSyncFrontParts += $SudoCmd + ' rm -rf ' + $NonDockerFrontendRoot + '/*'
    $rSyncFrontParts += $SudoCmd + ' cp -r ' + $srcFront + '/* ' + $NonDockerFrontendRoot + '/'
    if ($skipLoginFlag -eq '1') {
        $rSyncFrontParts += (
            'if [ -d /tmp/frontcrm_tenant_bak ]; then ' + $SudoCmd + ' rm -rf ' + $NonDockerFrontendRoot + '/tenant; ' +
            $SudoCmd + ' cp -a /tmp/frontcrm_tenant_bak ' + $NonDockerFrontendRoot + '/tenant; ' +
            $SudoCmd + ' rm -rf /tmp/frontcrm_tenant_bak; fi'
        )
    }
    # chmod -R 远快于 find -exec（万级文件时后者易拖死 SSH）
    $rSyncFrontParts += $SudoCmd + ' chown -R ' + $ServerUser + ':' + $ServerUser + ' ' + $NonDockerFrontendRoot
    $rSyncFrontParts += $SudoCmd + ' chmod -R u=rwX,go=rX ' + $NonDockerFrontendRoot
    $rSyncFront = ($rSyncFrontParts -join '; ')
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$rSyncFront"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: sync frontend failed." -ForegroundColor Red; exit 1 }

    # 2) 覆盖后端 publish
    Write-Host ('>>> Non-Docker: sync backend publish ({0} -> {1})' -f $srcBack, $NonDockerBackendRoot) -ForegroundColor Gray
    $rSyncBack = $SudoCmd + ' rm -rf ' + $NonDockerBackendRoot + '/*; ' + $SudoCmd + ' cp -r ' + $srcBack + '/* ' + $NonDockerBackendRoot + '/; ' + $SudoCmd + ' chown -R ' + $ServerUser + ':' + $ServerUser + ' ' + $NonDockerBackendRoot + '; ' + $SudoCmd + ' chmod -R u=rwX,go=rX ' + $NonDockerBackendRoot
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rSyncBack
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: sync backend failed." -ForegroundColor Red; exit 1 }

    # 3) reload nginx（如果 nginx 存在）
    Write-Host ">>> Non-Docker: nginx reload (if nginx exists)..." -ForegroundColor Gray
    $rNginxReload = 'if command -v nginx >/dev/null 2>&1; then ' + $SudoCmd + ' nginx -t && ' + $SudoCmd + ' systemctl reload nginx; fi'
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" $rNginxReload
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: nginx reload / nginx -t failed." -ForegroundColor Red; exit 1 }

    # 4) restart api: free port first; keep remote bash in one line
    Write-Host ('>>> Non-Docker: restart crm-api (free port {0} first)...' -f $BackendPort) -ForegroundColor Gray
    $nd = $NonDockerBackendRoot
    $bp = $BackendPort
    # Stop then start separately so a failed start does not look like a hang on "Detecting...".
    $stopApiOneLine = 'if ' + $SudoCmd + ' systemctl list-unit-files 2>/dev/null | grep -qF ''crm-api.service''; then ' + $SudoCmd + ' systemctl stop crm-api 2>/dev/null || true; sleep 2; else pkill -f ''[d]otnet CRM.API.dll'' 2>/dev/null || true; sleep 2; fi'
    Write-Host '>>> Non-Docker: stopping crm-api...' -ForegroundColor Gray
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$stopApiOneLine"

    $startApiOneLine = 'if ' + $SudoCmd + ' systemctl list-unit-files 2>/dev/null | grep -qF ''crm-api.service''; then ' + $SudoCmd + ' systemctl daemon-reload; ' + $SudoCmd + ' systemctl start crm-api || exit 1; sleep 2; else cd ' + $nd + ' || exit 1; export ASPNETCORE_ENVIRONMENT=Production; export ASPNETCORE_URLS=http://0.0.0.0:' + $bp + '; nohup dotnet CRM.API.dll > api.log 2>&1 & sleep 2; fi'
    Write-Host '>>> Non-Docker: starting crm-api...' -ForegroundColor Gray
    & ssh @SshTty @SshOpts -p $SshPort "$SshTarget" "$startApiOneLine"
    $restartExit = $LASTEXITCODE

    # Kestrel may be ready after systemctl returns; verify by health endpoint.
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
        Write-Host '>>> Remote diagnostics (run on server if empty: ssh then see DEPLOY_OPERATION_MANUAL.md section 4):' -ForegroundColor Yellow
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
exit 0
