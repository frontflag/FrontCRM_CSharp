using System;
using System.Data;
using Npgsql;

class TestDbConnect
{
    static void Main()
    {
        Console.WriteLine("=== FrontCRM 数据库连接测试 ===");
        Console.WriteLine();
        
        string connectionString = "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234";
        
        Console.WriteLine("连接字符串:");
        Console.WriteLine(connectionString);
        Console.WriteLine();
        
        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                Console.WriteLine("正在连接...");
                connection.Open();
                
                if (connection.State == ConnectionState.Open)
                {
                    Console.WriteLine("✅ 连接成功!");
                    
                    // 获取数据库信息
                    using (NpgsqlCommand command = new NpgsqlCommand("SELECT current_database(), current_user, version()", connection))
                    {
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Console.WriteLine($"\n数据库信息:");
                                Console.WriteLine($"  数据库名: {reader.GetString(0)}");
                                Console.WriteLine($"  当前用户: {reader.GetString(1)}");
                                Console.WriteLine($"  PostgreSQL 版本: {reader.GetString(2)}");
                            }
                        }
                    }
                    
                    // 检查表数量
                    using (NpgsqlCommand command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'", connection))
                    {
                        int tableCount = Convert.ToInt32(command.ExecuteScalar());
                        Console.WriteLine($"\n表数量: {tableCount}");
                        
                        if (tableCount > 0)
                        {
                            // 列出表名
                            using (NpgsqlCommand tablesCommand = new NpgsqlCommand(
                                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name", connection))
                            {
                                using (NpgsqlDataReader tablesReader = tablesCommand.ExecuteReader())
                                {
                                    Console.WriteLine("表列表:");
                                    while (tablesReader.Read())
                                    {
                                        Console.WriteLine($"  - {tablesReader.GetString(0)}");
                                    }
                                }
                            }
                        }
                    }
                    
                    connection.Close();
                    
                    Console.WriteLine("\n🎉 测试完成! 数据库可正常访问。");
                }
                else
                {
                    Console.WriteLine("❌ 连接失败: 连接状态不是 Open");
                }
            }
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"❌ PostgreSQL 错误: {ex.Message}");
            
            // 尝试其他常见密码
            Console.WriteLine("\n尝试其他常见密码...");
            string[] passwords = { "postgres", "postgres123", "123456", "password", "" };
            
            foreach (string password in passwords)
            {
                try
                {
                    string testConnStr = $"Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password={password}";
                    using (NpgsqlConnection testConn = new NpgsqlConnection(testConnStr))
                    {
                        testConn.Open();
                        Console.WriteLine($"✅ 成功! 正确密码: '{(string.IsNullOrEmpty(password) ? "[空密码]" : password)}'");
                        testConn.Close();
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
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}