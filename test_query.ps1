try {
    # 安装 Npgsql 模块（如果未安装）
    if (-not (Get-Module -ListAvailable -Name Npgsql)) {
        Install-Package Npgsql -Source nuget.org -Force -Scope CurrentUser
    }
    
    # 连接 PostgreSQL
    $conn = New-Object Npgsql.NpgsqlConnection("Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT * FROM "user" ORDER BY "CreateTime" DESC LIMIT 5'
    
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
    
    if ($results.Count -eq 0) {
        Write-Host "user表中没有数据！"
    } else {
        Write-Host "user表数据:"
        $results | ConvertTo-Json -Depth 3
    }
} catch {
    Write-Host "查询失败: $_"
}
