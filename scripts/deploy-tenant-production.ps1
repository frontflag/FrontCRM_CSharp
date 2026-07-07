# 按公司（Tenant）参数化构建并部署 FrontCRM
# 配置：deploy/tenants.json
#
# 公司1 Semicore  129.226.161.3  蓝色登录页  left-right
# 公司2 IDESemi   43.154.198.15  紫色登录页  right-left（可在 .env.production.idesemi 改）
# 公司3 ECO-INF  ecoinf  深色荧光绿登录页（production.ecoinf，部署前请在 tenants.json 配置 serverIP）
# 仿真 FZ        43.129.212.65  与 Semicore 相同构建（production.semicore）
#
# 示例：
#   .\scripts\deploy-tenant-production.ps1 -Tenant semicore
#   .\scripts\deploy-tenant-production.ps1 -Tenant idesemi -SkipLoginPage
#   .\scripts\deploy-tenant-production.ps1 -Tenant idesemi -IncludeLoginPage
#   .\scripts\deploy-tenant-production.ps1 -Tenant ecoinf
#   .\scripts\deploy-tenant-production.ps1 -Tenant fz
#   .\scripts\deploy-tenant-production.ps1 -Tenant all

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('semicore', 'idesemi', 'fz', 'ecoinf', 'all')]
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

function Initialize-DeployScriptConsole {
    # Windows PowerShell 5.1 reads UTF-8 .ps1 reliably only with BOM; align console output too.
    try {
        if ($Host.Name -eq 'ConsoleHost') {
            chcp 65001 | Out-Null
            [Console]::InputEncoding = [System.Text.Encoding]::UTF8
            [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
            $script:OutputEncoding = [System.Text.Encoding]::UTF8
        }
    } catch {
        # Non-interactive hosts may reject chcp; ignore.
    }
}

Initialize-DeployScriptConsole

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
    $batchTenants = @('semicore', 'idesemi', 'ecoinf', 'fz')
    $failedTenants = New-Object System.Collections.Generic.List[string]
    $succeededTenants = New-Object System.Collections.Generic.List[string]
    $tenantResults = New-Object System.Collections.Generic.List[object]
    $batchStartTime = Get-Date

    Write-Host ""
    Write-Host "=== Deploy ALL tenants (sequential) ===" -ForegroundColor Cyan
    Write-Host "Order: $($batchTenants -join ' -> ')" -ForegroundColor Gray
    Write-Host "Started at: $($batchStartTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
    Write-Host ""

    for ($i = 0; $i -lt $batchTenants.Count; $i++) {
        $t = $batchTenants[$i]
        Write-Host ""
        Write-Host ">>> Batch $($i + 1)/$($batchTenants.Count): $t" -ForegroundColor Magenta
        Write-Host ""

        $tenantStartTime = Get-Date
        $childParams = Get-DeployTenantChildParams -TargetTenant $t
        & $PSCommandPath @childParams
        $tenantExitCode = $LASTEXITCODE
        $tenantEndTime = Get-Date
        $tenantElapsedMinutes = [math]::Round(($tenantEndTime - $tenantStartTime).TotalMinutes, 1)
        $tenantSucceeded = ($tenantExitCode -eq 0)

        $tenantResults.Add([PSCustomObject]@{
            Tenant         = $t
            Succeeded      = $tenantSucceeded
            StartedAt      = $tenantStartTime
            FinishedAt     = $tenantEndTime
            ElapsedMinutes = $tenantElapsedMinutes
            ExitCode       = $tenantExitCode
        }) | Out-Null

        if (-not $tenantSucceeded) {
            $failedTenants.Add($t) | Out-Null
            Write-Host ""
            Write-Host "!!! Tenant '$t' deploy failed (exit $tenantExitCode); continuing with next tenant..." -ForegroundColor Red
        } else {
            $succeededTenants.Add($t) | Out-Null
        }
    }

    $successCount = $succeededTenants.Count
    $failCount = $failedTenants.Count
    $summaryColor = if ($failCount -gt 0) { 'Red' } else { 'Green' }
    $batchEndTime = Get-Date
    $elapsedMinutes = [math]::Round(($batchEndTime - $batchStartTime).TotalMinutes, 1)

    Write-Host ""
    Write-Host "=== Batch deploy summary ===" -ForegroundColor $summaryColor
    Write-Host "Started at: $($batchStartTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
    Write-Host "Finished at: $($batchEndTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
    Write-Host "Elapsed: $elapsedMinutes minute(s)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "--- Per tenant ---" -ForegroundColor Cyan
    foreach ($result in $tenantResults) {
        $statusLabel = if ($result.Succeeded) { 'OK' } else { 'FAILED' }
        $statusColor = if ($result.Succeeded) { 'Green' } else { 'Red' }
        $startedText = $result.StartedAt.ToString('yyyy-MM-dd HH:mm:ss')
        $finishedText = $result.FinishedAt.ToString('yyyy-MM-dd HH:mm:ss')
        Write-Host ("[{0}] {1,-8} {2} -> {3}  ({4} min)" -f $statusLabel, $result.Tenant, $startedText, $finishedText, $result.ElapsedMinutes) -ForegroundColor $statusColor
    }
    Write-Host ""
    Write-Host "Succeeded: $successCount tenant(s)" -ForegroundColor Green
    if ($successCount -gt 0) {
        Write-Host "  Tenants: $($succeededTenants -join ', ')" -ForegroundColor Gray
    }
    Write-Host "Failed: $failCount tenant(s)" -ForegroundColor $(if ($failCount -gt 0) { 'Red' } else { 'Gray' })
    if ($failCount -gt 0) {
        Write-Host "  Tenants: $($failedTenants -join ', ')" -ForegroundColor Red
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
