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
#   .\scripts\deploy-tenant-production.ps1 -Tenant all -SkipBuild
#   .\scripts\deploy-tenant-production.ps1 -Tenant all -SkipBuild -ParallelUpload

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('semicore', 'idesemi', 'fz', 'ecoinf', 'all')]
    [string]$Tenant,

    [switch]$SkipBuild,
    [switch]$ParallelUpload,
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

function Get-BuildModeSuffix {
    param([string]$BuildMode)
    if ([string]::IsNullOrWhiteSpace($BuildMode)) { return 'default' }
    if ($BuildMode -match '^production\.(.+)$') { return $Matches[1] }
    return ($BuildMode -replace '[^\w\-]', '_')
}

function Get-DeployPackageNameForTenant {
    param($TenantConfig)
    $buildMode = if ($TenantConfig.buildMode) { [string]$TenantConfig.buildMode } else { 'production' }
    $suffix = Get-BuildModeSuffix -BuildMode $buildMode
    return "frontcrm_deploy_$suffix"
}

function Resolve-DeploySshKeyPath {
    param([string]$ExplicitPath)
    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        $resolved = (Resolve-Path -LiteralPath $ExplicitPath -ErrorAction SilentlyContinue).Path
        if ($resolved) { return $resolved }
        throw "SshKeyPath not found: $ExplicitPath"
    }

    $candidates = @('id_ed25519', 'id_rsa')
    foreach ($name in $candidates) {
        $candidate = Join-Path $env:USERPROFILE ".ssh\$name"
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    return ''
}

function Get-DeployFullParams {
    param(
        [string]$TargetTenant,
        [object]$TenantConfig,
        [string]$DeployPackageName = '',
        [switch]$ForceSkipBuild,
        [string]$EffectiveSshKeyPath = ''
    )

    $effectiveDeploymentMode = $DeploymentMode
    if ($effectiveDeploymentMode -eq 'auto' -and $TenantConfig.deploymentMode) {
        $effectiveDeploymentMode = [string]$TenantConfig.deploymentMode
    }

    $params = @{
        Tenant                = $TargetTenant
        ServerIP              = [string]$TenantConfig.serverIP
        SshPort               = [int]$TenantConfig.sshPort
        ServerUser            = [string]$TenantConfig.serverUser
        RemoteDeployPath      = [string]$TenantConfig.remoteDeployPath
        NonDockerFrontendRoot = [string]$TenantConfig.nonDockerFrontendRoot
        NonDockerBackendRoot  = [string]$TenantConfig.nonDockerBackendRoot
        DeploymentMode        = $effectiveDeploymentMode
    }

    if ($ForceSkipBuild -or $SkipBuild) { $params.SkipBuild = $true }
    if ($AllowPasswordPrompt) { $params.AllowPasswordPrompt = $true }
    if ($RequestTtyForSudo) { $params.RequestTtyForSudo = $true }
    if ($IncludeLoginPage) { $params.IncludeLoginPage = $true }
    if ($SkipLoginPage) { $params.SkipLoginPage = $true }
    if (-not [string]::IsNullOrWhiteSpace($EffectiveSshKeyPath)) { $params.SshKeyPath = $EffectiveSshKeyPath }
    if (-not [string]::IsNullOrWhiteSpace($DeployPackageName)) { $params.DeployPackageName = $DeployPackageName }

    return $params
}

function Get-DeployLogErrorSummary {
    param([string]$LogFilePath)

    if (-not (Test-Path -LiteralPath $LogFilePath)) { return @() }

    $lines = Get-Content -LiteralPath $LogFilePath -ErrorAction SilentlyContinue |
        Where-Object { $_ -match 'ERROR:|failed|Deploy job state' }

    if (-not $lines) { return @() }
    return @($lines | Select-Object -Last 5)
}

function Get-DeployLogTail {
    param(
        [string]$LogFilePath,
        [int]$LineCount = 1
    )

    if (-not (Test-Path -LiteralPath $LogFilePath)) { return '' }
    $lines = Get-Content -LiteralPath $LogFilePath -Tail $LineCount -ErrorAction SilentlyContinue
    if (-not $lines) { return '' }
    if ($lines -is [string]) { return $lines.Trim() }
    return ($lines[-1]).Trim()
}

