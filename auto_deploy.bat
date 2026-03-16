@echo off
chcp 65001 >nul
echo ============================================
echo FrontCRM 前端自动部署
echo ============================================
echo.

set SERVER=129.226.161.3
set USER=ubuntu
set PASS=xl@Front#1729
set LOCAL_PATH=d:\MyProject\FrontCRM_CSharp\CRM.Web\dist
set REMOTE_PATH=/home/ubuntu/frontcrm_deploy/CRM.Web/dist

echo 服务器: %SERVER%
echo 用户: %USER%
echo.

echo [1/2] 正在上传前端文件...
echo.

:: 创建 expect 脚本来处理密码
(
echo spawn scp -r -o StrictHostKeyChecking=no "%LOCAL_PATH%\*" %USER%@%SERVER%:%REMOTE_PATH%
echo expect "password:"
echo send "%PASS%\r"
echo expect eof
) > %TEMP%\scp_expect.txt

:: 使用 plink 如果存在
if exist "%PROGRAMFILES%\PuTTY\pscp.exe" (
    echo 使用 PuTTY PSCP 上传...
    "%PROGRAMFILES%\PuTTY\pscp.exe" -r -pw %PASS% -P 22 "%LOCAL_PATH%\*" "%USER%@%SERVER%:%REMOTE_PATH%"
) else (
    echo 使用 OpenSSH 上传，请稍等...
    echo.
    echo 密码: %PASS%
    echo.
    scp -r -o StrictHostKeyChecking=no "%LOCAL_PATH%\*" "%USER%@%SERVER%:%REMOTE_PATH%"
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo 上传可能失败或需要手动输入密码
    echo 密码是: %PASS%
    pause
)

echo.
echo [2/2] 正在重启前端容器...
echo.

:: SSH 登录并重启
(
echo spawn ssh -o StrictHostKeyChecking=no %USER%@%SERVER%
echo expect "password:"
echo send "%PASS%\r"
echo expect "$"
echo send "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend\r"
echo expect "$"
echo send "exit\r"
echo expect eof
) > %TEMP%\ssh_expect.txt

if exist "%PROGRAMFILES%\PuTTY\plink.exe" (
    echo 使用 PuTTY Plink 执行...
    "%PROGRAMFILES%\PuTTY\plink.exe" -ssh -pw %PASS% %USER%@%SERVER% "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend"
) else (
    echo 使用 OpenSSH 连接，请稍等...
    ssh -o StrictHostKeyChecking=no %USER%@%SERVER% "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend"
)

echo.
echo ============================================
echo 部署完成!
echo ============================================
echo.
echo 访问地址: http://%SERVER%
pause
