$headers = @{'Content-Type'='application/json'}
$body = @{
    userName = "testapi001"
    email = "testapi001@test.com"
    password = "Test123456!"
} | ConvertTo-Json

try {
    $r = Invoke-RestMethod -Uri 'http://localhost:5000/api/v1/auth/register' -Method POST -Headers $headers -Body $body
    Write-Host "API响应成功:"
    $r | ConvertTo-Json -Depth 3
} catch {
    Write-Host "API调用失败:"
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        $responseBody = $reader.ReadToEnd()
        Write-Host "错误详情: $responseBody"
    }
}