function ConvertTo-DeployResultArray {
    param($Value)

    if ($null -eq $Value) { return @() }
    if ($Value -is [System.Collections.Generic.List[object]]) {
        return ,@($Value.ToArray())
    }
    if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string])) {
        return ,@($Value)
    }
    return ,@($Value)
}

function Format-DeployTimestamp {
    param($Value)

    if ($null -eq $Value) { return '-' }
    if ($Value -is [datetime]) { return $Value.ToString('yyyy-MM-dd HH:mm:ss') }
    return [string]$Value
}

function Invoke-SequentialTenantDeploy {
    param(
        [string]$DeployScriptPath,
        [object]$TenantsConfig,
        [string[]]$TenantIds,
        [string]$EffectiveSshKeyPath = ''
    )

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($tenantId in $TenantIds) {
        $cfg = $TenantsConfig.$tenantId
        $packageName = Get-DeployPackageNameForTenant -TenantConfig $cfg
        $deployParams = Get-DeployFullParams -TargetTenant $tenantId -TenantConfig $cfg -DeployPackageName $packageName -ForceSkipBuild -EffectiveSshKeyPath $EffectiveSshKeyPath
        $tenantStartTime = Get-Date

        Write-Host ""
        Write-Host ">>> Deploy tenant: $tenantId ($packageName -> $($cfg.serverUser)@$($cfg.serverIP))" -ForegroundColor Magenta
        & $DeployScriptPath @deployParams
        $tenantExitCode = if ($null -ne $LASTEXITCODE) { [int]$LASTEXITCODE } else { 0 }
        $tenantEndTime = Get-Date
        $tenantSucceeded = ($tenantExitCode -eq 0)

        $results.Add([PSCustomObject]@{
            Tenant         = $tenantId
            Package        = $packageName
            StartedAt      = $tenantStartTime
            FinishedAt     = $tenantEndTime
            ElapsedMinutes = [math]::Round(($tenantEndTime - $tenantStartTime).TotalMinutes, 1)
            ExitCode       = $tenantExitCode
            Succeeded      = $tenantSucceeded
        }) | Out-Null

        if (-not $tenantSucceeded) {
            Write-Host "!!! Tenant '$tenantId' deploy failed (exit $tenantExitCode); continuing..." -ForegroundColor Red
        }
    }

    return ,@($results.ToArray())
}

