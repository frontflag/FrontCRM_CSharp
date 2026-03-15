# 完整业务流程测试脚本
param(
    [string]$ApiBaseUrl = "http://localhost:5000/api/v1",
    [string]$DbHost = "localhost",
    [int]$DbPort = 5432,
    [string]$DbName = "FrontCRM",
    [string]$DbUser = "postgres",
    [string]$DbPassword = "1234"
)

$ErrorActionPreference = "Stop"
$script:testResults = @()
$script:authToken = $null
$script:testUserId = $null
$script:testCustomerId = $null
$script:testContactId = $null
$script:testAddressId = $null

function Write-TestResult {
    param($TestName, $Success, $Message)
    $result = [PSCustomObject]@{
        TestName = $TestName
        Success = $Success
        Message = $Message
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    $script:testResults += $result
    
    $status = if ($Success) { "PASS" } else { "FAIL" }
    Write-Host "[$($result.Timestamp)] [$status] $TestName - $Message"
}

function Invoke-ApiRequest {
    param($Method, $Endpoint, $Body = $null, $Token = $null)
    
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    
    $uri = "$ApiBaseUrl$Endpoint"
    
    try {
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            return Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -Body $jsonBody -TimeoutSec 10
        } else {
            return Invoke-RestMethod -Uri $uri -Method $Method -Headers $headers -TimeoutSec 10
        }
    } catch {
        $errorMsg = $_.Exception.Message
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $errorBody = $reader.ReadToEnd()
            $errorMsg = "$errorMsg : $errorBody"
        }
        throw $errorMsg
    }
}

function Query-Database {
    param($Sql)
    $connString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"
    try {
        Add-Type -Path "$PSScriptRoot/TestDbQuery/bin/Debug/net9.0/Npgsql.dll" -ErrorAction SilentlyContinue
    } catch {}
    
    try {
        $conn = New-Object Npgsql.NpgsqlConnection($connString)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $reader = $cmd.ExecuteReader()
        $results = @()
        while ($reader.Read()) {
            $row = @{}
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $row[$reader.GetName($i)] = $reader.GetValue($i)
            }
            $results += $row
        }
        $reader.Close()
        $conn.Close()
        return $results
    } catch {
        throw "Database query failed: $_"
    }
}

Write-Host "========================================"
Write-Host "    Complete Business Process Test"
Write-Host "========================================"
Write-Host "API URL: $ApiBaseUrl"
Write-Host "Database: ${DbName}@${DbHost}:${DbPort}"
Write-Host ""

# ==================== TEST 1: User Authentication ====================
Write-Host "`n[TEST GROUP 1: User Authentication]"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testUser = @{
    userName = "testuser_$timestamp"
    email = "test_$timestamp@test.com"
    password = "Test123456!"
}

# 1.1 Register
Write-Host "`nTest 1.1: User Registration"
try {
    $response = Invoke-ApiRequest -Method "POST" -Endpoint "/auth/register" -Body $testUser
    if ($response.success -and $response.data.token) {
        $script:authToken = $response.data.token
        Write-TestResult -TestName "1.1 User Registration" -Success $true -Message "User $($testUser.userName) registered successfully"
        
        # Verify in database
        Start-Sleep -Milliseconds 500
        try {
            $dbResult = Query-Database -Sql "SELECT COUNT(*) as cnt FROM ""user"" WHERE ""UserName"" = '$($testUser.userName)'"
            if ($dbResult[0].cnt -eq 1) {
                Write-TestResult -TestName "1.2 DB Verify - User Registration" -Success $true -Message "User found in database"
            } else {
                Write-TestResult -TestName "1.2 DB Verify - User Registration" -Success $false -Message "User NOT found in database"
            }
        } catch {
            Write-TestResult -TestName "1.2 DB Verify - User Registration" -Success $false -Message "DB query failed: $_"
        }
    } else {
        Write-TestResult -TestName "1.1 User Registration" -Success $false -Message "Registration failed: $($response.message)"
    }
} catch {
    Write-TestResult -TestName "1.1 User Registration" -Success $false -Message "Exception: $_"
}

