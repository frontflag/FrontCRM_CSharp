using CRM.API.Extensions;
using CRM.API.Middlewares;
using Microsoft.EntityFrameworkCore;
using CRM.Infrastructure.Data;
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
        });
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (app.Environment.IsDevelopment())
            {
                context.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating the database");
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
