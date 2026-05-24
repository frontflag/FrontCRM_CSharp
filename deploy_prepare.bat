@echo off
echo =========================================
echo FrontCRM Deployment Preparation
echo =========================================

REM Check if frontend is built
if not exist "CRM.Web\dist\index.html" (
    echo Frontend not built. Building...
    cd CRM.Web
    call npm run build
    if errorlevel 1 (
        echo Frontend build failed!
        exit /b 1
    )
    cd ..
)

REM Check if backend is published
if not exist "CRM.API\publish\CRM.API.dll" (
    echo Backend not published. Publishing...
    cd CRM.API
    call dotnet publish -c Release -o publish
    if errorlevel 1 (
        echo Backend publish failed!
        exit /b 1
    )
    cd ..
)

REM Create deployment directory
if exist frontcrm_deploy rmdir /s /q frontcrm_deploy
mkdir frontcrm_deploy
mkdir frontcrm_deploy\CRM.Web
mkdir frontcrm_deploy\CRM.API

REM Copy frontend files
xcopy /E /Y CRM.Web\dist frontcrm_deploy\CRM.Web\dist\
xcopy /Y CRM.Web\Dockerfile frontcrm_deploy\CRM.Web\
xcopy /Y CRM.Web\nginx.conf frontcrm_deploy\CRM.Web\

REM Copy backend files
xcopy /E /Y CRM.API\publish frontcrm_deploy\CRM.API\publish\

REM Copy configuration files
xcopy /Y docker-compose.yml frontcrm_deploy\
xcopy /Y Dockerfile.backend frontcrm_deploy\
xcopy /Y start_services.sh frontcrm_deploy\

echo =========================================
echo Deployment package created: frontcrm_deploy
echo =========================================
dir frontcrm_deploy