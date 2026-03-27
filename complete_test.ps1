# Complete Business Process Test
$ErrorActionPreference = "Stop"

$apiUrl = "http://localhost:5000/api/v1"
$testResults = @()
$token = $null
$customerId = $null
$contactId = $null
$addressId = $null

function Test-Step {
    param($Name, $ScriptBlock)
    try {
        $result = & $ScriptBlock
        $script:testResults += [PSCustomObject]@{ Name = $Name; Result = "PASS"; Message = $result }
        Write-Host "[PASS] $Name - $result" -ForegroundColor Green
        return $true
    } catch {
        $errMsg = $_.ToString()
        if ($errMsg.Length -gt 100) { $errMsg = $errMsg.Substring(0, 100) + "..." }
        $script:testResults += [PSCustomObject]@{ Name = $Name; Result = "FAIL"; Message = $errMsg }
        Write-Host "[FAIL] $Name - $errMsg" -ForegroundColor Red
        return $false
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    COMPLETE BUSINESS PROCESS TEST" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testUser = @{ userName = "user_$timestamp"; email = "user_$timestamp@test.com"; password = "Test123456!" }
$testCustomer = @{ 
    customerName = "Customer $timestamp"
    customerShortName = "Cust $timestamp"
    customerLevel = "A"
    customerType = 1
    customerCode = "C$timestamp"
    unifiedSocialCreditCode = "91110000MA00${timestamp}XX"
    remarks = "Test customer"
    creditLimit = 100000
    paymentTerms = "Net 30"
    currency = "RMB"
}

# ==================== PHASE 1: USER AUTHENTICATION ====================
Write-Host "`n[PHASE 1: USER AUTHENTICATION]" -ForegroundColor Yellow

Test-Step "1.1 Register User" {
    $body = $testUser | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/auth/register" -Method POST -ContentType "application/json" -Body $body
    if (-not $response.success) { throw $response.message }
    $script:token = $response.data.token
    return "Token received"
}

Test-Step "1.2 Login User" {
    $login = @{ email = $testUser.email; password = $testUser.password } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/auth/login" -Method POST -ContentType "application/json" -Body $login
    if (-not $response.success) { throw $response.message }
    $script:token = $response.data.token
    return "Login successful"
}

Test-Step "1.3 Get Current User" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/auth/me" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "User: $($response.data.UserName)"
}

# ==================== PHASE 2: CUSTOMER MANAGEMENT ====================
Write-Host "`n[PHASE 2: CUSTOMER MANAGEMENT]" -ForegroundColor Yellow

Test-Step "2.1 Create Customer" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $body = $testCustomer | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers" -Method POST -Headers $headers -Body $body
    if (-not $response.success) { throw $response.message }
    $script:customerId = $response.data.id
    return "ID: $customerId"
}

Test-Step "2.2 Get Customer List" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers?pageNumber=1&pageSize=10" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "Count: $($response.data.items.Count)"
}

Test-Step "2.3 Get Customer Detail" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "Name: $($response.data.officialName)"
}

Test-Step "2.4 Update Customer" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $update = @{ 
        customerName = $testCustomer.customerName
        customerShortName = "Updated Name"
        customerLevel = "B"
        remarks = "Updated remarks"
    } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId" -Method PUT -Headers $headers -Body $update
    if (-not $response.success) { throw $response.message }
    return "Updated"
}

Test-Step "2.5 Get Customer Statistics" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/statistics" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "Total: $($response.data.totalCount)"
}

# ==================== PHASE 3: CONTACT MANAGEMENT ====================
Write-Host "`n[PHASE 3: CONTACT MANAGEMENT]" -ForegroundColor Yellow

$testContact = @{ name = "John Doe"; phone = "13800138000"; email = "john@test.com"; position = "Manager"; isDefault = $true }

Test-Step "3.1 Create Contact" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $body = $testContact | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/contacts" -Method POST -Headers $headers -Body $body
    if (-not $response.success) { throw $response.message }
    $script:contactId = $response.data.id
    return "ID: $contactId"
}

Test-Step "3.2 Get Customer Contacts" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/contacts" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "Count: $($response.data.Count)"
}

Test-Step "3.3 Update Contact" {
    if (-not $contactId) { throw "No contact ID" }
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $update = @{ name = "Jane Doe"; phone = "13900139000"; position = "Director" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/contacts/$contactId" -Method PUT -Headers $headers -Body $update
    if (-not $response.success) { throw $response.message }
    return "Updated"
}

# ==================== PHASE 4: ADDRESS MANAGEMENT ====================
Write-Host "`n[PHASE 4: ADDRESS MANAGEMENT]" -ForegroundColor Yellow

$testAddress = @{ 
    addressType = 1
    address = "100 Test Road, Beijing"
    contactName = "Jane Smith"
    contactPhone = "13900139000"
    isDefault = $true
}

Test-Step "4.1 Create Address" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $body = $testAddress | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/addresses" -Method POST -Headers $headers -Body $body
    if (-not $response.success) { throw $response.message }
    $script:addressId = $response.data.id
    return "ID: $addressId"
}

Test-Step "4.2 Get Customer Addresses" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/addresses" -Method GET -Headers $headers
    if (-not $response.success) { throw "Failed" }
    return "Count: $($response.data.Count)"
}

Test-Step "4.3 Update Address" {
    if (-not $addressId) { throw "No address ID" }
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $update = @{ address = "200 New Road, Shanghai"; contactName = "Tom Brown" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/addresses/$addressId" -Method PUT -Headers $headers -Body $update
    if (-not $response.success) { throw $response.message }
    return "Updated"
}

# ==================== PHASE 5: DATABASE VERIFICATION ====================
Write-Host "`n[PHASE 5: DATABASE VERIFICATION]" -ForegroundColor Yellow

Start-Sleep -Seconds 1

Test-Step "5.1 Verify User in DB" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM ""user"" WHERE ""UserName"" = '$($testUser.userName)'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Not found" }
    return "Found"
}

Test-Step "5.2 Verify Customer in DB" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customerinfo WHERE ""OfficialName"" = '$($testCustomer.customerName)'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Not found" }
    return "Found"
}

Test-Step "5.3 Verify Contact in DB" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customercontactinfo WHERE ""CustomerId"" = '$customerId'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Not found" }
    return "Found"
}

Test-Step "5.4 Verify Address in DB" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customeraddress WHERE ""CustomerId"" = '$customerId'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Not found" }
    return "Found"
}

# ==================== SUMMARY ====================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "           TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passed = ($testResults | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Result -eq "FAIL" }).Count
$total = $testResults.Count

Write-Host "Total Tests:  $total"
Write-Host "Passed:       $passed" -ForegroundColor Green
Write-Host "Failed:       $failed" -ForegroundColor Red
if ($total -gt 0) {
    Write-Host "Pass Rate:    $([math]::Round(($passed/$total)*100, 1))%" -ForegroundColor Yellow
}

if ($failed -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Result -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Message)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan

# Save report
$reportFile = "test_report_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
$testResults | Export-Csv -Path $reportFile -NoTypeInformation
Write-Host "Report saved: $reportFile" -ForegroundColor Green

exit $failed
