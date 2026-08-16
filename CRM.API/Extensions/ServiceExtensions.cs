using CRM.API.Services.Interfaces;
using CRM.API.Services.Implementations;
using CRM.Core.Constants;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Services;
using CRM.Core.Services.RfqAssignment;
using CRM.Core.Services.InternalTransfer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CRM.Infrastructure.Extensions;
using CRM.Infrastructure.Document;
using CRM.Infrastructure.Services;
using IP2Region.Net.Abstractions;
using IP2Region.Net.XDB;
using Microsoft.Extensions.Hosting;

namespace CRM.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("数据库连接字符串未配置。请在 appsettings.json 中配置 ConnectionStrings:DefaultConnection。");
            }

            services.AddInfrastructure(connectionString);

            services.AddHttpContextAccessor();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ICustomerIntelReportService, CustomerIntelReportService>();
            services.AddScoped<IVendorIntelReportService, VendorIntelReportService>();
            services.AddScoped<IApprovalPartyIntelWarmupService, ApprovalPartyIntelWarmupService>();
            services.AddScoped<IEntityLookupService, EntityLookupService>();
            services.AddScoped<IRFQService, RFQService>();
            services.AddScoped<IRfqPurchaserRoundRobinCursorStore, RfqPurchaserRoundRobinCursorStore>();
            services.AddScoped<RfqPurchaserRoundRobinPicker>();
            services.AddScoped<IRfqPurchaserAssignStrategy, ItemRoundRobinPurchaserAssignStrategy>();
            services.AddScoped<IRfqPurchaserAssignStrategy, SameBrandPurchaserAssignStrategy>();
            services.AddScoped<IRfqPurchaserAssignStrategy, PurchaseQuotePriorityPurchaserAssignStrategy>();
            services.AddScoped<IRfqPurchaserAssignmentOrchestrator, RfqPurchaserAssignmentOrchestrator>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQ>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQ>>();
            services.AddScoped<IRepository<CRM.Core.Models.RFQ.RFQItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.RFQ.RFQItem>>();
            services.AddScoped<IQuoteService, QuoteService>();
            services.AddScoped<IQuoteStatusSyncService, QuoteStatusSyncService>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.Quote>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.Quote>>();
            services.AddScoped<IRepository<CRM.Core.Models.Quote.QuoteItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Quote.QuoteItem>>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IStockInService, StockInService>();
            services.AddScoped<IStockInBatchService, StockInBatchService>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockInBatch>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockInBatch>>();
            services.AddScoped<IStockOutBatchService, StockOutBatchService>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockOutBatch>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockOutBatch>>();
            services.AddScoped<IDisplayTimeZoneService, DisplayTimeZoneService>();
            services.AddScoped<IDictionaryService, DictionaryService>();
            services.AddScoped<ISysDictItemAdminService, SysDictItemAdminService>();
            services.AddScoped<IBizBrandService, CRM.Infrastructure.Biz.BizBrandService>();
            services.AddScoped<IStockOutService, StockOutService>();
            services.AddScoped<IPackingService, CRM.Infrastructure.Packings.PackingService>();
            services.AddScoped<IPackingStatusReconcileService, CRM.Infrastructure.Packings.PackingStatusReconcileService>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.Packing>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.Packing>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.PackingItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.PackingItem>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.PickingTask>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.PickingTask>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.PickingTaskItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.PickingTaskItem>>();
            services.AddScoped<IRepository<CRM.Core.Models.Inventory.StockItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Inventory.StockItem>>();
            services.AddScoped<ICustomsBrokerService, CustomsBrokerService>();
            services.AddScoped<ICustomsDeclarationService, CustomsDeclarationService>();
            services.AddScoped<ICustomsPendlistService, CustomsPendlistService>();
            services.AddScoped<ICustomsPendlistFlowService, CustomsPendlistFlowService>();
            services.AddScoped<ICustomsV2FlowService, CustomsV2FlowService>();
            services.AddScoped<ICustomsFeeCalculator, CustomsFeeCalculator>();
            services.AddScoped<IPurchaseCostParamService, PurchaseCostParamService>();
            services.AddScoped<IRepository<CRM.Core.Models.Customs.PurchaseCostParam>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Customs.PurchaseCostParam>>();
            services.AddScoped<IRepository<CRM.Core.Models.Customs.PurchaseCostParamChangeLog>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Customs.PurchaseCostParamChangeLog>>();
            services.AddScoped<ISellOrderMainStatusSyncService, SellOrderMainStatusSyncService>();
            services.AddScoped<IPurchaseOrderMainStatusSyncService, PurchaseOrderMainStatusSyncService>();
            services.AddScoped<ISellOrderItemExtendSyncService, SellOrderItemExtendSyncService>();
            services.AddScoped<ISellOrderItemPurchasedStockAvailableSyncService, SellOrderItemPurchasedStockAvailableSyncService>();
            services.AddScoped<IPurchaseOrderItemExtendSyncService, PurchaseOrderItemExtendSyncService>();
            services.AddScoped<ILogisticsService, LogisticsService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IInventoryCenterService, InventoryCenterService>();
            services.AddScoped<IInternalTransferPostingKernel, InternalTransferPostingKernel>();
            services.AddScoped<IManualStockTransferService, ManualStockTransferService>();

            // 标签系统
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<ITagApplyService, TagApplyService>();
            services.AddScoped<ITagFilterService, TagFilterService>();
            services.AddScoped<IRfqTagService, RfqTagService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<ILogRecentService, LogRecentService>();
            services.AddScoped<ILogOperationAppendService, LogOperationAppendService>();
            services.AddScoped<IExportOperationLogService, ExportOperationLogService>();
            services.AddScoped<ILoginLogService, LoginLogService>();

            var xdbRel = configuration["Ip2Region:Ipv4XdbPath"] ?? "../data/ip2region/ip2region_v4.xdb";
            var xdbFull = Path.IsPathRooted(xdbRel)
                ? xdbRel
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, xdbRel));
            if (File.Exists(xdbFull))
                services.AddIP2RegionService(xdbFull, CachePolicy.VectorIndex);
            else
                services.AddSingleton<ISearcher, NullIpSearcher>();
            services.AddScoped<IDraftService, DraftService>();
            services.AddScoped<IRbacService, RbacService>();
            services.AddScoped<ISysRelationMapService, CRM.Infrastructure.RelationMaps.SysRelationMapService>();
            services.AddScoped<IPurchaseQuoterPoolService, CRM.Infrastructure.PurchaseParams.PurchaseQuoterPoolService>();
            services.AddScoped<ISalesParamsService, CRM.Infrastructure.SalesParams.SalesParamsService>();
            services.AddScoped<IDataPermissionService, DataPermissionService>();
            services.AddScoped<IApprovalRecordService, ApprovalRecordService>();
            services.AddScoped<IOrderJourneyLogService, OrderJourneyLogService>();
            services.AddScoped<IRepository<CRM.Core.Models.System.OrderJourneyLog>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.System.OrderJourneyLog>>();
            services.AddDocumentModule(configuration);

            // 销售看板
            services.AddScoped<ISalesAnalyticsService, SalesAnalyticsService>();
            services.AddScoped<ISalesAnalyticsReconciliationService, SalesAnalyticsReconciliationService>();
            services.AddScoped<IPurchaseAnalyticsService, PurchaseAnalyticsService>();
            services.AddScoped<ILogisticsAnalyticsService, LogisticsAnalyticsService>();
            services.AddScoped<IFinanceAnalyticsService, FinanceAnalyticsService>();
            services.AddScoped<IRepository<CRM.Core.Models.Rbac.RbacDepartment>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Rbac.RbacDepartment>>();
            services.AddScoped<IRepository<CRM.Core.Models.Rbac.RbacUserDepartment>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Rbac.RbacUserDepartment>>();

            // 销售订单模块
            services.AddScoped<ISalesOrderCustomerDownstreamSyncService, SalesOrderCustomerDownstreamSyncService>();
            services.AddScoped<ISalesOrderSalesPriceDownstreamSyncService, SalesOrderSalesPriceDownstreamSyncService>();
            services.AddScoped<ISalesOrderService, SalesOrderService>();
            services.AddScoped<ISalesOrderJourneyService, SalesOrderJourneyService>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrder>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrder>>();
            services.AddScoped<IRepository<CRM.Core.Models.Sales.SellOrderItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Sales.SellOrderItem>>();

            // 采购订单模块
            services.AddScoped<IPurchaseOrderVendorChangeService, PurchaseOrderVendorChangeService>();
            services.AddScoped<IPurchaseOrderPurchasePriceDownstreamSyncService, PurchaseOrderPurchasePriceDownstreamSyncService>();
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
            services.AddScoped<IFreightForwarderCompanyService, FreightForwarderCompanyService>();
            services.AddScoped<IFinanceFreightForwarderPayableService, FinanceFreightForwarderPayableService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FreightForwarderCompany>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FreightForwarderCompany>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FreightForwarderCompanyBank>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FreightForwarderCompanyBank>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceFreightForwarderPayment>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceFreightForwarderPayment>>();
            services.AddScoped<IFinanceReceivableService, FinanceReceivableService>();
            services.AddScoped<IFinanceCustomerAdvanceService, FinanceCustomerAdvanceService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceReceivable>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceReceivable>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceReceivableWriteOff>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceReceivableWriteOff>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceCustomerAdvance>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceCustomerAdvance>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceCustomerAdvanceLedger>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceCustomerAdvanceLedger>>();
            // 财务模块 - 进项发票
            services.AddScoped<IFinancePurchaseInvoiceService, FinancePurchaseInvoiceService>();
            services.AddScoped<IFinancePurchaseInvoiceWriteOffService, FinancePurchaseInvoiceWriteOffService>();
            services.AddScoped<IFinancePurchaseInvoicePaymentSyncService, FinancePurchaseInvoicePaymentSyncService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePurchaseInvoice>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePurchaseInvoice>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePurchaseInvoiceItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePurchaseInvoiceItem>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePurchaseInvoiceWriteOff>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePurchaseInvoiceWriteOff>>();
            // 财务模块 - 销项发票
            services.AddScoped<IFinanceSellInvoiceService, FinanceSellInvoiceService>();
            services.AddScoped<IFinanceSellInvoiceWriteOffService, FinanceSellInvoiceWriteOffService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceSellInvoice>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceSellInvoice>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.SellInvoiceItem>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.SellInvoiceItem>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceSellInvoiceWriteOff>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceSellInvoiceWriteOff>>();
            services.AddScoped<IFinanceExchangeRateService, FinanceExchangeRateService>();
            services.AddScoped<IFinancePaymentBankService, FinancePaymentBankService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinancePaymentBank>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinancePaymentBank>>();
            services.AddScoped<IForceDeleteGuardService, ForceDeleteGuardService>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceExchangeRateSetting>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceExchangeRateSetting>>();
            services.AddScoped<IRepository<CRM.Core.Models.Finance.FinanceExchangeRateChangeLog>, CRM.Infrastructure.Repositories.Repository<CRM.Core.Models.Finance.FinanceExchangeRateChangeLog>>();

            // 微信认证
            services.AddScoped<IWechatAuthService, WechatAuthService>();
            services.AddScoped<IWechatLoginTicketRepository, CRM.Infrastructure.Repositories.WechatLoginTicketRepository>();
            services.AddScoped<IWechatBindRequestRepository, CRM.Infrastructure.Repositories.WechatBindRequestRepository>();
            services.AddHttpClient();

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddSingleton<IMailboxPasswordCipher, MailboxPasswordCipher>();
            services.AddScoped<IMailboxVerifyService, MailboxVerifyService>();
            services.AddScoped<IMailboxSendService, MailboxSendService>();
            services.AddScoped<IUserMailSyncService, UserMailSyncService>();

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
                    Description =
                        "FrontCRM 智能进销存管理系统 API。\n\n" +
                        "**强制删除**：所有 `POST …/force-delete` 须在 JSON 体中提交 **confirmBillCode**，且与对应业务单号完全一致（进项=InvoiceNo，销项=InvoiceCode，其余见仓库文档 `document/实现方案/强制删除_API契约与错误码.md`）。\n" +
                        "成功响应多为 **ApiResponse**（success/message/data/errorCode）或财务/采购申请的 **{ success, message }**；错误时优先展示响应体 **message**（与前端 `getApiErrorMessage` 一致）。\n" +
                        "多数强制删除仅 **系统管理员** 可调用。",
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

            services.AddHostedService<CRM.API.Services.TelemetryCleanupHostedService>();
            services.AddHostedService<CRM.API.Services.MailSyncHostedService>();

            return services;
        }
    }
}