function Invoke-ParallelTenantDeploy {
    param(
        [string]$DeployScriptPath,
        [object]$TenantsConfig,
        [string[]]$TenantIds,
        [string]$LogRoot,
        [string]$EffectiveSshKeyPath
    )

    $runspacePool = [runspacefactory]::CreateRunspacePool(1, $TenantIds.Count)
    $runspacePool.Open()

    $workItems = New-Object System.Collections.Generic.List[object]
    try {
        foreach ($tenantId in $TenantIds) {
            $cfg = $TenantsConfig.$tenantId
            $packageName = Get-DeployPackageNameForTenant -TenantConfig $cfg
            $deployParams = Get-DeployFullParams -TargetTenant $tenantId -TenantConfig $cfg -DeployPackageName $packageName -ForceSkipBuild -EffectiveSshKeyPath $EffectiveSshKeyPath
            $logFile = Join-Path $LogRoot "$tenantId.log"

            $ps = [powershell]::Create()
            $ps.RunspacePool = $runspacePool
            [void]$ps.AddScript({
                param(
                    [string]$ScriptPath,
                    [hashtable]$Params,
                    [string]$TenantName,
                    [string]$LogPath
                )

                $ErrorActionPreference = 'Continue'
                $exitCode = 1
                try {
                    Start-Transcript -LiteralPath $LogPath -Force | Out-Null
                    & $ScriptPath @Params
                    $exitCode = if ($null -ne $LASTEXITCODE) { [int]$LASTEXITCODE } else { 0 }
                } catch {
                    Write-Host "ERROR: $($_.Exception.Message)"
                    $exitCode = 1
                } finally {
                    try { Stop-Transcript | Out-Null } catch { }
                }

                return @{
                    Tenant   = $TenantName
                    ExitCode = $exitCode
                }
            }).AddArgument($DeployScriptPath).AddArgument($deployParams).AddArgument($tenantId).AddArgument($logFile)

            $workItems.Add([PSCustomObject]@{
                Tenant    = $tenantId
                Package   = $packageName
                LogFile   = $logFile
                StartedAt = Get-Date
                PowerShell = $ps
                Handle    = $ps.BeginInvoke()
            }) | Out-Null

            Write-Host "Started deploy: $tenantId ($packageName -> $($cfg.serverUser)@$($cfg.serverIP))" -ForegroundColor Gray
            Write-Host "  Log: $logFile" -ForegroundColor DarkGray
        }

        $results = New-Object System.Collections.Generic.List[object]
        $pending = @($workItems | Where-Object { -not $_.Handle.IsCompleted })
        $lastProgressAt = [datetime]::MinValue

        while ($pending.Count -gt 0) {
            $now = Get-Date
            if (($now - $lastProgressAt).TotalSeconds -ge 15) {
                Write-Host ""
                Write-Host ("--- Progress ({0:HH:mm:ss}) ---" -f $now) -ForegroundColor DarkCyan
                foreach ($item in $workItems) {
                    $status = if ($item.Handle.IsCompleted) { 'done' } else { 'running' }
                    $tail = Get-DeployLogTail -LogFilePath $item.LogFile
                    if ([string]::IsNullOrWhiteSpace($tail)) {
                        $tail = '(waiting for log output)'
                    }
                    Write-Host ("  [{0}] {1,-8} {2}" -f $status, $item.Tenant, $tail) -ForegroundColor Gray
                }
                $lastProgressAt = $now
            }
            Start-Sleep -Seconds 2
            $pending = @($workItems | Where-Object { -not $_.Handle.IsCompleted })
        }

        foreach ($item in $workItems) {
            $invokeResult = $item.PowerShell.EndInvoke($item.Handle)
            $item.PowerShell.Dispose()

            $payload = $null
            if ($invokeResult -and $invokeResult.Count -gt 0) {
                $payload = $invokeResult[0]
            }

            $exitCode = 1
            if ($payload) {
                $exitCode = [int]$payload.ExitCode
            }

            $output = @()
            if (Test-Path -LiteralPath $item.LogFile) {
                $output = Get-Content -LiteralPath $item.LogFile -ErrorAction SilentlyContinue
            }

            $finishedAt = Get-Date
            $results.Add([PSCustomObject]@{
                Tenant         = $item.Tenant
                Package        = $item.Package
                LogFile        = $item.LogFile
                StartedAt      = $item.StartedAt
                FinishedAt     = $finishedAt
                ElapsedMinutes = [math]::Round(($finishedAt - $item.StartedAt).TotalMinutes, 1)
                ExitCode       = $exitCode
                Succeeded      = ($exitCode -eq 0)
                Output         = $output
                ErrorSummary   = (Get-DeployLogErrorSummary -LogFilePath $item.LogFile)
            }) | Out-Null
        }

        return ,@($results.ToArray())
    }
    finally {
        $runspacePool.Close()
        $runspacePool.Dispose()
    }
}

function Test-BatchDeployPackages {
    param(
        [object]$TenantsConfig,
        [string[]]$TenantIds,
        [string]$RootPath
    )

    $missing = New-Object System.Collections.Generic.List[string]
    $checked = @{}

    foreach ($tenantId in $TenantIds) {
        $cfg = $TenantsConfig.$tenantId
        $packageName = Get-DeployPackageNameForTenant -TenantConfig $cfg
        if ($checked.ContainsKey($packageName)) { continue }
        $checked[$packageName] = $true

        $packagePath = Join-Path $RootPath $packageName
        $indexHtml = Join-Path $packagePath "CRM.Web/dist/index.html"
        $apiDll = Join-Path $packagePath "CRM.API/publish/CRM.API.dll"
        if (-not (Test-Path $indexHtml) -or -not (Test-Path $apiDll)) {
            $missing.Add($packageName) | Out-Null
        }
    }

    return ,$missing
}

Initialize-DeployScriptConsole

$RepoRoot = Split-Path -Parent $PSScriptRoot
$tenantsFile = Join-Path $RepoRoot "deploy\tenants.json"
if (-not (Test-Path $tenantsFile)) {
    throw "tenants.json not found: $tenantsFile"
}

