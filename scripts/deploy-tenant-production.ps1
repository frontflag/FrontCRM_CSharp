# 按公司（Tenant）参数化构建并部署 FrontCRM
# 配置：deploy/tenants.json
#
# 公司1 Semicore  129.226.161.3  蓝色登录页  left-right
# 公司2 IDESemi   43.154.198.15  紫色登录页  right-left（可在 .env.production.idesemi 改）
# 仿真 FZ        43.129.212.65  与 Semicore 相同构建（production.semicore）
#
# 示例：
#   .\scripts\deploy-tenant-production.ps1 -Tenant semicore
#   .\scripts\deploy-tenant-production.ps1 -Tenant idesemi -SkipLoginPage
#   .\scripts\deploy-tenant-production.ps1 -Tenant idesemi -IncludeLoginPage
#   .\scripts\deploy-tenant-production.ps1 -Tenant fz

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('semicore', 'idesemi', 'fz')]
    [string]$Tenant,

    [switch]$SkipBuild,
    [switch]$AllowPasswordPrompt,
    [switch]$RequestTtyForSudo,
    [switch]$IncludeLoginPage,
    [switch]$SkipLoginPage,
    [string]$SshKeyPath = "",
    [ValidateSet("auto", "docker", "nonDocker")]
    [string]$DeploymentMode = "auto"
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$tenantsFile = Join-Path $RepoRoot "deploy\tenants.json"
if (-not (Test-Path $tenantsFile)) {
    throw "tenants.json not found: $tenantsFile"
}

$allTenants = Get-Content $tenantsFile -Raw | ConvertFrom-Json
$cfg = $allTenants.$Tenant
if (-not $cfg) {
    throw "Unknown tenant in tenants.json: $Tenant"
}

Write-Host ""
Write-Host "=== Deploy tenant: $Tenant ($($cfg.displayName)) ===" -ForegroundColor Cyan
Write-Host "Server: $($cfg.serverUser)@$($cfg.serverIP)" -ForegroundColor Gray
Write-Host ""

$deployScript = Join-Path $RepoRoot "deploy_full_to_server.ps1"
if (-not (Test-Path $deployScript)) {
    throw "deploy_full_to_server.ps1 not found: $deployScript"
}

$params = @{
    Tenant               = $Tenant
    ServerIP             = [string]$cfg.serverIP
    SshPort              = [int]$cfg.sshPort
    ServerUser           = [string]$cfg.serverUser
    RemoteDeployPath     = [string]$cfg.remoteDeployPath
    NonDockerFrontendRoot = [string]$cfg.nonDockerFrontendRoot
    NonDockerBackendRoot  = [string]$cfg.nonDockerBackendRoot
    DeploymentMode       = $DeploymentMode
}

if ($SkipBuild) { $params.SkipBuild = $true }
if ($AllowPasswordPrompt) { $params.AllowPasswordPrompt = $true }
if ($RequestTtyForSudo) { $params.RequestTtyForSudo = $true }
if ($IncludeLoginPage) { $params.IncludeLoginPage = $true }
if ($SkipLoginPage) { $params.SkipLoginPage = $true }
if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) { $params.SshKeyPath = $SshKeyPath }

& $deployScript @params
exit $LASTEXITCODE
