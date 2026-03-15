using System;
using Npgsql;

class TestDbConnection
{
    static void Main()
    {
        Console.WriteLine("=== PostgreSQL 连接测试 ===");
        Console.WriteLine();
        
        // 测试连接字符串
        string connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=1234";
        
        Console.WriteLine("连接参数:");
        Console.WriteLine($"- Host: localhost");
        Console.WriteLine($"- Port: 5432");
        Console.WriteLine($"- Database: postgres");
        Console.WriteLine($"- Username: postgres");
        Console.WriteLine($"- Password: 1234");
        Console.WriteLine();
        
        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                Console.WriteLine("尝试连接...");
                connection.Open();
                Console.WriteLine("✅ 连接成功!");
                
                // 获取服务器版本
                using (var command = new NpgsqlCommand("SELECT version();", connection))
                {
                    var version = command.ExecuteScalar();
                    Console.WriteLine($"PostgreSQL 版本: {version}");
                }
                
                // 检查数据库列表
                Console.WriteLine();
                Console.WriteLine("数据库列表:");
                using (var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dbName = reader.GetString(0);
                            Console.WriteLine($"- {dbName}");
                            
                            if (dbName == "FrontCRM")
                            {
                                Console.WriteLine("  ✅ FrontCRM 数据库存在");
                            }
                        }
                    }
                }
                
                connection.Close();
            }
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"❌ 连接失败: {ex.Message}");
            
            // 尝试常见密码
            Console.WriteLine();
            Console.WriteLine("尝试常用密码...");
            string[] passwords = ["postgres", "postgres123", "123456", "password", ""];
            
            foreach (string password in passwords)
            {
                try
                {
                    string testConnStr = $"Host=localhost;Port=5432;Database=postgres;Username=postgres;Password={password}";
                    using (var conn = new NpgsqlConnection(testConnStr))
                    {
                        conn.Open();
                        Console.WriteLine($"✅ 成功! 密码: {(string.IsNullOrEmpty(password) ? "[空密码]" : password)}");
                        conn.Close();
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 未知错误: {ex.Message}");
        }
        
        Console.WriteLine();
        Console.WriteLine("=== 测试完成 ===");
    }
}