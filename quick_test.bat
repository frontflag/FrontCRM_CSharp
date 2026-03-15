@echo off
chcp 65001 >nul
echo ========================================
echo Running CRM.Core.Tests
echo ========================================
cd /d "d:\MyProject\FrontCRM_CSharp"
dotnet test tests\CRM.Core.Tests --verbosity normal
echo ========================================
pause
