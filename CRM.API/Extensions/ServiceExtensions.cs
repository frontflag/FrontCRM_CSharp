using CRM.API.Services.Interfaces;
using CRM.API.Services.Implementations;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CRM.Infrastructure.Extensions;

namespace CRM.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=postgres123";

            services.AddInfrastructure(connectionString);

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IRFQService, RFQService>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQ>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQ>>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQItem>>();
            services.AddScoped<IQuoteService, QuoteService>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.Quote>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.Quote>>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.QuoteItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.QuoteItem>>();

            // 销售订单模块
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrder>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrder>>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrderItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrderItem>>();

            // 采购订单模块
            services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
            services.AddScoped<IRepository<CRM.Core.Models.Purchase.PurchaseOrder>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Purchase.PurchaseOrder>>();
            services.AddScoped<IRepository<CRM.Core.Models.Purchase.PurchaseOrderItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Purchase.PurchaseOrderItem>>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JwtSettings.Issuer,
                    ValidAudience = JwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.SecretKey))
                };
            });

            return services;
        }
    }
}