# 1.2 Login
Write-Host "`nTest 1.3: User Login"
try {
    $loginBody = @{ email = $testUser.email; password = $testUser.password }
    $response = Invoke-ApiRequest -Method "POST" -Endpoint "/auth/login" -Body $loginBody
    if ($response.success -and $response.data.token) {
        $script:authToken = $response.data.token
        Write-TestResult -TestName "1.3 User Login" -Success $true -Message "Login successful, token received"
    } else {
        Write-TestResult -TestName "1.3 User Login" -Success $false -Message "Login failed: $($response.message)"
    }
} catch {
    Write-TestResult -TestName "1.3 User Login" -Success $false -Message "Exception: $_"
}

# 1.3 Get Current User
Write-Host "`nTest 1.4: Get Current User"
try {
    $response = Invoke-ApiRequest -Method "GET" -Endpoint "/auth/me" -Token $script:authToken
    if ($response.success) {
        Write-TestResult -TestName "1.4 Get Current User" -Success $true -Message "Retrieved user: $($response.data.UserName)"
    } else {
        Write-TestResult -TestName "1.4 Get Current User" -Success $false -Message "Failed to get user"
    }
} catch {
    Write-TestResult -TestName "1.4 Get Current User" -Success $false -Message "Exception: $_"
}

# ==================== TEST 2: Customer Management ====================
Write-Host "`n[TEST GROUP 2: Customer Management]"

$testCustomer = @{
    customerName = "Test Customer $timestamp"
    customerShortName = "Test Cust"
    customerLevel = "A"
    customerType = 1
    unifiedSocialCreditCode = "91110000MA00${timestamp}XX"
    remarks = "Test customer created by automation"
    creditLimit = 100000
    paymentTerms = "Net 30"
    currency = "CNY"
}

# 2.1 Create Customer
Write-Host "`nTest 2.1: Create Customer"
try {
    $response = Invoke-ApiRequest -Method "POST" -Endpoint "/customers" -Body $testCustomer -Token $script:authToken
    if ($response.success -and $response.data) {
        $script:testCustomerId = $response.data.id
        Write-TestResult -TestName "2.1 Create Customer" -Success $true -Message "Customer created with ID: $script:testCustomerId"
        
        # Verify in database
        Start-Sleep -Milliseconds 500
        try {
            $dbResult = Query-Database -Sql "SELECT COUNT(*) as cnt FROM customerinfo WHERE ""OfficialName"" = '$($testCustomer.customerName)'"
            if ($dbResult[0].cnt -eq 1) {
                Write-TestResult -TestName "2.2 DB Verify - Create Customer" -Success $true -Message "Customer found in database"
            } else {
                Write-TestResult -TestName "2.2 DB Verify - Create Customer" -Success $false -Message "Customer NOT found in database"
            }
        } catch {
            Write-TestResult -TestName "2.2 DB Verify - Create Customer" -Success $false -Message "DB query failed: $_"
        }
    } else {
        Write-TestResult -TestName "2.1 Create Customer" -Success $false -Message "Failed: $($response.message)"
    }
} catch {
    Write-TestResult -TestName "2.1 Create Customer" -Success $false -Message "Exception: $_"
}

# 2.2 Get Customer List
Write-Host "`nTest 2.3: Get Customer List"
try {
    $response = Invoke-ApiRequest -Method "GET" -Endpoint "/customers?pageNumber=1&pageSize=10" -Token $script:authToken
    if ($response.success -and $response.data.items) {
        Write-TestResult -TestName "2.3 Get Customer List" -Success $true -Message "Retrieved $($response.data.items.Count) customers"
    } else {
        Write-TestResult -TestName "2.3 Get Customer List" -Success $false -Message "Failed to get list"
    }
} catch {
    Write-TestResult -TestName "2.3 Get Customer List" -Success $false -Message "Exception: $_"
}

