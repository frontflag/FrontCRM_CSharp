$baseUrl = "http://localhost:5000"

$customerData = @{
    customerCode = "TEST001"
    customerName = "测试科技有限公司"
    customerShortName = "测试科技"
    customerType = 0
    customerLevel = "VIP"
    industry = "Technology"
    unifiedSocialCreditCode = "91110000123456789X"
    salesPersonId = "user-001"
    creditLimit = 100000
    paymentTerms = 30
    currency = 1
    taxRate = 13
    invoiceType = 1
    isActive = $true
    remarks = "重要客户"
} | ConvertTo-Json

try {
    Write-Host "=== 创建测试客户 ===" -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $customerData -ContentType "application/json"
    Write-Host "创建成功!" -ForegroundColor Green
    $response.data | ConvertTo-Json -Depth 3
    
    Write-Host ""
    Write-Host "=== 查询客户列表 ===" -ForegroundColor Cyan
    $list = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET
    $list.data.items | ConvertTo-Json -Depth 3
    
    if ($list.data.items.Count -gt 0) {
        $id = $list.data.items[0].id
        Write-Host ""
        Write-Host "=== 查询客户详情 ===" -ForegroundColor Cyan
        $detail = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$id" -Method GET
        $detail.data | ConvertTo-Json -Depth 3
    }
} catch {
    Write-Host "错误: $_" -ForegroundColor Red
}
