# FrontCRM 简化部署脚本 (PowerShell)

# 配置变量
$server = "129.226.161.3"
$user = "ubuntu"
$projectPath = "d:\MyProject\FrontCRM_CSharp"
$remotePath = "/home/ubuntu/frontcrm"

Write-Host "🚀 FrontCRM 香港服务器部署" -ForegroundColor Green
Write-Host "================================="

# 1. 上传项目文件
Write-Host "1. 上传项目文件到服务器..." -ForegroundColor Yellow
$password = Read-Host "请输入服务器密码" -AsSecureString
$credential = New-Object System.Management.Automation.PSCredential($user, $password)

# 使用SCP上传（需要安装Posh-SSH模块）
Write-Host "请手动执行以下命令上传文件：" -ForegroundColor Yellow
Write-Host ""
Write-Host "scp -r `"$projectPath`" $user@${server}:/home/ubuntu/" -ForegroundColor Cyan
Write-Host ""
Write-Host "或者使用WinSCP等工具上传" -ForegroundColor Cyan
Write-Host ""

# 2. SSH连接和执行部署
Write-Host "2. 连接到服务器执行部署..." -ForegroundColor Yellow
$deployScript = @"
# 创建项目目录
mkdir -p /home/ubuntu/frontcrm
cd /home/ubuntu/frontcrm

# 如果是压缩上传，解压
# unzip frontcrm.zip

# 解决NuGet包下载问题
cat > nuget.config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="tencent" value="https://mirrors.cloud.tencent.com/nuget/" />
  </packageSources>
</configuration>
EOF

# 构建前端
cd CRM.Web
npm install
npm run build
cd ..

# 启动Docker服务
docker-compose up -d

# 检查状态
echo "等待服务启动..."
sleep 10
docker-compose ps
echo ""
echo "查看日志: docker-compose logs -f"
echo ""
echo "前端访问: http://${server}"
echo "后端API: http://${server}:5000"
"@

Write-Host "部署脚本已准备好：" -ForegroundColor Green
Write-Host ""
Write-Host "$deployScript" -ForegroundColor Cyan
Write-Host ""
Write-Host "请复制以上脚本，然后在SSH连接后执行" -ForegroundColor Yellow

# 3. 提供SSH连接命令
Write-Host ""
Write-Host "3. SSH连接命令：" -ForegroundColor Green
Write-Host "ssh $user@$server" -ForegroundColor Cyan
Write-Host "输入密码后，粘贴上面的部署脚本" -ForegroundColor Yellow

# 4. 验证部署
Write-Host ""
Write-Host "4. 验证部署：" -ForegroundColor Green
Write-Host "打开浏览器访问: http://$server" -ForegroundColor Cyan
Write-Host "测试API: http://${server}:5000/api/health" -ForegroundColor Cyan

Write-Host ""
Write-Host "✅ 部署指南完成！" -ForegroundColor Green