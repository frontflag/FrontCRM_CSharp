# FrontCRM 前端部署脚本
param(
    [string]$ServerIP = "129.226.161.3",
    [string]$Username = "ubuntu",
    [string]$Password = "xl@Front#1729",
    [string]$LocalDistPath = "d:\MyProject\FrontCRM_CSharp\CRM.Web\dist",
    [string]$RemotePath = "/home/ubuntu/frontcrm_deploy/CRM.Web/dist"
)

Write-Host "============================================" -ForegroundColor Green
Write-Host "FrontCRM 前端部署脚本" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "服务器: $ServerIP" -ForegroundColor Cyan
Write-Host "用户: $Username" -ForegroundColor Cyan
Write-Host "本地路径: $LocalDistPath" -ForegroundColor Cyan
Write-Host "远程路径: $RemotePath" -ForegroundColor Cyan
Write-Host ""

# 检查本地dist目录
if (-not (Test-Path $LocalDistPath)) {
    Write-Host "错误: 本地dist目录不存在!" -ForegroundColor Red
    exit 1
}

# 使用 SCP 上传文件
Write-Host "正在上传前端文件到服务器..." -ForegroundColor Yellow
Write-Host "这可能需要几分钟..." -ForegroundColor Gray

$sshCommand = "ssh"
$scpCommand = "scp"

# 创建临时脚本用于自动输入密码
$tempScript = @"
spawn scp -r -o StrictHostKeyChecking=no "$LocalDistPath\*" $Username@$ServerIP`:$RemotePath
expect "password:"
send "$Password\r"
expect eof
"@

# 使用 plink/pscp (PuTTY) 如果可用
$puttyPath = "${env:ProgramFiles}\PuTTY\pscp.exe"
if (Test-Path $puttyPath) {
    Write-Host "使用 PuTTY PSCP..." -ForegroundColor Green
    & $puttyPath -r -pw $Password -P 22 "$LocalDistPath\*" "$Username@${ServerIP}:$RemotePath"
} else {
    # 使用 OpenSSH - 需要手动输入密码
    Write-Host "请手动执行以下命令并输入密码 '$Password':" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "scp -r -o StrictHostKeyChecking=no `"$LocalDistPath\*`" `"$Username@${ServerIP}:$RemotePath`"" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "或者使用 sshpass (如果已安装):" -ForegroundColor Yellow
    Write-Host "sshpass -p '$Password' scp -r -o StrictHostKeyChecking=no `"$LocalDistPath\*`" `"$Username@${ServerIP}:$RemotePath`"" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "上传完成后，请SSH到服务器重启前端容器:" -ForegroundColor Green
Write-Host "ssh $Username@$ServerIP" -ForegroundColor Cyan
Write-Host "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend" -ForegroundColor Cyan
