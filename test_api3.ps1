# API Test Script - Frontend Field Names Test
$baseUrl = "http://localhost:5000"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "========== Frontend Field Names Test Start =========="

# Test with frontend field names (customerName instead of officialName)
Write-Host "`n[TEST] Create Customer with frontend field names (customerName, customerShortName)..."
try {
    $newCustomer = @{
        customerCode = "FE$timestamp"
        customerName = "Frontend Field Company"
        customerShortName = "Frontend Short"
        customerLevel = "VIP"
        customerType = 1
        industry = "Technology"
        salesPersonId = "user-001"
        remarks = "Test with frontend field names"
        creditLimit = 10000
        paymentTerms = 60
        currency = 2
    } | ConvertTo-Json
    
    Write-Host "Request Body: $newCustomer"
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
    Write-Host "PASS: Create Customer with frontend fields OK" -ForegroundColor Green
    
    $data = $response.data
    Write-Host "`nField Mapping Verification:"
    Write-Host "  Input customerName -> officialName: $($data.officialName)"
    Write-Host "  customerName (alias): $($data.customerName)"
    Write-Host "  Input customerShortName -> nickName: $($data.nickName)"
    Write-Host "  customerShortName (alias): $($data.customerShortName)"
    Write-Host "  Input customerLevel -> level: $($data.level)"
    Write-Host "  customerLevel (alias): $($data.customerLevel)"
    Write-Host "  Input customerType -> type: $($data.type)"
    Write-Host "  customerType (alias): $($data.customerType)"
    Write-Host "  Input salesPersonId -> salesUserId: $($data.salesUserId)"
    Write-Host "  salesPersonId (alias): $($data.salesPersonId)"
    Write-Host "  Input remarks -> remark: $($data.remark)"
    Write-Host "  remarks (alias): $($data.remarks)"
    Write-Host "  Input creditLimit -> creditLine: $($data.creditLine)"
    Write-Host "  creditLimit (alias): $($data.creditLimit)"
    Write-Host "  Input paymentTerms -> payment: $($data.payment)"
    Write-Host "  paymentTerms (alias): $($data.paymentTerms)"
    Write-Host "  Input currency -> tradeCurrency: $($data.tradeCurrency)"
    Write-Host "  currency (alias): $($data.currency)"
    
    $global:testId = $data.id
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

# Verify by getting the customer
if ($global:testId) {
    Write-Host "`n[TEST] Verify saved customer by ID ($global:testId)..."
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$global:testId" -Method GET -TimeoutSec 10
        Write-Host "PASS: Get Customer OK" -ForegroundColor Green
        $data = $response.data
        
        Write-Host "`nVerify all fields returned correctly:"
        Write-Host "  officialName: $($data.officialName)"
        Write-Host "  customerName (should be same): $($data.customerName)"
        Write-Host "  nickName: $($data.nickName)"
        Write-Host "  customerShortName (should be same): $($data.customerShortName)"
        Write-Host "  level: $($data.level)"
        Write-Host "  customerLevel (should be mapped): $($data.customerLevel)"
        Write-Host "  industry: $($data.industry)"
        Write-Host "  creditLine: $($data.creditLine)"
        Write-Host "  creditLimit (should be same): $($data.creditLimit)"
        Write-Host "  salesUserId: $($data.salesUserId)"
        Write-Host "  salesPersonId (should be same): $($data.salesPersonId)"
    } catch {
        Write-Host "FAIL: $_" -ForegroundColor Red
    }
}

# Test Customer List
Write-Host "`n[TEST] Get Customer List..."
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET -TimeoutSec 10
    Write-Host "PASS: Get Customer List OK" -ForegroundColor Green
    Write-Host "Total Count: $($response.data.totalCount)"
    
    if ($response.data.items.Count -gt 0) {
        $first = $response.data.items[0]
        Write-Host "`nFirst customer in list:"
        Write-Host "  customerCode: $($first.customerCode)"
        Write-Host "  officialName: $($first.officialName)"
        Write-Host "  customerName: $($first.customerName)"
        Write-Host "  level: $($first.level)"
        Write-Host "  customerLevel: $($first.customerLevel)"
    }
} catch {
    Write-Host "FAIL: $_" -ForegroundColor Red
}

Write-Host "`n========== Frontend Field Names Test Complete =========="
