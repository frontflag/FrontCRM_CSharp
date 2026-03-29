using CRM.API.Extensions;
using CRM.API.Middlewares;
using Microsoft.EntityFrameworkCore;
using CRM.Infrastructure.Data;
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

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // 支持 camelCase 属性名映射
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // 允许 null 值
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            // 忽略循环引用
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

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
    app.UseAuthorization();

    app.MapControllers();

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
            
            if (app.Environment.IsDevelopment())
            {
                // 应用所有待处理的迁移
                context.Database.Migrate();
            }
            
            Log.Information("数据库连接成功");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            Log.Fatal(ex, "数据库连接失败: {Message}", ex.Message);
            throw new InvalidOperationException($"数据库连接失败: {ex.Message}", ex);
        }
    }

    Log.Information("Starting FrontCRM API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>仅用于日志：解析 Npgsql 连接串并隐藏密码，切勿记录明文。</summary>
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
