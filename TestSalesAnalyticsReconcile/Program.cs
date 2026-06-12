using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Services;
using CRM.Infrastructure.Analytics;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.Extensions;
using CRM.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static string FindConfigPath()
{
    var dir = Directory.GetCurrentDirectory();
    for (var i = 0; i < 6; i++)
    {
        var p = Path.Combine(dir, "CRM.API", "appsettings.Development.json");
        if (File.Exists(p)) return p;
        p = Path.Combine(dir, "CRM.API", "appsettings.json");
        if (File.Exists(p)) return p;
        dir = Directory.GetParent(dir)?.FullName ?? dir;
    }
    throw new FileNotFoundException("找不到 CRM.API appsettings");
}

var configPath = FindConfigPath();
var config = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false)
    .AddJsonFile(configPath.Replace("appsettings.json", "appsettings.Development.json"), optional: true)
    .Build();

var conn = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    Console.Error.WriteLine("未配置 ConnectionStrings:DefaultConnection");
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddInfrastructure(conn);
services.AddScoped<IRbacService, RbacService>();
services.AddScoped<IDataPermissionService, DataPermissionService>();
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQ>, Repository<CRM.Core.Models.RFQ.RFQ>>();
services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQItem>, Repository<CRM.Core.Models.RFQ.RFQItem>>();
services.AddScoped<IRepository<CRM.Core.Models.Customer.CustomerInfo>, Repository<CRM.Core.Models.Customer.CustomerInfo>>();
services.AddScoped<IRepository<CRM.Core.Models.Vendor.VendorInfo>, Repository<CRM.Core.Models.Vendor.VendorInfo>>();
services.AddScoped<IRepository<CRM.Core.Models.User>, Repository<CRM.Core.Models.User>>();
services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();
services.AddScoped<ISalesAnalyticsReconciliationService, SalesAnalyticsReconciliationService>();

await using var sp = services.BuildServiceProvider().CreateAsyncScope();
var db = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var reconcile = sp.ServiceProvider.GetRequiredService<ISalesAnalyticsReconciliationService>();

Console.WriteLine("销售看板联调对账");
Console.WriteLine($"数据库: {MaskConn(conn)}");
Console.WriteLine();

var dateTo = DateTime.UtcNow.Date;
var dateFrom = dateTo.AddMonths(-5);

var sampleUsers = await db.Users.AsNoTracking()
    .Where(u => u.UserName == "sales_mgr" || u.UserName == "sales_staff" || u.UserName == "admin")
    .Select(u => new { u.Id, u.UserName })
    .ToListAsync();

if (sampleUsers.Count == 0)
{
    Console.WriteLine("未找到 sales_mgr / sales_staff / admin，请先准备演示账号。");
    return 2;
}

var anyFail = false;
foreach (var u in sampleUsers)
{
    var summary = await sp.ServiceProvider.GetRequiredService<IRbacService>().GetUserPermissionSummaryAsync(u.Id);
    var levels = CRM.Core.Utilities.SalesAnalyticsScopeValidator.GetAllowedViewLevels(summary);
    if (levels.Count == 0)
    {
        Console.WriteLine($"[{u.UserName}] 无销售数据范围，跳过");
        continue;
    }

    foreach (var level in levels)
    {
        try
        {
            var report = await reconcile.ReconcileAsync(
                u.Id,
                new SalesAnalyticsQueryParams
                {
                    ViewLevel = level,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                });

            var status = report.AllMatched ? "PASS" : "FAIL";
            if (!report.AllMatched) anyFail = true;
            Console.WriteLine($"[{status}] {u.UserName}  scope={report.SaleDataScope}  view={report.ViewLevel}  {dateFrom:yyyy-MM-dd}~{dateTo:yyyy-MM-dd}");

            if (report.ListPathMatched == true)
                Console.WriteLine("    list-path: PASS");
            else if (report.ListPathMatched == false)
                Console.WriteLine("    list-path: FAIL");

            foreach (var m in report.Metrics.Where(x => !x.Matched))
            {
                Console.WriteLine($"    ✗ {m.Label}: dashboard={m.DashboardValue} baseline={m.BaselineValue} delta={m.Delta}");
            }
        }
        catch (Exception ex)
        {
            anyFail = true;
            Console.WriteLine($"[ERROR] {u.UserName} view={level}: {ex.Message}");
        }
    }
}

Console.WriteLine();
Console.WriteLine(anyFail ? "对账未全部通过。" : "对账全部通过。");
return anyFail ? 3 : 0;

static string MaskConn(string c)
{
    var parts = c.Split(';', StringSplitOptions.RemoveEmptyEntries);
    return string.Join(";", parts.Where(p =>
        !p.StartsWith("Password", StringComparison.OrdinalIgnoreCase)));
}
