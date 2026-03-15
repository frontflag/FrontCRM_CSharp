$url = "http://localhost:5000/api/v1/customers?pageNumber=1&pageSize=10"
Write-Host "Testing: $url" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri $url -Method GET -TimeoutSec 10
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)"
} catch {
    Write-Host "FAILED: $_" -ForegroundColor Red
}