# 2.3 Get Customer Detail
Write-Host "`nTest 2.4: Get Customer Detail"
if ($script:testCustomerId) {
    try {
        $response = Invoke-ApiRequest -Method "GET" -Endpoint "/customers/$script:testCustomerId" -Token $script:authToken
        if ($response.success) {
            Write-TestResult -TestName "2.4 Get Customer Detail" -Success $true -Message "Retrieved: $($response.data.officialName)"
        } else {
            Write-TestResult -TestName "2.4 Get Customer Detail" -Success $false -Message "Failed to get detail"
        }
    } catch {
        Write-TestResult -TestName "2.4 Get Customer Detail" -Success $false -Message "Exception: $_"
    }
} else {
    Write-TestResult -TestName "2.4 Get Customer Detail" -Success $false -Message "Skipped - no customer ID"
}

# 2.4 Update Customer
Write-Host "`nTest 2.5: Update Customer"
if ($script:testCustomerId) {
    try {
        $updateData = @{
            customerName = $testCustomer.customerName
            customerShortName = "Updated Name"
            customerLevel = "B"
            remarks = "Updated remarks"
        }
        $response = Invoke-ApiRequest -Method "PUT" -Endpoint "/customers/$script:testCustomerId" -Body $updateData -Token $script:authToken
        if ($response.success) {
            Write-TestResult -TestName "2.5 Update Customer" -Success $true -Message "Customer updated successfully"
            
            # Verify in database
            Start-Sleep -Milliseconds 500
            try {
                $dbResult = Query-Database -Sql "SELECT ""NickName"" FROM customerinfo WHERE ""Id"" = '$script:testCustomerId'"
                if ($dbResult[0].NickName -eq "Updated Name") {
                    Write-TestResult -TestName "2.6 DB Verify - Update Customer" -Success $true -Message "Update synced to database"
                } else {
                    Write-TestResult -TestName "2.6 DB Verify - Update Customer" -Success $false -Message "Update NOT synced to database"
                }
            } catch {
                Write-TestResult -TestName "2.6 DB Verify - Update Customer" -Success $false -Message "DB query failed: $_"
            }
        } else {
            Write-TestResult -TestName "2.5 Update Customer" -Success $false -Message "Update failed"
        }
    } catch {
        Write-TestResult -TestName "2.5 Update Customer" -Success $false -Message "Exception: $_"
    }
} else {
    Write-TestResult -TestName "2.5 Update Customer" -Success $false -Message "Skipped - no customer ID"
}

# 2.5 Get Customer Statistics
Write-Host "`nTest 2.7: Get Customer Statistics"
try {
    $response = Invoke-ApiRequest -Method "GET" -Endpoint "/customers/statistics" -Token $script:authToken
    if ($response.success) {
        Write-TestResult -TestName "2.7 Get Customer Statistics" -Success $true -Message "Stats: Total=$($response.data.totalCount), Active=$($response.data.activeCount)"
    } else {
        Write-TestResult -TestName "2.7 Get Customer Statistics" -Success $false -Message "Failed to get statistics"
    }
} catch {
    Write-TestResult -TestName "2.7 Get Customer Statistics" -Success $false -Message "Exception: $_"
}

# ==================== TEST 3: Customer Contacts ====================
Write-Host "`n[TEST GROUP 3: Customer Contacts]"

