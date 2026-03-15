# Complete API Test
$baseUrl = "http://localhost:5000"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$allTestsPassed = $true

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "       CRM API Complete Test Suite" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Wait for API
$maxRetries = 10
$retry = 0
$apiReady = $false
while ($retry -lt $maxRetries -and -not $apiReady) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "`nAPI is ready on port 5000!" -ForegroundColor Green
        $apiReady = $true
    } catch {
        $retry++
        Write-Host "Waiting for API... attempt $retry/$maxRetries" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

if (-not $apiReady) {
    Write-Host "API failed to start" -ForegroundColor Red
    exit 1
}

# ==================== TEST 1: Create Customer ====================
Write-Host "`n[TEST 1] Create Customer with frontend fields..." -ForegroundColor Yellow

$createData = @{
    customerCode = "TEST$timestamp"
    customerName = "Test Company Ltd"
    customerShortName = "TestCo"
    customerLevel = "VIP"
    customerType = 1
    industry = "Technology"
    salesPersonId = "user-001"
    remarks = "Test customer created via API"
    creditLimit = 50000
    paymentTerms = 30
    currency = 1
    unifiedSocialCreditCode = "91110000123456789X"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $createData -ContentType "application/json" -TimeoutSec 10
    $createdCustomer = $response.data
    $customerId = $createdCustomer.id
    
    Write-Host "  Customer created with ID: $customerId" -ForegroundColor Green
    
    # Verify all fields
    $checks = @(
        @{ Field = "customerName->officialName"; Expected = "Test Company Ltd"; Actual = $createdCustomer.officialName },
        @{ Field = "customerShortName->nickName"; Expected = "TestCo"; Actual = $createdCustomer.nickName },
        @{ Field = "customerLevel->level"; Expected = 5; Actual = $createdCustomer.level },
        @{ Field = "customerLevel string"; Expected = "VIP"; Actual = $createdCustomer.customerLevel },
        @{ Field = "customerType->type"; Expected = 1; Actual = $createdCustomer.type },
        @{ Field = "salesPersonId->salesUserId"; Expected = "user-001"; Actual = $createdCustomer.salesUserId },
        @{ Field = "remarks->remark"; Expected = "Test customer created via API"; Actual = $createdCustomer.remark },
        @{ Field = "creditLimit->creditLine"; Expected = 50000; Actual = $createdCustomer.creditLine },
        @{ Field = "paymentTerms->payment"; Expected = 30; Actual = $createdCustomer.payment },
        @{ Field = "currency->tradeCurrency"; Expected = 1; Actual = $createdCustomer.tradeCurrency },
        @{ Field = "unifiedSocialCreditCode->creditCode"; Expected = "91110000123456789X"; Actual = $createdCustomer.creditCode }
    )
    
    foreach ($check in $checks) {
        if ($check.Actual -eq $check.Expected) {
            Write-Host "    [PASS] $($check.Field)" -ForegroundColor Green
        } else {
            Write-Host "    [FAIL] $($check.Field): got '$($check.Actual)', expected '$($check.Expected)'" -ForegroundColor Red
            $allTestsPassed = $false
        }
    }
} catch {
    Write-Host "    [FAIL] Create customer failed: $_" -ForegroundColor Red
    $allTestsPassed = $false
}

# ==================== TEST 2: Get Customer Detail ====================
Write-Host "`n[TEST 2] Get Customer Detail..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$customerId" -TimeoutSec 10
    $customer = $response.data
    
    if ($customer.officialName -eq "Test Company Ltd") {
        Write-Host "    [PASS] Retrieved customer correctly" -ForegroundColor Green
    } else {
        Write-Host "    [FAIL] Customer data mismatch" -ForegroundColor Red
        $allTestsPassed = $false
    }
} catch {
    Write-Host "    [FAIL] Get customer failed: $_" -ForegroundColor Red
    $allTestsPassed = $false
}

# ==================== TEST 3: Update Customer ====================
Write-Host "`n[TEST 3] Update Customer..." -ForegroundColor Yellow

