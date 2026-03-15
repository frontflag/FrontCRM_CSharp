@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo    COMPLETE BUSINESS PROCESS TEST
echo ========================================
echo.

set API_URL=http://localhost:5000/api/v1
set TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=!TIMESTAMP: =0!
set TEST_USER=user_!TIMESTAMP!
set TEST_EMAIL=user_!TIMESTAMP!@test.com
set TEST_PASS=Test123456!
set TOKEN=
set CUSTOMER_ID=
set CONTACT_ID=
set ADDRESS_ID=

echo Test ID: !TIMESTAMP!
echo.

:: Test 1: Register
echo [TEST 1] User Registration
curl -s -X POST "%API_URL%/auth/register" -H "Content-Type: application/json" -d "{\"userName\":\"!TEST_USER!\",\"email\":\"!TEST_EMAIL!\",\"password\":\"!TEST_PASS!\"}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] User registered: !TEST_USER!
    for /f "tokens=* delims=" %%a in ('findstr "token" result.json') do (
        set "line=%%a"
        set "TOKEN=!line:~15,-2!"
    )
) else (
    echo [FAIL] Registration failed
    type result.json
    goto :end
)
echo.

:: Test 2: Login
echo [TEST 2] User Login
curl -s -X POST "%API_URL%/auth/login" -H "Content-Type: application/json" -d "{\"email\":\"!TEST_EMAIL!\",\"password\":\"!TEST_PASS!\"}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Login successful
) else (
    echo [FAIL] Login failed
)
echo.

:: Test 3: Get Current User
echo [TEST 3] Get Current User
curl -s -X GET "%API_URL%/auth/me" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got current user
) else (
    echo [FAIL] Failed to get user
)
echo.

:: Test 4: Create Customer
echo [TEST 4] Create Customer
curl -s -X POST "%API_URL%/customers" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"customerName\":\"Customer !TIMESTAMP!\",\"customerShortName\":\"Cust !TIMESTAMP!\",\"customerLevel\":\"A\",\"customerType\":1,\"customerCode\":\"C!TIMESTAMP!\",\"unifiedSocialCreditCode\":\"91110000MA00!TIMESTAMP!XX\",\"remarks\":\"Test customer\",\"creditLimit\":100000,\"paymentTerms\":\"Net 30\",\"currency\":\"CNY\"}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Customer created
    for /f "tokens=*" %%a in ('findstr "\"id\"" result.json') do (
        set "line=%%a"
        for /f "delims=:" %%b in ("!line!") do set "CUSTOMER_ID=%%c"
        set "CUSTOMER_ID=!CUSTOMER_ID:~2,-2!"
    )
    echo Customer ID: !CUSTOMER_ID!
) else (
    echo [FAIL] Customer creation failed
    type result.json
)
echo.

:: Test 5: Get Customer List
echo [TEST 5] Get Customer List
curl -s -X GET "%API_URL%/customers?pageNumber=1&pageSize=10" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got customer list
) else (
    echo [FAIL] Failed
)
echo.

:: Test 6: Get Customer Detail
echo [TEST 6] Get Customer Detail
curl -s -X GET "%API_URL%/customers/!CUSTOMER_ID!" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got customer detail
) else (
    echo [FAIL] Failed
)
echo.

:: Test 7: Update Customer
echo [TEST 7] Update Customer
curl -s -X PUT "%API_URL%/customers/!CUSTOMER_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"customerName\":\"Customer !TIMESTAMP!\",\"customerShortName\":\"Updated Name\",\"customerLevel\":\"B\",\"remarks\":\"Updated remarks\"}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Customer updated
) else (
    echo [FAIL] Update failed
    type result.json
)
echo.

:: Test 8: Get Statistics
echo [TEST 8] Get Customer Statistics
curl -s -X GET "%API_URL%/customers/statistics" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got statistics
) else (
    echo [FAIL] Failed
)
echo.

:: Test 9: Create Contact
echo [TEST 9] Create Contact
curl -s -X POST "%API_URL%/customers/!CUSTOMER_ID!/contacts" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"name\":\"John Doe\",\"phone\":\"13800138000\",\"email\":\"john@test.com\",\"position\":\"Manager\",\"isDefault\":true}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Contact created
    for /f "tokens=*" %%a in ('findstr "\"id\"" result.json') do (
        set "line=%%a"
        for /f "delims=:" %%b in ("!line!") do set "CONTACT_ID=%%c"
        set "CONTACT_ID=!CONTACT_ID:~2,-2!"
    )
    echo Contact ID: !CONTACT_ID!
) else (
    echo [FAIL] Contact creation failed
    type result.json
)
echo.

:: Test 10: Get Contacts
echo [TEST 10] Get Customer Contacts
curl -s -X GET "%API_URL%/customers/!CUSTOMER_ID!/contacts" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got contacts
) else (
    echo [FAIL] Failed
)
echo.

:: Test 11: Update Contact
echo [TEST 11] Update Contact
if "!CONTACT_ID!"=="" (
    echo [SKIP] No contact ID
) else (
    curl -s -X PUT "%API_URL%/contacts/!CONTACT_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"name\":\"Jane Doe\",\"phone\":\"13900139000\",\"position\":\"Director\"}" > result.json
    findstr "success" result.json | findstr "true" >nul
    if !errorlevel! == 0 (
        echo [PASS] Contact updated
    ) else (
        echo [FAIL] Update failed
    )
)
echo.

:: Test 12: Create Address
echo [TEST 12] Create Address
curl -s -X POST "%API_URL%/customers/!CUSTOMER_ID!/addresses" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"addressType\":1,\"address\":\"100 Test Road, Beijing\",\"contactName\":\"Jane Smith\",\"contactPhone\":\"13900139000\",\"isDefault\":true}" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Address created
    for /f "tokens=*" %%a in ('findstr "\"id\"" result.json') do (
        set "line=%%a"
        for /f "delims=:" %%b in ("!line!") do set "ADDRESS_ID=%%c"
        set "ADDRESS_ID=!ADDRESS_ID:~2,-2!"
    )
    echo Address ID: !ADDRESS_ID!
) else (
    echo [FAIL] Address creation failed
    type result.json
)
echo.

:: Test 13: Get Addresses
echo [TEST 13] Get Customer Addresses
curl -s -X GET "%API_URL%/customers/!CUSTOMER_ID!/addresses" -H "Authorization: Bearer !TOKEN!" > result.json
findstr "success" result.json | findstr "true" >nul
if !errorlevel! == 0 (
    echo [PASS] Got addresses
) else (
    echo [FAIL] Failed
)
echo.

:: Test 14: Update Address
echo [TEST 14] Update Address
if "!ADDRESS_ID!"=="" (
    echo [SKIP] No address ID
) else (
    curl -s -X PUT "%API_URL%/addresses/!ADDRESS_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"address\":\"200 New Road, Shanghai\",\"contactName\":\"Tom Brown\"}" > result.json
    findstr "success" result.json | findstr "true" >nul
    if !errorlevel! == 0 (
        echo [PASS] Address updated
    ) else (
        echo [FAIL] Update failed
    )
)
echo.

:: Database Verification
echo [TEST 15-18] Database Verification
cd TestDbQuery
dotnet run > db_result.txt 2>&1
type db_result.txt
cd ..
echo.

:end
echo ========================================
echo    TEST COMPLETE
echo ========================================

:: Cleanup
del result.json 2>nul
del TestDbQuery\db_result.txt 2>nul

pause
