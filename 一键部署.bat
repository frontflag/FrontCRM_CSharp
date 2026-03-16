@echo off
echo ============================================
echo FrontCRM 前端部署 - 129.226.161.3
echo ============================================
echo.
echo 密码: xl@Front#1729
echo.

set "PASS=xl@Front#1729"
set "SERVER=129.226.161.3"
set "USER=ubuntu"
set "LOCAL=d:\MyProject\FrontCRM_CSharp\CRM.Web\dist"
set "REMOTE=/home/ubuntu/frontcrm_deploy/CRM.Web/dist"

echo [1/2] 正在上传前端文件...
scp -r -o StrictHostKeyChecking=no "%LOCAL%\*" "%USER%@%SERVER%:%REMOTE%"
if errorlevel 1 (
    echo 上传失败，请检查密码
    pause
    exit /b 1
)

echo.
echo [2/2] 重启前端容器...
ssh -o StrictHostKeyChecking=no "%USER%@%SERVER%" "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend"

echo.
echo ============================================
echo 部署完成!
echo ============================================
echo 访问: http://%SERVER%
pause
