# API Test Script
$baseUrl = "http://localhost:5000"

Write-Host "========== API Interface Test Start =========="

# 1. Test Customer List API
Write-Host "`n[TEST 1] Get Customer List..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET -TimeoutSec 10
    Write-Host "PASS: Customer List API OK" -ForegroundColor Green
    Write-Host "Response Data:"
    $response | ConvertTo-Json -Depth 5 | Write-Host
} catch {
    Write-Host "FAIL: Customer List API Error: $_" -ForegroundColor Red
}

# 2. Test Create Customer API
Write-Host "`n[TEST 2] Create New Customer..."
try {
    $newCustomer = @{
        customerCode = "TEST001"
        officialName = "Test Company"
        nickName = "Test Customer"
        level = 3
        type = 1
        industry = "Technology"
        salesUserId = "user-001"
        remark = "API Test"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    Write-Host "PASS: Create Customer API OK" -ForegroundColor Green
    Write-Host "Response Data:"
    $response | ConvertTo-Json -Depth 5 | Write-Host
    $global:createdCustomerId = $response.id
} catch {
    Write-Host "FAIL: Create Customer API Error: $_" -ForegroundColor Red
}

# 3. Test Get Customer Detail API
if ($global:createdCustomerId) {
    Write-Host "`n[TEST 3] Get Customer Detail (ID: $global:createdCustomerId)..."
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:createdCustomerId" -Method GET -TimeoutSec 10
        Write-Host "PASS: Get Customer Detail API OK" -ForegroundColor Green
        Write-Host "Response Data:"
        $response | ConvertTo-Json -Depth 5 | Write-Host
    } catch {
        Write-Host "FAIL: Get Customer Detail API Error: $_" -ForegroundColor Red
    }
}

# 4. Test Customer List Again
Write-Host "`n[TEST 4] Get Customer List Again..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET -TimeoutSec 10
    Write-Host "PASS: Customer List API OK" -ForegroundColor Green
    $items = $response.items
    $total = $response.totalCount
    Write-Host "Total Customers: $total"
    if ($items -and $items.Count -gt 0) {
        Write-Host "First Customer Data:"
        $items[0] | Select-Object id, customerCode, officialName, customerName, nickName, customerShortName, level, customerLevel, industry | ConvertTo-Json | Write-Host
    }
} catch {
    Write-Host "FAIL: Customer List API Error: $_" -ForegroundColor Red
}

# 5. Test Update Customer API
if ($global:createdCustomerId) {
    Write-Host "`n[TEST 5] Update Customer..."
    try {
        $updateData = @{
            officialName = "Test Company - Updated"
            nickName = "Test Customer - Updated"
            industry = "Finance"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:createdCustomerId" -Method PUT -Body $updateData -ContentType "application/json" -TimeoutSec 10
        Write-Host "PASS: Update Customer API OK" -ForegroundColor Green
        Write-Host "Response Data:"
        $response | ConvertTo-Json -Depth 5 | Write-Host
    } catch {
        Write-Host "FAIL: Update Customer API Error: $_" -ForegroundColor Red
    }
}

Write-Host "`n========== API Interface Test Complete =========="
