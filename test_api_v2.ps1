# API Test V2
$baseUrl = "http://localhost:5001"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "========== API Test V2 Start ==========" -ForegroundColor Cyan

# Wait for API
$maxRetries = 10
$retry = 0
$apiReady = $false
while ($retry -lt $maxRetries -and -not $apiReady) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "API is ready on port 5001!" -ForegroundColor Green
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

# Test with ALL frontend field names
Write-Host "`n[TEST] Create Customer with ALL frontend field names..." -ForegroundColor Cyan
try {
    $newCustomer = @{
        customerCode = "V2$timestamp"
        customerName = "V2 Frontend Company"
        customerShortName = "V2 Short"
        customerLevel = "VIP"
        customerType = 1
        industry = "Technology"
        salesPersonId = "user-001"
        remarks = "V2 Test with all fields"
        creditLimit = 10000
        paymentTerms = 60
        currency = 2
        unifiedSocialCreditCode = "91110000123456789X"
    } | ConvertTo-Json
    
    Write-Host "Request Body: $newCustomer"
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    
    Write-Host "`nPASS: Create Customer OK" -ForegroundColor Green
    $data = $response.data
    
    Write-Host "`n========== Field Mapping Verification ==========" -ForegroundColor Yellow
    
    $allPass = $true
    
    # Test customerName
    if ($data.officialName -eq "V2 Frontend Company") {
        Write-Host "[PASS] customerName -> officialName: '$($data.officialName)'" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] customerName -> officialName: '$($data.officialName)' (expected: 'V2 Frontend Company')" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test customerShortName
    if ($data.nickName -eq "V2 Short") {
        Write-Host "[PASS] customerShortName -> nickName: '$($data.nickName)'" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] customerShortName -> nickName: '$($data.nickName)' (expected: 'V2 Short')" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test customerLevel
    if ($data.level -eq 5 -and $data.customerLevel -eq "VIP") {
        Write-Host "[PASS] customerLevel -> level: $($data.level) (string: '$($data.customerLevel)')" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] customerLevel -> level: $($data.level) (expected: 5/VIP)" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test customerType
    if ($data.type -eq 1) {
        Write-Host "[PASS] customerType -> type: $($data.type)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] customerType -> type: $($data.type) (expected: 1)" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test salesPersonId
    if ($data.salesUserId -eq "user-001") {
        Write-Host "[PASS] salesPersonId -> salesUserId: '$($data.salesUserId)'" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] salesPersonId -> salesUserId: '$($data.salesUserId)' (expected: 'user-001')" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test remarks
    if ($data.remark -eq "V2 Test with all fields") {
        Write-Host "[PASS] remarks -> remark: '$($data.remark)'" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] remarks -> remark: '$($data.remark)' (expected: 'V2 Test with all fields')" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test creditLimit
    if ($data.creditLine -eq 10000) {
        Write-Host "[PASS] creditLimit -> creditLine: $($data.creditLine)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] creditLimit -> creditLine: $($data.creditLine) (expected: 10000)" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test paymentTerms
    if ($data.payment -eq 60) {
        Write-Host "[PASS] paymentTerms -> payment: $($data.payment)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] paymentTerms -> payment: $($data.payment) (expected: 60)" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test currency
    if ($data.tradeCurrency -eq 2) {
        Write-Host "[PASS] currency -> tradeCurrency: $($data.tradeCurrency)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] currency -> tradeCurrency: $($data.tradeCurrency) (expected: 2)" -ForegroundColor Red
        $allPass = $false
    }
    
    # Test unifiedSocialCreditCode
    if ($data.creditCode -eq "91110000123456789X") {
        Write-Host "[PASS] unifiedSocialCreditCode -> creditCode: '$($data.creditCode)'" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] unifiedSocialCreditCode -> creditCode: '$($data.creditCode)' (expected: '91110000123456789X')" -ForegroundColor Red
        $allPass = $false
    }
    
    $global:testId = $data.id
    
    Write-Host "`n========== Summary ==========" -ForegroundColor Yellow
    if ($allPass) {
        Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
    } else {
        Write-Host "SOME TESTS FAILED!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

# Verify Get Customer Detail
if ($global:testId) {
    Write-Host "`n[TEST] Verify Get Customer Detail..." -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:testId" -TimeoutSec 10
        $data = $response.data
        
        Write-Host "PASS: Get Customer OK" -ForegroundColor Green
        Write-Host "  officialName: '$($data.officialName)'"
        Write-Host "  customerName (alias): '$($data.customerName)'"
        Write-Host "  level: $($data.level)"
        Write-Host "  customerLevel (alias): '$($data.customerLevel)'"
    } catch {
        Write-Host "FAIL: $_" -ForegroundColor Red
    }
}

# Test Customer List
Write-Host "`n[TEST] Get Customer List..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -TimeoutSec 10
    Write-Host "PASS: Get Customer List OK" -ForegroundColor Green
    Write-Host "  Total Count: $($response.data.totalCount)"
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

Write-Host "`n========== API Test V2 Complete ==========" -ForegroundColor Cyan
