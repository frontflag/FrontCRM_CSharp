using System.Net.Sockets;
using System.Reflection;
using CRM.API.Extensions;
using CRM.API.Middlewares;
using CRM.Core.Document;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CRM.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting FrontCRM API");

    builder.Host.UseSerilog();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // 支持 camelCase 属性名映射
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // 反序列化时属性名不区分大小写（避免客户端 userName / UserName 等差异导致绑定为空 → 400）
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            // 允许 null 值
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            // 忽略循环引用
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddApiServices(builder.Configuration, builder.Environment);

    // 将相对路径 DocumentModule:RootPath 固定到程序内容根目录，避免生产环境工作目录变化导致“上传成功但读文件 404”；多实例需共享同一绝对路径（NAS/云盘等）。
    builder.Services.PostConfigure<DocumentModuleOptions>(opts =>
    {
        var root = opts.RootPath?.Trim() ?? "";
        if (string.IsNullOrEmpty(root)) return;
        if (!Path.IsPathRooted(root))
            opts.RootPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, root));
    });

    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseCors("AllowAll");

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FrontCRM API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "FrontCRM API 文档";
    });

    // 仅 HTTP 启动（如 launch profile「http」）时无 HTTPS 端口，UseHttpsRedirection 会报 WRN「Failed to determine the https port」
    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseMiddleware<RequireActiveUserMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // 生产曾出现「DLL 含路由字符串但 Swagger/路由无 exchange-rates、dictionaries/mgmt」：多为依赖程序集与 CRM.API.dll 不同步或类型加载失败。
    try
    {
        var actionProvider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        var items = actionProvider.ActionDescriptors.Items;
        var hasExchange = items.Any(a =>
            (a.AttributeRouteInfo?.Template ?? string.Empty).Contains("exchange-rates", StringComparison.OrdinalIgnoreCase));
        var hasDictMgmt = items.Any(a =>
            (a.AttributeRouteInfo?.Template ?? string.Empty).Contains("dictionaries/mgmt", StringComparison.OrdinalIgnoreCase));
        if (!hasExchange || !hasDictMgmt)
        {
            Log.Warning(
                "路由自检: Swagger/Action 表未包含预期模板 (exchange-rates={Ex}, dictionaries/mgmt={Mgmt})，已注册 Action 数={Total}。请整包同步 publish 内全部 DLL，并查日志是否曾有 ReflectionTypeLoadException。",
                hasExchange, hasDictMgmt, items.Count);

            try
            {
                var asm = typeof(Program).Assembly;
                foreach (var t in asm.GetExportedTypes().Where(x =>
                             x.Name is "FinanceExchangeRateController" or "DictionariesAdminController"))
                    Log.Warning("程序集中仍可反射到类型: {Type}", t.FullName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Error(ex, "GetExportedTypes 失败 (ReflectionTypeLoadException)，请核对 CRM.Core.dll / CRM.Infrastructure.dll 版本是否与 CRM.API.dll 同属一次 publish");
                foreach (var le in ex.LoaderExceptions ?? Array.Empty<Exception?>())
                    if (le != null)
                        Log.Error("Loader: {Msg}", le.Message);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Debug(ex, "启动期路由自检异常（可忽略）");
    }

    // 数据库连接检查 - 无法连接时直接报错停止
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var rawCs = configuration.GetConnectionString("DefaultConnection");
            Log.Information("数据库连接配置（脱敏，Password 已替换）: {MaskedConnectionString}", MaskConnectionStringForLog(rawCs));

            // 测试数据库连接
            var canConnect = context.Database.CanConnect();
            if (!canConnect)
            {
                throw new InvalidOperationException("无法连接到数据库，请检查数据库连接字符串和数据库服务状态。");
            }
            
            // 安全策略：
            // API 启动只检查数据库连通性，不允许自动更改数据库结构或数据。
            // 结构变更必须通过手工 SQL 或独立开发工具执行。
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Count > 0)
            {
                Log.Warning("检测到待执行迁移数量: {Count}。API 启动不会自动迁移数据库。", pendingMigrations.Count);
            }
            
            Log.Information("数据库连接成功");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Log.Fatal(ex, "数据库连接失败: {Message}", ex.Message);
            throw new InvalidOperationException($"数据库连接失败: {ex.Message}", ex);
        }
    }

    var aspnetUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var cfgUrls = app.Configuration["urls"] ?? app.Configuration["Urls"];
    Log.Information(
        "启动诊断: Environment={Environment}; PID={ProcessId}; ContentRoot={ContentRoot}; ASPNETCORE_URLS={AspNetCoreUrls}; 配置Urls={ConfigUrls}; Kestrel端点计数={UrlCount}",
        app.Environment.EnvironmentName,
        Environment.ProcessId,
        app.Environment.ContentRootPath,
        aspnetUrls ?? "(未设置)",
        cfgUrls ?? "(未设置)",
        app.Urls.Count);
    if (app.Urls.Count > 0)
        Log.Information("Kestrel 已注册地址: {Urls}", string.Join(", ", app.Urls));

    Log.Information("即将调用 Kestrel 监听（若失败请查是否端口已被占用）");
    app.Run();
}
catch (Exception ex)
{
    if (IsAddressAlreadyInUse(ex))
    {
        Log.Fatal(
            ex,
            "Kestrel 绑定失败：端口已被占用。请在服务器执行: sudo ss -tlnp | grep :5000 或 sudo lsof -i :5000；停止旧 dotnet/Docker 后再启动。PID={ProcessId}",
            Environment.ProcessId);
    }
    else
        Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>仅用于日志：解析 Npgsql 连接串并隐藏密码，切勿记录明文。</summary>
static bool IsAddressAlreadyInUse(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is IOException io && io.Message.Contains("already in use", StringComparison.OrdinalIgnoreCase))
            return true;
        if (e is SocketException se && se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            return true;
    }
    return false;
}

static string MaskConnectionStringForLog(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return "(empty)";
    try
    {
        var b = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(b.Password))
            b.Password = "***";
        return b.ConnectionString;
    }
    catch (Exception ex)
    {
        return $"(无法解析连接串: {ex.Message})";
    }
}
