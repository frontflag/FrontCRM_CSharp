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

            // 注册物料数据服务（当前使用 Mock 实现，待 Nexar API 就绪后替换为 NexarComponentDataService）
            // 替换方式：将下面一行改为 services.AddScoped<IComponentDataService, NexarComponentDataService>();
            services.AddScoped<IComponentDataService, MockComponentDataService>();
            services.AddScoped<IComponentCacheService, ComponentCacheService>();

            return services;
        }
    }
}
