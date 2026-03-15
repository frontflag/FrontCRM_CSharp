# Final API Test
$baseUrl = "http://localhost:5002"
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Host "========== Final API Test ==========" -ForegroundColor Cyan

# Test with ALL frontend field names
$newCustomer = @{
    customerCode = "FINAL$timestamp"
    customerName = "Final Test Company"
    customerShortName = "Final Short"
    customerLevel = "VIP"
    customerType = 1
    industry = "Technology"
    salesPersonId = "user-001"
    remarks = "Final test with all fields"
    creditLimit = 10000
    paymentTerms = 60
    currency = 2
    unifiedSocialCreditCode = "91110000123456789X"
} | ConvertTo-Json

Write-Host "Creating customer with all frontend fields..."
$response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $newCustomer -ContentType "application/json" -TimeoutSec 10
$data = $response.data

Write-Host "`nResults:"

# Test all fields
$tests = @(
    @{ Name = "customerName -> officialName"; Expected = "Final Test Company"; Actual = $data.officialName },
    @{ Name = "customerShortName -> nickName"; Expected = "Final Short"; Actual = $data.nickName },
    @{ Name = "customerLevel -> level"; Expected = 5; Actual = $data.level },
    @{ Name = "customerLevel (string)"; Expected = "VIP"; Actual = $data.customerLevel },
    @{ Name = "customerType -> type"; Expected = 1; Actual = $data.type },
    @{ Name = "salesPersonId -> salesUserId"; Expected = "user-001"; Actual = $data.salesUserId },
    @{ Name = "remarks -> remark"; Expected = "Final test with all fields"; Actual = $data.remark },
    @{ Name = "creditLimit -> creditLine"; Expected = 10000; Actual = $data.creditLine },
    @{ Name = "paymentTerms -> payment"; Expected = 60; Actual = $data.payment },
    @{ Name = "currency -> tradeCurrency"; Expected = 2; Actual = $data.tradeCurrency },
    @{ Name = "unifiedSocialCreditCode -> creditCode"; Expected = "91110000123456789X"; Actual = $data.creditCode }
)

$allPass = $true
foreach ($test in $tests) {
    if ($test.Actual -eq $test.Expected) {
        Write-Host "  [PASS] $($test.Name): '$($test.Actual)'" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $($test.Name): '$($test.Actual)' (expected: '$($test.Expected)')" -ForegroundColor Red
        $allPass = $false
    }
}

Write-Host "`n========== Summary ==========" -ForegroundColor Yellow
if ($allPass) {
    Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "SOME TESTS FAILED!" -ForegroundColor Red
}
