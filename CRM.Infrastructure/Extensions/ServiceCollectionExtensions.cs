using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.PurchaseOrders;
using CRM.Infrastructure.RfqListQueries;
using CRM.Infrastructure.SalesOrders;
using CRM.Infrastructure.Quotes;
using CRM.Infrastructure.Customers;
using CRM.Infrastructure.Vendors;
using CRM.Infrastructure.PurchaseRequisitions;
using CRM.Infrastructure.Logistics;
using CRM.Infrastructure.StockIns;
using CRM.Infrastructure.StockOuts;
using CRM.Infrastructure.InventoryCenter;
using CRM.Infrastructure.Finance;
using CRM.Infrastructure.SystemLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(StockItemStockOutStatusMaterializationInterceptor.Instance);
                // 开发场景：数据库可能已经手动创建/对齐，但 EF 迁移快照与模型暂时不完全一致
                // PendingModelChangesWarning 会导致 Program.cs 里的 Database.Migrate() 直接抛异常终止进程。
                // 这里先忽略该警告，确保服务能正常启动进行业务验证。
                options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            services.AddScoped<IPurchaseOrderListQuery, PurchaseOrderListQuery>();
            services.AddScoped<IPurchaseOrderItemListQuery, PurchaseOrderItemListQuery>();
            services.AddScoped<ISalesOrderListQuery, SalesOrderListQuery>();
            services.AddScoped<ISalesOrderItemLineListQuery, SalesOrderItemLineListQuery>();
            services.AddScoped<IRfqMainListQuery, RfqMainListQuery>();
            services.AddScoped<IRfqItemListQuery, RfqItemListQuery>();
            services.AddScoped<IQuoteListQuery, QuoteListQuery>();
            services.AddScoped<ICustomerListQuery, CustomerListQuery>();
            services.AddScoped<IVendorListQuery, VendorListQuery>();
            services.AddScoped<IPurchaseRequisitionListQuery, PurchaseRequisitionListQuery>();
            services.AddScoped<IStockInBatchListQuery, global::CRM.Infrastructure.StockInBatches.StockInBatchListQuery>();
            services.AddScoped<IArrivalNoticeListQuery, ArrivalNoticeListQuery>();
            services.AddScoped<IQcListQuery, QcListQuery>();
            services.AddScoped<IStockInListQuery, StockInListQuery>();
            services.AddScoped<IStockOutListQuery, StockOutListQuery>();
            services.AddScoped<IStockOutRequestListQuery, StockOutRequestListQuery>();
            services.AddScoped<IStockOutItemListQuery, StockOutItemEfListQuery>();
            services.AddScoped<IInventoryStockItemListQuery, InventoryStockItemEfListQuery>();
            services.AddScoped<IInventoryMaterialOverviewStockPageQuery, InventoryMaterialOverviewStockPageQuery>();
            services.AddScoped<IInventoryCountPlanListQuery, InventoryCountPlanListQuery>();
            services.AddScoped<IFinancePaymentListQuery, FinancePaymentListQuery>();
            services.AddScoped<IFinancePurchaseInvoiceListQuery, FinancePurchaseInvoiceListQuery>();
            services.AddScoped<IFinanceReceiptListQuery, FinanceReceiptListQuery>();
            services.AddScoped<IFinanceSellInvoiceListQuery, FinanceSellInvoiceListQuery>();
            services.AddScoped<ILoginLogQueryService, LoginLogListQuery>();
            services.AddScoped<IOperationLogQueryService, OperationLogListQuery>();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 注册流水号服务和错误日志服务
            services.AddScoped<ISerialNumberService, SerialNumberService>();
            services.AddScoped<ISellOrderExtendLineSeqService, SellOrderExtendLineSeqService>();
            services.AddScoped<IPurchaseOrderExtendLineSeqService, PurchaseOrderExtendLineSeqService>();
            services.AddScoped<IStockInExtendLineSeqService, StockInExtendLineSeqService>();
            services.AddScoped<IStockExtendLineSeqService, StockExtendLineSeqService>();
            services.AddScoped<IErrorLogService, ErrorLogService>();

            // 注册物料数据服务（当前使用 Mock 实现，待 Nexar API 就绪后替换为 NexarComponentDataService）
            // 替换方式：将下面一行改为 services.AddScoped<IComponentDataService, NexarComponentDataService>();
            services.AddScoped<IComponentDataService, MockComponentDataService>();
            services.AddScoped<IComponentCacheService, ComponentCacheService>();

            return services;
        }
    }
}
