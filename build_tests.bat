@echo off
cd /d "d:\MyProject\FrontCRM_CSharp"
echo Building test project...
dotnet build tests\CRM.Core.Tests\CRM.Core.Tests.csproj --verbosity normal > build_output.txt 2>&1
echo Build completed with exit code: %ERRORLEVEL%
type build_output.txt
