# 解析JWT token获取用户ID
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJ0ZXN0YXBpMDAxQHRlc3QuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6InRlc3RhcGkwMDEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijg4MTMzZGI3LTFkY2MtNDkzNC1hMzhmLTE5NjRmMWNjMjYyNCIsImV4cCI6MTc3MzU2MzIxNywiaXNzIjoiRnJvbnRDUk0iLCJhdWQiOiJGcm9udENSTVVzZXJzIn0.ccNNJG0hbw4bQAHQHfl7DHNNIMZW6DWo4ZaJ__UHYM8"

# Base64解码payload
$parts = $token.Split('.')
$payload = $parts[1]
# 添加padding
switch ($payload.Length % 4) {
    0 { break }
    2 { $payload += "=="; break }
    3 { $payload += "="; break }
}
$bytes = [System.Convert]::FromBase64String($payload)
$json = [System.Text.Encoding]::UTF8.GetString($bytes)
$claims = $json | ConvertFrom-Json

Write-Host "Token中的用户ID (NameIdentifier): $($claims.'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier')"
Write-Host "Token中的用户名: $($claims.'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name')"
Write-Host "Token中的邮箱: $($claims.'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress')"
