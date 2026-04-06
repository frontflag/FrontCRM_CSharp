using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var argsSet = new HashSet<string>(args, StringComparer.OrdinalIgnoreCase);
if (!argsSet.Contains("--apply"))
{
    Console.WriteLine("Usage: dotnet run --project CRM.DbMigrator -- --apply --force-dev");
    Console.WriteLine("This tool is for development schema updates only.");
    return 1;
}

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Refused: CRM.DbMigrator cannot run under ASPNETCORE_ENVIRONMENT=Production.");
    return 2;
}

var isLocalOrDev =
    string.Equals(environment, "Local", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

if (!isLocalOrDev)
{
    Console.Error.WriteLine($"Refused: unsupported environment '{environment}'. Only Local/Development are allowed.");
    return 5;
}

if (!argsSet.Contains("--force-dev"))
{
    Console.Error.WriteLine("Refused: missing --force-dev.");
    Console.Error.WriteLine("Usage: dotnet run --project CRM.DbMigrator -- --apply --force-dev");
    return 6;
}

var repoRoot = Directory.GetCurrentDirectory();
var apiConfigRoot = Path.Combine(repoRoot, "CRM.API");
if (!Directory.Exists(apiConfigRoot))
{
    // Support running from CRM.DbMigrator directory
    apiConfigRoot = Path.GetFullPath(Path.Combine(repoRoot, "..", "CRM.API"));
}

if (!Directory.Exists(apiConfigRoot))
{
    Console.Error.WriteLine("Cannot locate CRM.API folder for appsettings files.");
    return 3;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(apiConfigRoot)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DefaultConnection is empty. Set ConnectionStrings__DefaultConnection first.");
    return 4;
}

var services = new ServiceCollection();
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    // 与 CRM.API AddInfrastructure 一致：快照与模型暂不完全对齐时仍可执行已提交的迁移 SQL
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

Console.WriteLine($"Environment: {environment}");
Console.WriteLine("Applying EF migrations...");
await db.Database.MigrateAsync();
Console.WriteLine("Done.");
return 0;
