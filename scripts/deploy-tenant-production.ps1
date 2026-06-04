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
#   .\scripts\deploy-tenant-production.ps1 -Tenant all

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('semicore', 'idesemi', 'fz', 'all')]
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

function Get-DeployTenantChildParams {
    param([string]$TargetTenant)

    $child = @{
        Tenant = $TargetTenant
    }
    if ($SkipBuild) { $child.SkipBuild = $true }
    if ($AllowPasswordPrompt) { $child.AllowPasswordPrompt = $true }
    if ($RequestTtyForSudo) { $child.RequestTtyForSudo = $true }
    if ($IncludeLoginPage) { $child.IncludeLoginPage = $true }
    if ($SkipLoginPage) { $child.SkipLoginPage = $true }
    if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) { $child.SshKeyPath = $SshKeyPath }
    if ($DeploymentMode -ne 'auto') { $child.DeploymentMode = $DeploymentMode }
    return $child
}

if ($Tenant -eq 'all') {
    $batchTenants = @('semicore', 'idesemi', 'fz')
    $failedTenants = New-Object System.Collections.Generic.List[string]
    $succeededTenants = New-Object System.Collections.Generic.List[string]

    Write-Host ""
    Write-Host "=== Deploy ALL tenants (sequential) ===" -ForegroundColor Cyan
    Write-Host "Order: $($batchTenants -join ' -> ')" -ForegroundColor Gray
    Write-Host ""

    for ($i = 0; $i -lt $batchTenants.Count; $i++) {
        $t = $batchTenants[$i]
        Write-Host ""
        Write-Host ">>> Batch $($i + 1)/$($batchTenants.Count): $t" -ForegroundColor Magenta
        Write-Host ""

        $childParams = Get-DeployTenantChildParams -TargetTenant $t
        & $PSCommandPath @childParams
        if ($LASTEXITCODE -ne 0) {
            $failedTenants.Add($t) | Out-Null
            Write-Host ""
            Write-Host "!!! Tenant '$t' deploy failed (exit $LASTEXITCODE); continuing with next tenant..." -ForegroundColor Red
        } else {
            $succeededTenants.Add($t) | Out-Null
        }
    }

    $successCount = $succeededTenants.Count
    $failCount = $failedTenants.Count
    $summaryColor = if ($failCount -gt 0) { 'Red' } else { 'Green' }

    Write-Host ""
    Write-Host "=== 批量发布完成 ===" -ForegroundColor $summaryColor
    Write-Host "发布成功: $successCount 个" -ForegroundColor Green
    if ($successCount -gt 0) {
        Write-Host "  成功租户: $($succeededTenants -join ', ')" -ForegroundColor Gray
    }
    Write-Host "发布失败: $failCount 个" -ForegroundColor $(if ($failCount -gt 0) { 'Red' } else { 'Gray' })
    if ($failCount -gt 0) {
        Write-Host "  失败租户: $($failedTenants -join ', ')" -ForegroundColor Red
        exit 1
    }

    exit 0
}

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

$effectiveDeploymentMode = $DeploymentMode
if ($effectiveDeploymentMode -eq 'auto' -and $cfg.deploymentMode) {
    $effectiveDeploymentMode = [string]$cfg.deploymentMode
}

$params = @{
    Tenant               = $Tenant
    ServerIP             = [string]$cfg.serverIP
    SshPort              = [int]$cfg.sshPort
    ServerUser           = [string]$cfg.serverUser
    RemoteDeployPath     = [string]$cfg.remoteDeployPath
    NonDockerFrontendRoot = [string]$cfg.nonDockerFrontendRoot
    NonDockerBackendRoot  = [string]$cfg.nonDockerBackendRoot
    DeploymentMode       = $effectiveDeploymentMode
}

if ($SkipBuild) { $params.SkipBuild = $true }
if ($AllowPasswordPrompt) { $params.AllowPasswordPrompt = $true }
if ($RequestTtyForSudo) { $params.RequestTtyForSudo = $true }
if ($IncludeLoginPage) { $params.IncludeLoginPage = $true }
if ($SkipLoginPage) { $params.SkipLoginPage = $true }
if (-not [string]::IsNullOrWhiteSpace($SshKeyPath)) { $params.SshKeyPath = $SshKeyPath }

& $deployScript @params
exit $LASTEXITCODE
