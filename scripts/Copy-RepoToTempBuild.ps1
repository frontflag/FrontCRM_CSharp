# 将仓库拷到临时构建目录。跳过 .git、本地产物、Uploads、bin/obj 等，避免 Copy-Item 在海量小文件上假死。
function Copy-RepoToTempBuild {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$DestinationRoot
    )

    if (-not (Test-Path -LiteralPath $SourceRoot)) {
        throw "Source root not found: $SourceRoot"
    }
    if (-not (Test-Path -LiteralPath $DestinationRoot)) {
        New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
    }

    $skipExact = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    @(
        '.git', '.vs', '.cursor', '.codebuddy', 'data', 'Logs',
        'artifacts', 'build_verify', 'CheckUsers', 'publish',
        'deploy_tmp_fix', 'TempHash'
    ) | ForEach-Object { [void]$skipExact.Add($_) }

    # 仅按目录名排除时，robocopy 会跳过树中所有同名目录。
    # 禁止 /XD bin：会误删 node_modules\vue-tsc\bin 等包内目录。
    # Logs/data/Uploads 只能用完整路径：源码有 CRM.Core\Models\Logs。
    $excludeDirs = New-Object System.Collections.Generic.List[string]
    foreach ($name in @('.git', '.vs', '.cursor', '.codebuddy')) {
        $excludeDirs.Add($name) | Out-Null
    }

    $apiUploads = Join-Path $SourceRoot 'CRM.API\Uploads'
    if (Test-Path -LiteralPath $apiUploads) {
        Write-Host "  skip CRM.API\Uploads" -ForegroundColor DarkGray
        $excludeDirs.Add($apiUploads) | Out-Null
    }

    foreach ($projDir in Get-ChildItem -LiteralPath $SourceRoot -Directory -Force) {
        if ($projDir.Name -eq 'node_modules') { continue }
        foreach ($leaf in @('bin', 'obj')) {
            $p = Join-Path $projDir.FullName $leaf
            if (Test-Path -LiteralPath $p) {
                $excludeDirs.Add($p) | Out-Null
            }
        }
        Get-ChildItem -LiteralPath $projDir.FullName -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            if ($_.Name -eq 'node_modules') { return }
            foreach ($leaf in @('bin', 'obj')) {
                $p = Join-Path $_.FullName $leaf
                if (Test-Path -LiteralPath $p) {
                    $excludeDirs.Add($p) | Out-Null
                }
            }
        }
    }

    foreach ($dir in Get-ChildItem -LiteralPath $SourceRoot -Directory -Force) {
        $n = $dir.Name
        $skip = $skipExact.Contains($n) -or
            ($n -like 'frontcrm_deploy*') -or
            ($n -like '_tmp_*') -or
            ($n -like '_build_*') -or
            ($n -like '_out_*') -or
            ($n -like '_verify_*') -or
            ($n -like '.tmp_build*') -or
            ($n -like 'build_output*')
        if ($skip) {
            Write-Host "  skip $n" -ForegroundColor DarkGray
            $excludeDirs.Add($dir.FullName) | Out-Null
        }
    }

    $rootNodeModules = Join-Path $SourceRoot 'node_modules'
    if (Test-Path -LiteralPath $rootNodeModules) {
        Write-Host "  skip node_modules (repo root)" -ForegroundColor DarkGray
        $excludeDirs.Add($rootNodeModules) | Out-Null
    }

    Write-Host "  robocopy (keeps CRM.Web\node_modules; skips .git/_tmp_*/Uploads and project bin/obj)..." -ForegroundColor Gray
    $robocopyArgs = @(
        $SourceRoot, $DestinationRoot,
        '/E', '/NFL', '/NDL', '/NJH', '/NJS', '/NC', '/NS', '/NP', '/R:1', '/W:1',
        '/XD'
    ) + @($excludeDirs)
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & robocopy @robocopyArgs
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prevEap
    if ($code -ge 8) {
        throw "robocopy failed with exit code $code"
    }
    $global:LASTEXITCODE = 0
}
