# 校验部署包内 CRM.Web/dist/help 是否与仓库 help/ 源一致。
# 用于 deploy_full_to_server.ps1 等上传前拦截陈旧帮助文档。
#
# 用法：
#   .\scripts\verify-deploy-help.ps1 -DeployPackagePath D:\MyProject\FrontCRM_CSharp\frontcrm_deploy_semicore
#   .\scripts\verify-deploy-help.ps1 -DeployPackageName frontcrm_deploy_semicore

param(
    [string]$DeployPackagePath = '',
    [string]$DeployPackageName = 'frontcrm_deploy',
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($DeployPackagePath)) {
    $DeployPackagePath = Join-Path $RepoRoot $DeployPackageName
}

$sourceHelp = Join-Path $RepoRoot 'help'
$distHelp = Join-Path $DeployPackagePath 'CRM.Web/dist/help'
$verifyScript = Join-Path $RepoRoot 'scripts/verify-help-dist.mjs'

if (-not (Test-Path $verifyScript)) {
    Write-Host "ERROR: verify-help-dist.mjs not found: $verifyScript" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $sourceHelp)) {
    Write-Host "ERROR: help source not found: $sourceHelp" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $distHelp)) {
    Write-Host "ERROR: deploy package missing dist/help: $distHelp" -ForegroundColor Red
    Write-Host "  Rebuild frontend (npm run build) before deploy, or remove -SkipBuild." -ForegroundColor Yellow
    exit 1
}

Write-Host ">>> Verify help in deploy package: $DeployPackagePath" -ForegroundColor Gray
$verifyOutput = & node $verifyScript --source $sourceHelp --dist $distHelp 2>&1
foreach ($line in @($verifyOutput)) {
    if ($line -is [System.Management.Automation.ErrorRecord]) {
        Write-Host $line.ToString() -ForegroundColor Red
    } else {
        Write-Host $line
    }
}
exit $LASTEXITCODE
