# ============================================
# FrontCRM 微信端发布脚本
# 同时构建 H5 和微信小程序
# ============================================

param(
    [string]$Target = "all",    # all | h5 | mp-weixin
    [string]$Server = "43.129.212.65",
    [string]$ServerUser = "root",
    [string]$DeployPath = "/var/www/wechat"
)

$WeChatDir = "$PSScriptRoot\CRM.WeChat"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FrontCRM 微信端构建发布" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查依赖
if (-not (Test-Path "$WeChatDir\node_modules")) {
    Write-Host "[1/3] 安装依赖..." -ForegroundColor Yellow
    Set-Location $WeChatDir
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 依赖安装失败" -ForegroundColor Red
        exit 1
    }
}

# 构建 H5
if ($Target -eq "all" -or $Target -eq "h5") {
    Write-Host "[2/3] 构建 H5 公众号版本..." -ForegroundColor Yellow
    Set-Location $WeChatDir
    npm run build:h5
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ H5 构建失败" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ H5 构建完成 → dist/build/h5/" -ForegroundColor Green

    # 上传到服务器
    $H5Dist = "$WeChatDir\dist\build\h5"
    Write-Host "[3/3] 上传 H5 到服务器 $Server ..." -ForegroundColor Yellow
    Write-Host "  scp -r $H5Dist\* ${ServerUser}@${Server}:${DeployPath}/"
    Write-Host "  (请手动执行以上命令，或配置 SSH 密钥后自动上传)" -ForegroundColor DarkYellow
}

# 构建微信小程序
if ($Target -eq "all" -or $Target -eq "mp-weixin") {
    Write-Host "[2/3] 构建微信小程序版本..." -ForegroundColor Yellow
    Set-Location $WeChatDir
    npm run build:mp-weixin
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 微信小程序构建失败" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ 微信小程序构建完成 → dist/build/mp-weixin/" -ForegroundColor Green
    Write-Host ""
    Write-Host "后续步骤：" -ForegroundColor Cyan
    Write-Host "  1. 打开微信开发者工具" -ForegroundColor White
    Write-Host "  2. 导入项目 → 选择 $WeChatDir\dist\build\mp-weixin\" -ForegroundColor White
    Write-Host "  3. 填写 AppID → 上传代码 → 提交审核" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  构建完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
