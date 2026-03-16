using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.Repositories;
using CRM.Infrastructure.Services;

namespace CRM.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            // 使用 PostgreSQL 数据库
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 注册流水号服务和错误日志服务
            services.AddScoped<ISerialNumberService, SerialNumberService>();
            services.AddScoped<IErrorLogService, ErrorLogService>();

            return services;
        }
    }
}
