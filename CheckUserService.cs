using System;
using Microsoft.EntityFrameworkCore;
using CRM.Infrastructure.Data;

class Program
{
    static void Main()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=frontcrm;Username=postgres;Password=123456")
            .Options;

        using var context = new ApplicationDbContext(options);
        
        // 检查指定邮箱的用户
        var user = context.Users.FirstOrDefaultAsync(u => u.Email == "3161798@qq.com").Result;
        
        if (user != null)
        {
            Console.WriteLine($"User found: {user.UserName}");
            Console.WriteLine($"Email: {user.Email}");
            Console.WriteLine($"Status: {user.Status}");
            Console.WriteLine($"IsActive: {user.IsActive}");
            Console.WriteLine($"Plain Password: {user.PasswordPlain}");
        }
        else
        {
            Console.WriteLine("User not found!");
            Console.WriteLine("\nExisting users:");
            var users = context.Users.Take(10).ToListAsync().Result;
            foreach (var u in users)
            {
                Console.WriteLine($"  {u.UserName} - {u.Email} (Status: {u.Status})");
            }
        }
    }
}
