# Simple Business Process Test
$ErrorActionPreference = "Stop"

$apiUrl = "http://localhost:5000/api/v1"
$testResults = @()

function Test-Step {
    param($Name, $ScriptBlock)
    try {
        $result = & $ScriptBlock
        $testResults += [PSCustomObject]@{ Name = $Name; Result = "PASS"; Message = $result }
        Write-Host "[PASS] $Name - $result" -ForegroundColor Green
        return $true
    } catch {
        $testResults += [PSCustomObject]@{ Name = $Name; Result = "FAIL"; Message = $_ }
        Write-Host "[FAIL] $Name - $_" -ForegroundColor Red
        return $false
    }
}

Write-Host "========================================"
Write-Host "    Business Process Test"
Write-Host "========================================"

# Test 1: Register User
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testUser = @{
    userName = "user_$timestamp"
    email = "user_$timestamp@test.com"
    password = "Test123456!"
}

$token = $null
Test-Step "1.1 User Registration" {
    $body = $testUser | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/auth/register" -Method POST -ContentType "application/json" -Body $body
    if (-not $response.success) { throw $response.message }
    $script:token = $response.data.token
    return "User: $($testUser.userName)"
}

# Test 2: Verify User in DB
Start-Sleep -Milliseconds 500
Test-Step "1.2 Verify User in Database" {
    $env:DOTNET_CLI_HOME = "$PSScriptRoot/.dotnet"
    $output = & dotnet run --project "$PSScriptRoot/TestDbQuery" -- "SELECT COUNT(*) as cnt FROM ""user"" WHERE ""UserName"" = '$($testUser.userName)'" 2>&1
    if ($output -match "cnt:\s*(\d+)" -and $matches[1] -eq "1") {
        return "Found in DB"
    }
    # Fallback: use direct query
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    try {
        Add-Type -AssemblyName "Npgsql" -ErrorAction SilentlyContinue
    } catch {
        # Try loading from test project
        $npgsqlPath = "$PSScriptRoot/TestDbQuery/bin/Debug/net9.0/Npgsql.dll"
        if (Test-Path $npgsqlPath) {
            Add-Type -Path $npgsqlPath
        }
    }
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM ""user"" WHERE ""UserName"" = '$($testUser.userName)'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "User not found in database" }
    return "Found in DB"
}

# Test 3: Create Customer
$customerId = $null
$testCustomer = @{
    customerName = "Customer $timestamp"
    customerShortName = "Cust $timestamp"
    customerLevel = "A"
    customerType = 1
    unifiedSocialCreditCode = "91110000MA00${timestamp}XX"
    customerCode = "C$timestamp"
}

Test-Step "2.1 Create Customer" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $body = $testCustomer | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers" -Method POST -Headers $headers -Body $body
    if (-not $response.success) { throw $response.message }
    $script:customerId = $response.data.id
    return "ID: $customerId"
}

# Test 4: Verify Customer in DB
Start-Sleep -Milliseconds 500
Test-Step "2.2 Verify Customer in Database" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customerinfo WHERE ""OfficialName"" = '$($testCustomer.customerName)'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Customer not found in database" }
    return "Found in DB"
}

# Test 5: Get Customer List
Test-Step "2.3 Get Customer List" {
    $headers = @{ "Authorization" = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$apiUrl/customers?pageNumber=1&pageSize=10" -Method GET -Headers $headers
    if (-not $response.success) { throw $response.message }
    return "Count: $($response.data.items.Count)"
}

# Test 6: Update Customer
Test-Step "2.4 Update Customer" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $update = @{ customerName = $testCustomer.customerName; customerShortName = "Updated"; customerLevel = "B" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId" -Method PUT -Headers $headers -Body $update
    if (-not $response.success) { throw $response.message }
    return "Updated"
}

# Test 7: Verify Update in DB
Start-Sleep -Milliseconds 500
Test-Step "2.5 Verify Update in Database" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT ""NickName"" FROM customerinfo WHERE ""Id"" = '$customerId'"
    $nickName = $cmd.ExecuteScalar()
    $conn.Close()
    if ($nickName -ne "Updated") { throw "Update not synced: $nickName" }
    return "Updated in DB"
}

# Test 8: Create Contact
Test-Step "3.1 Create Contact" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $contact = @{ name = "John Doe"; phone = "13800138000"; email = "john@test.com"; position = "Manager" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/contacts" -Method POST -Headers $headers -Body $contact
    if (-not $response.success) { throw $response.message }
    return "Created"
}

# Test 9: Verify Contact in DB
Start-Sleep -Milliseconds 500
Test-Step "3.2 Verify Contact in Database" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customercontactinfo WHERE ""CustomerId"" = '$customerId'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Contact not found" }
    return "Found in DB"
}

# Test 10: Create Address
Test-Step "4.1 Create Address" {
    $headers = @{ "Authorization" = "Bearer $token"; "Content-Type" = "application/json" }
    $address = @{ addressType = 1; address = "100 Test Road"; contactName = "Jane"; contactPhone = "13900139000" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$apiUrl/customers/$customerId/addresses" -Method POST -Headers $headers -Body $address
    if (-not $response.success) { throw $response.message }
    return "Created"
}

# Test 11: Verify Address in DB
Start-Sleep -Milliseconds 500
Test-Step "4.2 Verify Address in Database" {
    $connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234"
    $conn = New-Object Npgsql.NpgsqlConnection($connString)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM customeraddress WHERE ""CustomerId"" = '$customerId'"
    $count = $cmd.ExecuteScalar()
    $conn.Close()
    if ($count -eq 0) { throw "Address not found" }
    return "Found in DB"
}

# Summary
Write-Host "`n========================================"
Write-Host "           TEST SUMMARY"
Write-Host "========================================"

$passed = ($testResults | Where-Object { $_.Result -eq "PASS" }).Count
$failed = ($testResults | Where-Object { $_.Result -eq "FAIL" }).Count
$total = $testResults.Count

Write-Host "Total: $total"
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red

if ($failed -gt 0) {
    Write-Host "`nFailed Tests:"
    $testResults | Where-Object { $_.Result -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Message)"
    }
}

Write-Host "========================================"
exit $failed
