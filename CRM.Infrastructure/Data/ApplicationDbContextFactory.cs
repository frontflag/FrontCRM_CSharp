using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// EF Core 设计时 DbContext 工厂：避免 dotnet ef 启动 CRM.API 的 Host 逻辑。
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // dotnet ef 通常在执行目录（例如 CRM.API）启动，这里以当前目录为 base path 读取配置
            var basePath = Directory.GetCurrentDirectory();

            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                "Production";

            string? connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // 先读环境专用文件，再回退通用文件
                var candidates = new[]
                {
                    Path.Combine(basePath, $"appsettings.{environment}.json"),
                    Path.Combine(basePath, "appsettings.json")
                };

                foreach (var file in candidates)
                {
                    if (!File.Exists(file)) continue;

                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) &&
                        connStrings.TryGetProperty("DefaultConnection", out var dc))
                    {
                        connectionString = dc.GetString();
                        if (!string.IsNullOrWhiteSpace(connectionString))
                            break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "未找到 ConnectionStrings:DefaultConnection。请设置环境变量 ConnectionStrings__DefaultConnection，或在当前目录提供 appsettings.json。");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            // Some environments intentionally apply hand-written SQL migrations first.
            // Ignore model snapshot drift during "dotnet ef database update" to unblock schema patching.
            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

