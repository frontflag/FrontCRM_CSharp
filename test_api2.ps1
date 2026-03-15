# API Test Script - Detailed Test
$baseUrl = "http://localhost:5000"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "========== API Detailed Test Start =========="

# Test with different field names to verify mapping
Write-Host "`n[TEST] Create Customer with PascalCase fields..."
try {
    $newCustomer = @{
        CustomerCode = "TEST$timestamp"
        OfficialName = "PascalCase Company"
        NickName = "Pascal Nick"
        Level = 3
        Type = 1
        Industry = "Technology"
        SalesUserId = "user-001"
        Remark = "Test with PascalCase"
    } | ConvertTo-Json
    
    Write-Host "Request Body: $newCustomer"
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    Write-Host "PASS: Create Customer with PascalCase OK" -ForegroundColor Green
    Write-Host "Response:"
    $response | ConvertTo-Json -Depth 3 | Write-Host
    $global:testId1 = $response.data.id
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

Write-Host "`n[TEST] Create Customer with camelCase fields..."
try {
    $newCustomer = @{
        customerCode = "TEST2$timestamp"
        officialName = "camelCase Company"
        nickName = "camel Nick"
        level = 4
        type = 2
        industry = "Finance"
        salesUserId = "user-002"
        remark = "Test with camelCase"
    } | ConvertTo-Json
    
    Write-Host "Request Body: $newCustomer"
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    Write-Host "PASS: Create Customer with camelCase OK" -ForegroundColor Green
    Write-Host "Response:"
    $response | ConvertTo-Json -Depth 3 | Write-Host
    $global:testId2 = $response.data.id
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

# Verify the data was saved correctly
if ($global:testId1) {
    Write-Host "`n[TEST] Verify PascalCase customer saved correctly (ID: $global:testId1)..."
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:testId1" -Method GET -TimeoutSec 10
        Write-Host "PASS: Get Customer OK" -ForegroundColor Green
        $data = $response.data
        Write-Host "officialName from DB: $($data.officialName)"
        Write-Host "customerName (alias): $($data.customerName)"
        Write-Host "nickName from DB: $($data.nickName)"
        Write-Host "customerShortName (alias): $($data.customerShortName)"
    } catch {
        Write-Host "FAIL: $_" -ForegroundColor Red
    }
}

if ($global:testId2) {
    Write-Host "`n[TEST] Verify camelCase customer saved correctly (ID: $global:testId2)..."
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:testId2" -Method GET -TimeoutSec 10
        Write-Host "PASS: Get Customer OK" -ForegroundColor Green
        $data = $response.data
        Write-Host "officialName from DB: $($data.officialName)"
        Write-Host "customerName (alias): $($data.customerName)"
        Write-Host "nickName from DB: $($data.nickName)"
        Write-Host "customerShortName (alias): $($data.customerShortName)"
    } catch {
        Write-Host "FAIL: $_" -ForegroundColor Red
    }
}

Write-Host "`n========== API Detailed Test Complete =========="
