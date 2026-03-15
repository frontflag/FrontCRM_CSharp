@echo off
chcp 65001
echo ======================================
echo  启动 CRM Web 前端服务器
echo ======================================
echo.

cd /d "d:\MyProject\FrontCRM_CSharp\CRM.Web"

if not exist "node_modules" (
    echo 正在安装依赖，请稍候...
    call npm install
    if errorlevel 1 (
        echo 安装失败，请检查网络连接
        pause
        exit /b 1
    )
    echo 依赖安装完成！
    echo.
)

echo 正在启动开发服务器...
echo.
call npm run dev

pause