if ($script:testCustomerId) {
    $testContact = @{
        name = "John Doe"
        phone = "13800138000"
        email = "john.doe@test.com"
        position = "Manager"
        isDefault = $true
    }
    
    Write-Host "`nTest 3.1: Create Contact"
    try {
        $response = Invoke-ApiRequest -Method "POST" -Endpoint "/customers/$script:testCustomerId/contacts" -Body $testContact -Token $script:authToken
        if ($response.success) {
            $script:testContactId = $response.data.id
            Write-TestResult -TestName "3.1 Create Contact" -Success $true -Message "Contact created with ID: $script:testContactId"
            
            # Verify in database
            Start-Sleep -Milliseconds 500
            try {
                $dbResult = Query-Database -Sql "SELECT COUNT(*) as cnt FROM customercontactinfo WHERE ""CustomerId"" = '$script:testCustomerId' AND ""Name"" = '$($testContact.name)'"
                if ($dbResult[0].cnt -ge 1) {
                    Write-TestResult -TestName "3.2 DB Verify - Create Contact" -Success $true -Message "Contact found in database"
                } else {
                    Write-TestResult -TestName "3.2 DB Verify - Create Contact" -Success $false -Message "Contact NOT found in database"
                }
            } catch {
                Write-TestResult -TestName "3.2 DB Verify - Create Contact" -Success $false -Message "DB query failed: $_"
            }
        } else {
            Write-TestResult -TestName "3.1 Create Contact" -Success $false -Message "Failed: $($response.message)"
        }
    } catch {
        Write-TestResult -TestName "3.1 Create Contact" -Success $false -Message "Exception: $_"
    }
} else {
    Write-TestResult -TestName "3.1 Create Contact" -Success $false -Message "Skipped - no customer ID"
    Write-TestResult -TestName "3.2 DB Verify - Create Contact" -Success $false -Message "Skipped - no customer ID"
}

# ==================== TEST 4: Customer Addresses ====================
Write-Host "`n[TEST GROUP 4: Customer Addresses]"

if ($script:testCustomerId) {
    $testAddress = @{
        addressType = 1
        address = "100 Test Road, Beijing"
        contactName = "Jane Smith"
        contactPhone = "13900139000"
        isDefault = $true
    }
    
    Write-Host "`nTest 4.1: Create Address"
    try {
        $response = Invoke-ApiRequest -Method "POST" -Endpoint "/customers/$script:testCustomerId/addresses" -Body $testAddress -Token $script:authToken
        if ($response.success) {
            $script:testAddressId = $response.data.id
            Write-TestResult -TestName "4.1 Create Address" -Success $true -Message "Address created with ID: $script:testAddressId"
            
            # Verify in database
            Start-Sleep -Milliseconds 500
            try {
                $dbResult = Query-Database -Sql "SELECT COUNT(*) as cnt FROM customeraddress WHERE ""CustomerId"" = '$script:testCustomerId' AND ""Address"" LIKE '%Test Road%'"
                if ($dbResult[0].cnt -ge 1) {
                    Write-TestResult -TestName "4.2 DB Verify - Create Address" -Success $true -Message "Address found in database"
                } else {
                    Write-TestResult -TestName "4.2 DB Verify - Create Address" -Success $false -Message "Address NOT found in database"
                }
            } catch {
                Write-TestResult -TestName "4.2 DB Verify - Create Address" -Success $false -Message "DB query failed: $_"
            }
        } else {
            Write-TestResult -TestName "4.1 Create Address" -Success $false -Message "Failed: $($response.message)"
        }
    } catch {
        Write-TestResult -TestName "4.1 Create Address" -Success $false -Message "Exception: $_"
    }
} else {
    Write-TestResult -TestName "4.1 Create Address" -Success $false -Message "Skipped - no customer ID"
    Write-TestResult -TestName "4.2 DB Verify - Create Address" -Success $false -Message "Skipped - no customer ID"
}

# ==================== TEST REPORT ====================
Write-Host "`n========================================"
Write-Host "           TEST REPORT"
Write-Host "========================================"

$total = $script:testResults.Count
$passed = ($script:testResults | Where-Object { $_.Success }).Count
$failed = $total - $passed
$passRate = if ($total -gt 0) { [math]::Round(($passed/$total)*100, 2) } else { 0 }

Write-Host "`nTotal Tests:  $total"
Write-Host "Passed:       $passed"
Write-Host "Failed:       $failed"
Write-Host "Pass Rate:    $passRate%"

if ($failed -gt 0) {
    Write-Host "`nFailed Tests:"
    $script:testResults | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "  - $($_.TestName): $($_.Message)"
    }
}

# Export report
$reportPath = "$PSScriptRoot/test_report_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
$script:testResults | Export-Csv -Path $reportPath -NoTypeInformation -Encoding UTF8
Write-Host "`nReport saved to: $reportPath"
Write-Host "========================================"

exit $failed
