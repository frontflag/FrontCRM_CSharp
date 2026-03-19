@echo off
cd /d D:\MyProject\FrontCRM_CSharp\CRM.API
set ASPNETCORE_URLS=http://localhost:5002
set ASPNETCORE_ENVIRONMENT=Development
start "Backend API" dotnet bin\Debug\net9.0\CRM.API.dll
echo 后端服务启动中，访问 http://localhost:5002
timeout /t 3
