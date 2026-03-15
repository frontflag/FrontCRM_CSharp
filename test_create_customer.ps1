# 创建测试客户
$baseUrl = "http://localhost:5000"

$customerData = @{
    customerCode = "CUST001"
    customerName = "测试客户公司"
    customerShortName = "测试客户"
    customerType = 0
    customerLevel = "Normal"
    industry = "Technology"
    unifiedSocialCreditCode = "91110000123456789X"
    creditLimit = 100000
    paymentTerms = 30
    currency = 1
    taxRate = 13
    invoiceType = 1
    isActive = $true
    remarks = "这是一个测试客户"
    contacts = @(
        @{
            contactName = "张三"
            gender = 0
            mobilePhone = "13800138000"
            email = "zhangsan@test.com"
            isDefault = $true
        }
    )
} | ConvertTo-Json -Depth 5

try {
    Write-Host "=== 创建测试客户 ===" -ForegroundColor Cyan
    $headers = @{
        "Content-Type" = "application/json"
    }
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $customerData -Headers $headers
    Write-Host "创建成功!" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
    
    Write-Host ""
    Write-Host "=== 再次查询客户列表 ===" -ForegroundColor Cyan
    $list = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET
    $list | ConvertTo-Json -Depth 3
} catch {
    Write-Host "错误: $_" -ForegroundColor Red
}
