$baseUrl = "http://localhost:5000"

try {
    # 先获取列表
    $list = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers?pageNumber=1&pageSize=10" -Method GET
    Write-Host "=== 客户列表 ===" -ForegroundColor Cyan
    $list.data | ConvertTo-Json -Depth 3
    
    if ($list.data.items.Count -gt 0) {
        $id = $list.data.items[0].id
        Write-Host ""
        Write-Host "=== 客户详情 (ID: $id) ===" -ForegroundColor Cyan
        $detail = Invoke-RestMethod -Uri "$baseUrl/api/v1/customers/$id" -Method GET
        $detail.data | ConvertTo-Json -Depth 5
    }
} catch {
    Write-Host "错误: $_" -ForegroundColor Red
}