$updateData = @{
    customerName = "Updated Company Ltd"
    customerShortName = "UpdatedCo"
    customerLevel = "BPO"
    remarks = "Updated via API test"
    creditLimit = 100000
    paymentTerms = 60
    currency = 2
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$customerId" -Method PUT -Body $updateData -ContentType "application/json" -TimeoutSec 10
    $updatedCustomer = $response.data
    
    $updateChecks = @(
        @{ Field = "customerName"; Expected = "Updated Company Ltd"; Actual = $updatedCustomer.officialName },
        @{ Field = "customerShortName"; Expected = "UpdatedCo"; Actual = $updatedCustomer.nickName },
        @{ Field = "customerLevel->level"; Expected = 4; Actual = $updatedCustomer.level },
        @{ Field = "customerLevel string"; Expected = "BPO"; Actual = $updatedCustomer.customerLevel },
        @{ Field = "remarks"; Expected = "Updated via API test"; Actual = $updatedCustomer.remark },
        @{ Field = "creditLimit"; Expected = 100000; Actual = $updatedCustomer.creditLine },
        @{ Field = "paymentTerms"; Expected = 60; Actual = $updatedCustomer.payment },
        @{ Field = "currency"; Expected = 2; Actual = $updatedCustomer.tradeCurrency }
    )
    
    foreach ($check in $updateChecks) {
        if ($check.Actual -eq $check.Expected) {
            Write-Host "    [PASS] Update $($check.Field)" -ForegroundColor Green
        } else {
            Write-Host "    [FAIL] Update $($check.Field): got '$($check.Actual)', expected '$($check.Expected)'" -ForegroundColor Red
            $allTestsPassed = $false
        }
    }
} catch {
    Write-Host "    [FAIL] Update customer failed: $_" -ForegroundColor Red
    $allTestsPassed = $false
}

# ==================== TEST 4: Get Customer List ====================
Write-Host "`n[TEST 4] Get Customer List..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -TimeoutSec 10
    $list = $response.data
    
    Write-Host "    [PASS] Retrieved $($list.totalCount) customers" -ForegroundColor Green
    
    if ($list.items.Count -gt 0) {
        $first = $list.items[0]
        Write-Host "    First customer: $($first.customerCode) - $($first.officialName)" -ForegroundColor Gray
    }
} catch {
    Write-Host "    [FAIL] Get list failed: $_" -ForegroundColor Red
    $allTestsPassed = $false
}

# ==================== TEST 5: Verify Aliases in Response ====================
Write-Host "`n[TEST 5] Verify Response Aliases..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$customerId" -TimeoutSec 10
    $customer = $response.data
    
    # Check that aliases match main fields
    $aliasChecks = @(
        @{ Alias = "customerName"; Main = "officialName" },
        @{ Alias = "customerShortName"; Main = "nickName" },
        @{ Alias = "salesPersonId"; Main = "salesUserId" },
        @{ Alias = "remarks"; Main = "remark" },
        @{ Alias = "creditLimit"; Main = "creditLine" },
        @{ Alias = "paymentTerms"; Main = "payment" },
        @{ Alias = "currency"; Main = "tradeCurrency" },
        @{ Alias = "unifiedSocialCreditCode"; Main = "creditCode" }
    )
    
    foreach ($check in $aliasChecks) {
        $aliasValue = $customer.($check.Alias)
        $mainValue = $customer.($check.Main)
        if ($aliasValue -eq $mainValue) {
            Write-Host "    [PASS] $($check.Alias) == $($check.Main)" -ForegroundColor Green
        } else {
            Write-Host "    [WARN] $($check.Alias)('$aliasValue') != $($check.Main)('$mainValue')" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "    [FAIL] Verify aliases failed: $_" -ForegroundColor Red
}

# ==================== SUMMARY ====================
Write-Host "`n==============================================" -ForegroundColor Cyan
if ($allTestsPassed) {
    Write-Host "    ALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "    SOME TESTS FAILED!" -ForegroundColor Red
}
Write-Host "==============================================" -ForegroundColor Cyan

if ($allTestsPassed) { exit 0 } else { exit 1 }
