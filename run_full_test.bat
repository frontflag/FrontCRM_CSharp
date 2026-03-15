@echo off
chcp 65001 >nul
echo ========================================
echo    CRM 全业务流程测试
echo ========================================
echo.

:: 1. 停止所有dotnet进程
echo [1] 停止所有dotnet进程...
taskkill /F /IM dotnet.exe 2>nul
timeout /t 3 /nobreak >nul

:: 2. 启动API服务
echo [2] 启动API服务...
start "CRM.API" /MIN cmd /c "cd /d d:\MyProject\FrontCRM_CSharp\CRM.API && dotnet run --urls http://localhost:5030 > ..\api_test.log 2>&1"
timeout /t 12 /nobreak >nul

:: 3. 检查API是否启动
echo [3] 检查API状态...
curl -s http://localhost:5030/api/v1/health >nul 2>&1
if errorlevel 1 (
    echo API启动失败，查看日志...
    type ..\api_test.log
    exit /b 1
)
echo API启动成功!

:: 4. 运行测试
echo [4] 运行全业务流程测试...
cd /d d:\MyProject\FrontCRM_CSharp
dotnet run --project TestFullFlow/TestFullFlow.csproj --verbosity quiet

:: 5. 验证数据库
echo.
echo [5] 验证数据库写入结果...
cd /d d:\MyProject\FrontCRM_CSharp\TestDbQuery
dotnet run --verbosity quiet

:: 6. 清理
echo.
echo [6] 测试完成，停止API服务...
taskkill /F /IM dotnet.exe 2>nul

echo.
echo ========================================
echo    全业务流程测试执行完毕
echo ========================================
