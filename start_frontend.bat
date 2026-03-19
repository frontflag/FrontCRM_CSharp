@echo off
cd /d D:\MyProject\FrontCRM_CSharp\CRM.Web
start "Frontend" npm run dev
echo 前端服务启动中...
timeout /t 3
