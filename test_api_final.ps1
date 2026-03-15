# Final API Test
$baseUrl = "http://localhost:5000"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "========== Final API Test Start =========="

# Wait for API to be ready
$maxRetries = 10
$retry = 0
$apiReady = $false

while ($retry -lt $maxRetries -and -not $apiReady) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "API is ready!" -ForegroundColor Green
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

# Test with frontend field names
Write-Host "`n[TEST] Create Customer with frontend field names..." -ForegroundColor Cyan
try {
    $newCustomer = @{
        customerCode = "FE$timestamp"
        customerName = "Frontend Company"
        customerShortName = "Frontend Short"
        customerLevel = "VIP"
        customerType = 1
        industry = "Technology"
        salesPersonId = "user-001"
        remarks = "Test with frontend fields"
        creditLimit = 10000
        paymentTerms = 60
        currency = 2
    } | ConvertTo-Json
    
    Write-Host "Request: $newCustomer"
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    
    Write-Host "`nPASS: Create Customer OK" -ForegroundColor Green
    $data = $response.data
    
    Write-Host "`nField Mapping Results:"
    Write-Host "  customerName -> officialName: '$($data.officialName)'" -ForegroundColor $(if ($data.officialName -eq "Frontend Company") { "Green" } else { "Red" })
    Write-Host "  customerShortName -> nickName: '$($data.nickName)'" -ForegroundColor $(if ($data.nickName -eq "Frontend Short") { "Green" } else { "Red" })
    Write-Host "  customerLevel -> level: $($data.level)" -ForegroundColor $(if ($data.level -eq 5) { "Green" } else { "Red" })
    Write-Host "  customerLevel (string): '$($data.customerLevel)'" -ForegroundColor $(if ($data.customerLevel -eq "VIP") { "Green" } else { "Red" })
    Write-Host "  customerType -> type: $($data.type)" -ForegroundColor $(if ($data.type -eq 1) { "Green" } else { "Red" })
    Write-Host "  salesPersonId -> salesUserId: '$($data.salesUserId)'" -ForegroundColor $(if ($data.salesUserId -eq "user-001") { "Green" } else { "Red" })
    Write-Host "  remarks -> remark: '$($data.remark)'" -ForegroundColor $(if ($data.remark -eq "Test with frontend fields") { "Green" } else { "Red" })
    Write-Host "  creditLimit -> creditLine: $($data.creditLine)" -ForegroundColor $(if ($data.creditLine -eq 10000) { "Green" } else { "Red" })
    Write-Host "  paymentTerms -> payment: $($data.payment)" -ForegroundColor $(if ($data.payment -eq 60) { "Green" } else { "Red" })
    Write-Host "  currency -> tradeCurrency: $($data.tradeCurrency)" -ForegroundColor $(if ($data.tradeCurrency -eq 2) { "Green" } else { "Red" })
    
    $global:testId = $data.id
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

# Test Get Customer Detail
if ($global:testId) {
    Write-Host "`n[TEST] Get Customer Detail..." -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:testId" -TimeoutSec 10
        $data = $response.data
        
        Write-Host "PASS: Get Customer OK" -ForegroundColor Green
        Write-Host "  officialName from DB: '$($data.officialName)'"
        Write-Host "  customerName (alias): '$($data.customerName)'"
        Write-Host "  level from DB: $($data.level)"
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

Write-Host "`n========== API Test Complete =========="
