@echo off
chcp 65001 >nul
echo ========================================
echo    Business Process Test
echo ========================================
echo.

set API_URL=http://localhost:5000/api/v1
set TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%

echo Test Timestamp: %TIMESTAMP%
echo.

:: Test 1: User Registration
echo [TEST 1] User Registration
curl -s -X POST "%API_URL%/auth/register" -H "Content-Type: application/json" -d "{\"userName\":\"user_%TIMESTAMP%\",\"email\":\"user_%TIMESTAMP%@test.com\",\"password\":\"Test123456!\"}" > test_result.json
type test_result.json | findstr "success" | findstr "true" >nul
if %errorlevel% == 0 (
    echo [PASS] User registered successfully
) else (
    echo [FAIL] User registration failed
    type test_result.json
)
echo.

:: Test 2: User Login
echo [TEST 2] User Login
curl -s -X POST "%API_URL%/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"user_%TIMESTAMP%@test.com\",\"password\":\"Test123456!\"}" > test_result.json
type test_result.json | findstr "success" | findstr "true" >nul
if %errorlevel% == 0 (
    echo [PASS] User login successful
    for /f "tokens=*" %%a in ('type test_result.json ^| findstr "token"') do set TOKEN_LINE=%%a
) else (
    echo [FAIL] User login failed
)
echo.

:: Test 3: Create Customer
echo [TEST 3] Create Customer
curl -s -X POST "%API_URL%/customers" -H "Content-Type: application/json" -H "Authorization: Bearer %TOKEN_LINE:~15,-2%" -d "{\"customerName\":\"Customer %TIMESTAMP%\",\"customerShortName\":\"Cust %TIMESTAMP%\",\"customerLevel\":\"A\",\"customerType\":1,\"customerCode\":\"C%TIMESTAMP%\",\"unifiedSocialCreditCode\":\"91110000MA00%TIMESTAMP%XX\"}" > test_result.json
type test_result.json | findstr "success" | findstr "true" >nul
if %errorlevel% == 0 (
    echo [PASS] Customer created successfully
) else (
    echo [FAIL] Customer creation failed
    type test_result.json
)
echo.

:: Test 4: Get Customer List
echo [TEST 4] Get Customer List
curl -s -X GET "%API_URL%/customers?pageNumber=1&pageSize=10" -H "Authorization: Bearer %TOKEN_LINE:~15,-2%" > test_result.json
type test_result.json | findstr "success" | findstr "true" >nul
if %errorlevel% == 0 (
    echo [PASS] Customer list retrieved
) else (
    echo [FAIL] Failed to get customer list
)
echo.

:: Test 5: Database Verification
echo [TEST 5] Database Verification
cd TestDbQuery
dotnet run -- "SELECT COUNT(*) as cnt FROM ""user"" WHERE ""UserName"" LIKE 'user_%'" > db_result.txt
type db_result.txt | findstr "cnt:" >nul
if %errorlevel% == 0 (
    echo [PASS] Database query executed
    type db_result.txt
) else (
    echo [INFO] Check database manually
)
cd ..
echo.

echo ========================================
echo    Test Complete
echo ========================================

:: Cleanup
del test_result.json 2>nul
del TestDbQuery\db_result.txt 2>nul
