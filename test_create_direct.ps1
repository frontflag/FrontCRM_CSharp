$baseUrl = "http://localhost:5000"

# 使用后端字段名直接发送
$backendData = @{
    customerCode = "DIRECT001"
    officialName = "直接测试公司"
    nickName = "直接测试"
    level = 5  # VIP
    type = 0
    industry = "Technology"
    creditCode = "91110000123456789X"
    salesUserId = "user-001"
    creditLine = 100000
    payment = 30
    tradeCurrency = 1
    remark = "直接测试客户"
} | ConvertTo-Json

try {
    Write-Host "=== 使用后端字段名创建客户 ===" -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers" -Method POST -Body $backendData -ContentType "application/json"
    Write-Host "创建成功!" -ForegroundColor Green
    $response.data | Select-Object id, customerCode, officialName, nickName, level, customerLevel, industry | ConvertTo-Json
} catch {
    Write-Host "错误: $_" -ForegroundColor Red
}
