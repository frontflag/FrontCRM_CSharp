using CRM.API.Services.Interfaces;
using CRM.API.Services.Implementations;
using CRM.Core.Constants;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CRM.Infrastructure.Extensions;
using CRM.Infrastructure.Document;

namespace CRM.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("数据库连接字符串未配置。请在 appsettings.json 中配置 ConnectionStrings:DefaultConnection。");
            }

            services.AddInfrastructure(connectionString);

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IEntityLookupService, EntityLookupService>();
            services.AddScoped<IRFQService, RFQService>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQ>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQ>>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQItem>>();
            services.AddScoped<IQuoteService, QuoteService>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.Quote>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.Quote>>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.QuoteItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.QuoteItem>>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IStockInService, StockInService>();
            services.AddScoped<IStockInBatchService, StockInBatchService>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockInBatch>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockInBatch>>();
            services.AddScoped<IDisplayTimeZoneService, DisplayTimeZoneService>();
            services.AddScoped<IDictionaryService, DictionaryService>();
            services.AddScoped<ISysDictItemAdminService, SysDictItemAdminService>();
            services.AddScoped<IStockOutService, StockOutService>();
            services.AddScoped<ICustomsBrokerService, CustomsBrokerService>();
            services.AddScoped<ICustomsDeclarationService, CustomsDeclarationService>();
            services.AddScoped<ISellOrderItemExtendSyncService, SellOrderItemExtendSyncService>();
            services.AddScoped<ISellOrderItemPurchasedStockAvailableSyncService, SellOrderItemPurchasedStockAvailableSyncService>();
            services.AddScoped<IPurchaseOrderItemExtendSyncService, PurchaseOrderItemExtendSyncService>();
            services.AddScoped<ILogisticsService, LogisticsService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IInventoryCenterService, InventoryCenterService>();

            // 标签系统
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<ITagApplyService, TagApplyService>();
            services.AddScoped<ITagFilterService, TagFilterService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<ILogRecentService, LogRecentService>();
            services.AddScoped<IOperationLogQueryService, OperationLogQueryService>();
            services.AddScoped<IDraftService, DraftService>();
            services.AddScoped<IRbacService, RbacService>();
            services.AddScoped<IDataPermissionService, DataPermissionService>();
            services.AddScoped<IApprovalRecordService, ApprovalRecordService>();
            services.AddScoped<IOrderJourneyLogService, OrderJourneyLogService>();
            services.AddScoped<IRepository<CRM.Core.Models.System.OrderJourneyLog>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.System.OrderJourneyLog>>();
            services.AddDocumentModule(configuration);

            // 销售订单模块
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<ISalesOrderJourneyService, SalesOrderJourneyService>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrder>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrder>>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrderItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrderItem>>();

            // 采购订单模块
            services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
            services.AddScoped<IRepository<CRM.Core.Models.Purchase.PurchaseOrder>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Purchase.PurchaseOrder>>();
            services.AddScoped<IRepository<CRM.Core.Models.Purchase.PurchaseOrderItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Purchase.PurchaseOrderItem>>();

            // 采购申请模块
            services.AddScoped<IPurchaseRequisitionService, PurchaseRequisitionService>();
            services.AddScoped<IRepository<CRM.Core.Models.Purchase.PurchaseRequisition>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Purchase.PurchaseRequisition>>();

            // 旅程聚合需要的仓储（库存/物流）
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockOutRequest>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockOutRequest>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockInNotify>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockInNotify>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.QCInfo>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.QCInfo>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockIn>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockIn>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockOut>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockOut>>();

            // 财务模块 - 付款
            services.AddScoped<IFinancePaymentService, FinancePaymentService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePayment>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePayment>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePaymentItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePaymentItem>>();
            // 财务模块 - 收款
            services.AddScoped<IFinanceReceiptService, FinanceReceiptService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceReceipt>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceReceipt>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceReceiptItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceReceiptItem>>();
            // 财务模块 - 进项发票
            services.AddScoped<IFinancePurchaseInvoiceService, FinancePurchaseInvoiceService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePurchaseInvoice>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePurchaseInvoice>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePurchaseInvoiceItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePurchaseInvoiceItem>>();
            // 财务模块 - 销项发票
            services.AddScoped<IFinanceSellInvoiceService, FinanceSellInvoiceService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceSellInvoice>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceSellInvoice>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.SellInvoiceItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.SellInvoiceItem>>();
            services.AddScoped<IFinanceExchangeRateService, FinanceExchangeRateService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceExchangeRateSetting>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceExchangeRateSetting>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceExchangeRateChangeLog>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceExchangeRateChangeLog>>();

            // 微信认证
            services.AddScoped<IWechatAuthService, WechatAuthService>();
            services.AddScoped<IWechatLoginTicketRepository, CRM.Infrastructure.Repositories.WechatLoginTicketRepository>();
            services.AddScoped<IWechatBindRequestRepository, CRM.Infrastructure.Repositories.WechatBindRequestRepository>();
            services.AddHttpClient();

            services.AddScoped<IEmailSender, SmtpEmailSender>();

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

            // Swagger 配置
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FrontCRM API",
                    Version = "v1",
                    Description = "FrontCRM 智能进销存管理系统 API",
                    Contact = new OpenApiContact
                    {
                        Name = "FrontCRM Team"
                    }
                });

                // 添加 JWT 认证支持
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. \n\n请输入: Bearer {你的JWT令牌}\n\n例如: Bearer eyJhbGciOiJIUzI1NiIs...",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            return services;
        }
    }
}
