param(
  [string]$SshHost = "129.226.161.3",
  [int]$SshPort = 22,
  [string]$SshUser = "ubuntu",
  [string]$RemotePath = "/opt/frontcrm/frontend",

  # 使用 SSH 私钥实现免密登录（建议先在 Windows 生成密钥并把公钥写入服务器 authorized_keys）
  # 若为空字符串则不额外指定 -i（使用 ssh-agent/默认密钥）
  [string]$SshKeyPath = "",

  [string]$LocalWebRoot = "d:\MyProject\FrontCRM_CSharp\CRM.Web",
  [string]$LocalDistPath = "d:\MyProject\FrontCRM_CSharp\CRM.Web\dist",

  # 需要 sudo 权限但不想自动处理 sudo 密码时，建议设置为 $false
  [bool]$UseSudo = $true
)

function Assert-CommandExists {
  param([string]$Command)
  $cmd = Get-Command $Command -ErrorAction SilentlyContinue
  if (-not $cmd) {
    throw "Command not found: $Command"
  }
}

$TempBuildRoot = $null

try {
  Write-Host "=== FrontCRM Frontend Deploy (frontend only) ===" -ForegroundColor Cyan
  Write-Host "SSH: $SshUser@$SshHost`:$SshPort" -ForegroundColor Cyan
  Write-Host "Remote: $RemotePath" -ForegroundColor Cyan
  Write-Host "Local Web Root: $LocalWebRoot" -ForegroundColor Cyan
  Write-Host ""

  Assert-CommandExists "npm"
  Assert-CommandExists "scp"
  Assert-CommandExists "ssh"

  if (-not (Test-Path $LocalWebRoot)) {
    throw "LocalWebRoot not found: $LocalWebRoot"
  }

  if (-not (Test-Path $LocalDistPath)) {
    # 允许 dist 不存在（第一次构建会生成）
    Write-Host "Local dist not found yet, will build it: $LocalDistPath" -ForegroundColor Yellow
  }

  # 1) Build frontend（在临时目录构建，避免影响当前开发用的 node_modules）
  Write-Host "=== Step 1: npm ci / npm run build (temp workspace) ===" -ForegroundColor Green
  $TempBuildRoot = Join-Path $env:TEMP ("frontcrm_frontend_build_{0}" -f ([guid]::NewGuid().ToString("N")))
  New-Item -ItemType Directory -Path $TempBuildRoot -Force | Out-Null

  $excludeNames = @(
    "node_modules",
    "dist",
    ".cursor",
    "dev.log",
    "dev_new.log",
    "frontend.log",
    "frontend2.log",
    "frontcrm_web_update.tar.gz"
  )

  # 拷贝必要源码/配置到临时目录（不包含 node_modules/dist）
  foreach ($item in (Get-ChildItem -Force $LocalWebRoot)) {
    if ($excludeNames -contains $item.Name) { continue }
    $dest = Join-Path $TempBuildRoot $item.Name
    if ($item.PSIsContainer) {
      Copy-Item -Recurse -Force $item.FullName $dest
    } else {
      Copy-Item -Force $item.FullName $dest
    }
  }

  Push-Location $TempBuildRoot

  # npm ci 比 npm install 更可预测（lockfile 存在时）
  if (Test-Path (Join-Path $TempBuildRoot "package-lock.json")) {
    npm ci --no-audit --no-fund
  } else {
    npm install --no-audit --no-fund
  }

  npm run build
  Pop-Location

  # 将 temp/dist 覆盖拷回本地 dist，供后续 scp 上传
  if (-not (Test-Path $LocalDistPath)) {
    New-Item -ItemType Directory -Path $LocalDistPath -Force | Out-Null
  }
  Remove-Item -Recurse -Force (Join-Path $LocalDistPath "*") -ErrorAction SilentlyContinue
  Copy-Item -Recurse -Force (Join-Path $TempBuildRoot "dist\*") $LocalDistPath

  if (-not (Test-Path (Join-Path $LocalDistPath "index.html"))) {
    throw "Build output missing: $LocalDistPath/index.html"
  }

  # 2) Ensure remote directory exists
  Write-Host ""
  Write-Host "=== Step 2: upload dist to server ===" -ForegroundColor Green

  $sudoPrefix = ""
  if ($UseSudo) { $sudoPrefix = "sudo " }

  $sshKeyArgs = @()
  if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) {
    $sshKeyArgs = @("-i", $SshKeyPath)
  }

  & ssh @sshKeyArgs -p $SshPort "$SshUser@$SshHost" "$sudoPrefix mkdir -p `"$RemotePath`""

  # Copy contents (dist/* -> RemotePath/)
  # 注意：Windows PowerShell 里需要对 * 做正确处理，这里用引号包住通配符
  $remoteTarget = ($SshUser + "@" + $SshHost + ":" + $RemotePath + "/")
  $scpArgs = @(
    "-r",
    "-P", $SshPort,
    (Join-Path $LocalDistPath "*"),
    $remoteTarget
  )
  if ($sshKeyArgs.Count -gt 0) {
    & scp @sshKeyArgs @scpArgs
  } else {
    & scp @scpArgs
  }

  Write-Host "Upload done." -ForegroundColor Green

  # 3) Reload nginx (if nginx is running)
  Write-Host ""
  Write-Host "=== Step 3: nginx reload ===" -ForegroundColor Green
  & ssh @sshKeyArgs -p $SshPort "$SshUser@$SshHost" "$sudoPrefix nginx -t && $sudoPrefix systemctl reload nginx" | Out-Null

  Write-Host ""
  Write-Host "=== Done ===" -ForegroundColor Green
  Write-Host "Frontend should be updated at: http://129.226.161.3:80/" -ForegroundColor Cyan
} catch {
  Write-Error $_
  exit 1
}
finally {
  if ($TempBuildRoot -and (Test-Path $TempBuildRoot)) {
    Remove-Item -Recurse -Force $TempBuildRoot -ErrorAction SilentlyContinue
  }
}