$allTenants = Get-Content $tenantsFile -Raw | ConvertFrom-Json
$deployScript = Join-Path $RepoRoot "deploy_full_to_server.ps1"
if (-not (Test-Path $deployScript)) {
    throw "deploy_full_to_server.ps1 not found: $deployScript"
}

if ($Tenant -eq 'all') {
    $batchTenants = @('semicore', 'idesemi', 'ecoinf', 'fz')
    $failedTenants = New-Object System.Collections.Generic.List[string]
    $succeededTenants = New-Object System.Collections.Generic.List[string]
    $tenantResults = New-Object System.Collections.Generic.List[object]
    $batchStartTime = Get-Date

    Write-Host ""
    Write-Host "=== Deploy ALL tenants (batch build + upload) ===" -ForegroundColor Cyan
    Write-Host "Tenants: $($batchTenants -join ', ')" -ForegroundColor Gray
    Write-Host "Started at: $($batchStartTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
    Write-Host ""

    if ($RequestTtyForSudo) {
        Write-Host "WARN: -RequestTtyForSudo is ignored for -Tenant all (parallel upload is non-interactive)." -ForegroundColor DarkYellow
        Write-Host ""
    }

    if (-not $SkipBuild) {
        $buildScript = Join-Path $RepoRoot "scripts\build-batch-tenants.ps1"
        if (-not (Test-Path $buildScript)) {
            throw "build-batch-tenants.ps1 not found: $buildScript"
        }

        Write-Host ">>> Phase 1/2: batch build (backend once + frontend per buildMode)" -ForegroundColor Cyan
        Write-Host ""
        & $buildScript -BatchTenants $batchTenants
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: batch build failed (exit $LASTEXITCODE)" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        Write-Host ""
    } else {
        Write-Host ">>> Phase 1/2: build skipped (-SkipBuild)" -ForegroundColor Yellow
        Write-Host ""
    }

    $missingPackages = Test-BatchDeployPackages -TenantsConfig $allTenants -TenantIds $batchTenants -RootPath $RepoRoot
    if ($missingPackages.Count -gt 0) {
        Write-Host "ERROR: missing batch deployment package(s): $($missingPackages -join ', ')" -ForegroundColor Red
        Write-Host "Run without -SkipBuild, or execute .\scripts\build-batch-tenants.ps1 first." -ForegroundColor Yellow
        exit 1
    }

    $effectiveSshKeyPath = Resolve-DeploySshKeyPath -ExplicitPath $SshKeyPath
    $useParallelUpload = $ParallelUpload -and -not $AllowPasswordPrompt

    if ($useParallelUpload) {
        Write-Host ">>> Phase 2/2: parallel upload to $($batchTenants.Count) servers (-ParallelUpload)" -ForegroundColor Cyan
    } else {
        Write-Host ">>> Phase 2/2: sequential upload to $($batchTenants.Count) servers" -ForegroundColor Cyan
        if ($ParallelUpload -and $AllowPasswordPrompt) {
            Write-Host "  (password login forces sequential)" -ForegroundColor DarkGray
        }
    }
    Write-Host ""

    if ($useParallelUpload) {
        if ([string]::IsNullOrWhiteSpace($effectiveSshKeyPath)) {
            Write-Host "ERROR: no SSH private key found for parallel deploy." -ForegroundColor Red
            Write-Host "  Add key to $env:USERPROFILE\.ssh\id_ed25519 or id_rsa, or pass -SshKeyPath." -ForegroundColor Yellow
            Write-Host "  Or omit -ParallelUpload to use sequential deploy." -ForegroundColor Yellow
            exit 1
        }
        Write-Host "Using SSH key: $effectiveSshKeyPath" -ForegroundColor Gray
        Write-Host ""

        $logRoot = Join-Path $env:TEMP ("frontcrm_parallel_deploy_" + (Get-Date -Format 'yyyyMMdd_HHmmss'))
        New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
        Write-Host "Parallel deploy logs: $logRoot" -ForegroundColor DarkGray
        Write-Host "Progress updates every 15s. Press Ctrl+C to cancel." -ForegroundColor DarkGray
        Write-Host ""

        $parallelResults = Invoke-ParallelTenantDeploy `
            -DeployScriptPath $deployScript `
            -TenantsConfig $allTenants `
            -TenantIds $batchTenants `
            -LogRoot $logRoot `
            -EffectiveSshKeyPath $effectiveSshKeyPath

        foreach ($result in (ConvertTo-DeployResultArray $parallelResults)) {
            Write-Host ""
            Write-Host "----- Deploy output: $($result.Tenant) ($($result.Package)) -----" -ForegroundColor Cyan
            if ($result.Output) {
                $result.Output | ForEach-Object { Write-Host $_ }
            }
            if (-not $result.Succeeded) {
                if ($result.ErrorSummary) {
                    Write-Host "Likely cause:" -ForegroundColor Yellow
                    $result.ErrorSummary | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
                }
                Write-Host "Full log: $($result.LogFile)" -ForegroundColor DarkYellow
            }

            $tenantResults.Add([PSCustomObject]@{
                Tenant         = $result.Tenant
                Succeeded      = $result.Succeeded
                StartedAt      = $result.StartedAt
                FinishedAt     = $result.FinishedAt
                ElapsedMinutes = $result.ElapsedMinutes
                ExitCode       = $result.ExitCode
                Package        = $result.Package
            }) | Out-Null

            if ($result.Succeeded) {
                $succeededTenants.Add($result.Tenant) | Out-Null
            } else {
                $failedTenants.Add($result.Tenant) | Out-Null
            }
        }
    } else {
        $sequentialResults = Invoke-SequentialTenantDeploy `
            -DeployScriptPath $deployScript `
            -TenantsConfig $allTenants `
            -TenantIds $batchTenants `
            -EffectiveSshKeyPath $effectiveSshKeyPath

        foreach ($result in (ConvertTo-DeployResultArray $sequentialResults)) {
            $tenantResults.Add([PSCustomObject]@{
                Tenant         = $result.Tenant
                Succeeded      = $result.Succeeded
                StartedAt      = $result.StartedAt
                FinishedAt     = $result.FinishedAt
                ElapsedMinutes = $result.ElapsedMinutes
                ExitCode       = $result.ExitCode
                Package        = $result.Package
            }) | Out-Null

            if ($result.Succeeded) {
                $succeededTenants.Add($result.Tenant) | Out-Null
            } else {
                $failedTenants.Add($result.Tenant) | Out-Null
            }
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
    foreach ($result in (ConvertTo-DeployResultArray $tenantResults)) {
        if ($null -eq $result) { continue }
        $statusLabel = if ($result.Succeeded) { 'OK' } else { 'FAILED' }
        $statusColor = if ($result.Succeeded) { 'Green' } else { 'Red' }
        $startedText = Format-DeployTimestamp $result.StartedAt
        $finishedText = Format-DeployTimestamp $result.FinishedAt
        $tenantName = if ($result.Tenant) { [string]$result.Tenant } else { '-' }
        $packageName = if ($result.Package) { [string]$result.Package } else { '-' }
        $elapsed = if ($null -ne $result.ElapsedMinutes) { $result.ElapsedMinutes } else { '-' }
        Write-Host ("[{0}] {1,-8} {2} -> {3}  ({4} min)  [{5}]" -f $statusLabel, $tenantName, $startedText, $finishedText, $elapsed, $packageName) -ForegroundColor $statusColor
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

$cfg = $allTenants.$Tenant
if (-not $cfg) {
    throw "Unknown tenant in tenants.json: $Tenant"
}

Write-Host ""
Write-Host "=== Deploy tenant: $Tenant ($($cfg.displayName)) ===" -ForegroundColor Cyan
Write-Host "Server: $($cfg.serverUser)@$($cfg.serverIP)" -ForegroundColor Gray
Write-Host ""

$packageName = ''
if ($SkipBuild) {
    $batchPackageName = Get-DeployPackageNameForTenant -TenantConfig $cfg
    $batchPackagePath = Join-Path $RepoRoot $batchPackageName
    if (Test-Path (Join-Path $batchPackagePath "CRM.Web/dist/index.html")) {
        $packageName = $batchPackageName
        Write-Host "Using batch package: $packageName" -ForegroundColor Gray
    }
}

$params = Get-DeployFullParams -TargetTenant $Tenant -TenantConfig $cfg -DeployPackageName $packageName -EffectiveSshKeyPath (Resolve-DeploySshKeyPath -ExplicitPath $SshKeyPath)

& $deployScript @params
exit $LASTEXITCODE
