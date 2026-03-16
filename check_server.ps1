# 检查服务器上的前端文件
$server = "129.226.161.3"
$user = "ubuntu"
$pass = "xl@Front#1729"

Write-Host "检查服务器前端文件..." -ForegroundColor Yellow

# 使用 plink 执行命令
$plink = "${env:ProgramFiles}\PuTTY\plink.exe"
if (Test-Path $plink) {
    & $plink -ssh -pw $pass $user@$server "cat /home/ubuntu/frontcrm_deploy/CRM.Web/dist/index.html | head -30"
} else {
    Write-Host "请手动执行:" -ForegroundColor Cyan
    Write-Host "ssh $user@$server 'cat /home/ubuntu/frontcrm_deploy/CRM.Web/dist/index.html | head -30'" -ForegroundColor Gray
}
