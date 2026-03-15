using System;
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234";

try
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    
    Console.WriteLine("========================================");
    Console.WriteLine("    COMPLETE BUSINESS DATA CHECK");
    Console.WriteLine("========================================\n");
    
    // 1. User Table
    Console.WriteLine("[1] USER TABLE");
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"user\"", conn))
    {
        var count = (long)await cmd.ExecuteScalarAsync();
        Console.WriteLine($"    Total Records: {count}");
    }
    // 显示最近5个用户
    using (var cmd = new NpgsqlCommand("SELECT \"UserId\", \"UserName\", \"Email\", \"Status\" FROM \"user\" ORDER BY \"CreateTime\" DESC LIMIT 5", conn))
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"    - {reader["UserName"]} ({reader["Email"]}) - Status: {reader["Status"]}");
        }
    }
    
    // 2. Customer Table
    Console.WriteLine("\n[2] CUSTOMER TABLE");
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM customerinfo", conn))
    {
        Console.WriteLine($"    Total Records: {await cmd.ExecuteScalarAsync()}");
    }
    using (var cmd = new NpgsqlCommand("SELECT \"CustomerId\", \"CustomerCode\", \"OfficialName\" FROM customerinfo ORDER BY \"CreateTime\" DESC LIMIT 5", conn))
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"    - {reader["CustomerCode"]}: {reader["OfficialName"]}");
        }
    }
    
    // 3. Contact Table
    Console.WriteLine("\n[3] CONTACT TABLE");
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM customercontactinfo", conn))
    {
        Console.WriteLine($"    Total Records: {await cmd.ExecuteScalarAsync()}");
    }
    
    // 4. Address Table
    Console.WriteLine("\n[4] ADDRESS TABLE");
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM customeraddress", conn))
    {
        Console.WriteLine($"    Total Records: {await cmd.ExecuteScalarAsync()}");
    }
    
    // 5. Bank Table
    Console.WriteLine("\n[5] BANK TABLE");
    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM customerbankinfo", conn))
    {
        Console.WriteLine($"    Total Records: {await cmd.ExecuteScalarAsync()}");
    }
    
    // Summary
    Console.WriteLine("\n========================================");
    Console.WriteLine("    SUMMARY");
    Console.WriteLine("========================================");
    
    string[] tables = { "user", "customerinfo", "customercontactinfo", "customeraddress", "customerbankinfo" };
    long totalRecords = 0;
    foreach (var table in tables)
    {
        using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {table}", conn);
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        totalRecords += count;
        var status = count > 0 ? "✓" : "○";
        Console.WriteLine($"    {status} {table,-25} : {count,3} records");
    }
    
    Console.WriteLine($"\n    Total Records Across All Tables: {totalRecords}");
    Console.WriteLine("========================================");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
