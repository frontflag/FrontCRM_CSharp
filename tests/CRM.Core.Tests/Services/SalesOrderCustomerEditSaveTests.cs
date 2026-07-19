using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Core.Tests.Services;

/// <summary>
/// 销售订单编辑保存 — 客户 ID / 名称与主表、明细列表展示一致性（黄金路径回归）。
/// </summary>
public class SalesOrderCustomerEditSaveTests
{
    private const string OrderId = "SO-EDIT-1";
    private const string LineId = "SO-EDIT-1-1";

    [Fact]
    public async Task UpdateAsync_OnlyCustomerNameWithoutCustomerId_ShouldNotChangeCustomerId()
    {
        var ctx = CreateContext();
        ctx.Order.CustomerId = "CUST-OLD";
        ctx.Order.CustomerName = "旧客户";

        var result = await ctx.Service.UpdateAsync(OrderId, new UpdateSalesOrderRequest
        {
            CustomerName = "仅改快照名称"
        });

        Assert.Equal("CUST-OLD", result.CustomerId);
        Assert.Equal("仅改快照名称", result.CustomerName);
    }

    [Fact]
    public async Task AfterCustomerIdChange_MainListAndItemLineList_ShouldExposeSameCustomerName()
    {
        var ctx = CreateContext();
        ctx.Order.CustomerId = "CUST-OLD";
        ctx.Order.CustomerName = "旧客户有限公司";
        SeedCustomers(ctx);

        await ctx.Service.UpdateAsync(OrderId, new UpdateSalesOrderRequest
        {
            CustomerId = "CUST-NEW",
            CustomerName = "表单里可能过期的标签"
        });

        var mainPage = await ctx.Service.GetPagedAsync(new SalesOrderQueryRequest { Page = 1, PageSize = 20 });
        var itemPage = await ctx.Service.GetSellOrderItemLinesPagedAsync(new SellOrderItemLineQueryRequest
        {
            Page = 1,
            PageSize = 20
        });

        var mainName = mainPage.Items.Single().CustomerName;
        var itemName = itemPage.Items.Single().CustomerName;

        Assert.Equal("新客户有限公司", mainName);
        Assert.Equal("新客户有限公司", itemName);
        Assert.Equal(mainName, itemName);
    }

    /// <summary>
    /// 记录修复前故障模式：仅改 <c>customer_name</c> 快照、未改 <c>customer_id</c> 时，主表 enrich 与明细快照会不一致。
    /// 编辑页换客户时必须提交 <c>customerId</c>，不得依赖本路径。
    /// </summary>
    [Fact]
    public async Task WhenOnlyCustomerNameUpdated_WithoutCustomerId_MainListAndItemLineListCanDiffer()
    {
        var ctx = CreateContext();
        ctx.Order.CustomerId = "CUST-OLD";
        ctx.Order.CustomerName = "旧客户有限公司";
        SeedCustomers(ctx);

        await ctx.Service.UpdateAsync(OrderId, new UpdateSalesOrderRequest
        {
            CustomerName = "错误快照名称"
        });

        var mainPage = await ctx.Service.GetPagedAsync(new SalesOrderQueryRequest { Page = 1, PageSize = 20 });
        var itemPage = await ctx.Service.GetSellOrderItemLinesPagedAsync(new SellOrderItemLineQueryRequest
        {
            Page = 1,
            PageSize = 20
        });

        Assert.Equal("旧客户有限公司", mainPage.Items.Single().CustomerName);
        Assert.Equal("错误快照名称", itemPage.Items.Single().CustomerName);
        Assert.NotEqual(mainPage.Items.Single().CustomerName, itemPage.Items.Single().CustomerName);
    }

    private static void SeedCustomers(TestContext ctx)
    {
        ctx.Customers.AddRange(
            new CustomerInfo { Id = "CUST-OLD", OfficialName = "旧客户有限公司", CustomerCode = "OLD" },
            new CustomerInfo { Id = "CUST-NEW", OfficialName = "新客户有限公司", CustomerCode = "NEW" });

        ctx.CustomerRepository.GetByIdAsync("CUST-NEW").Returns(ctx.Customers.Single(c => c.Id == "CUST-NEW"));
        ctx.CustomerRepository.FindAsNoTrackingAsync(Arg.Any<Expression<Func<CustomerInfo, bool>>>())
            .Returns(call =>
            {
                var pred = call.Arg<Expression<Func<CustomerInfo, bool>>>().Compile();
                return Task.FromResult<IEnumerable<CustomerInfo>>(ctx.Customers.Where(pred).ToList());
            });
        ctx.CustomerRepository.FindAsync(Arg.Any<Expression<Func<CustomerInfo, bool>>>())
            .Returns(call =>
            {
                var pred = call.Arg<Expression<Func<CustomerInfo, bool>>>().Compile();
                return Task.FromResult<IEnumerable<CustomerInfo>>(ctx.Customers.Where(pred).ToList());
            });
    }

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.Order = new SellOrder
        {
            Id = OrderId,
            SellOrderCode = "SO-TEST-001",
            CustomerId = "CUST-OLD",
            CustomerName = "旧客户"
        };

        ctx.OrderRepository.GetByIdAsync(OrderId).Returns(_ => ctx.Order);
        ctx.OrderRepository.UpdateAsync(Arg.Any<SellOrder>()).Returns(call =>
        {
            ctx.Order = call.Arg<SellOrder>();
            return Task.CompletedTask;
        });
        ctx.OrderRepository.FindAsync(Arg.Any<Expression<Func<SellOrder, bool>>>())
            .Returns(call =>
            {
                var pred = call.Arg<Expression<Func<SellOrder, bool>>>().Compile();
                return Task.FromResult<IEnumerable<SellOrder>>(
                    new[] { ctx.Order }.Where(pred).ToList());
            });

        ctx.SalesOrderListQuery.GetPagedAsync(Arg.Any<SalesOrderQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new PagedResult<SellOrder>
            {
                Items = new List<SellOrder> { CloneOrder(ctx.Order) },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 20
            }));

        ctx.SalesOrderItemLineListQuery.GetPagedAsync(Arg.Any<SellOrderItemLineQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new PagedResult<SellOrderItemLineDto>
            {
                Items = new List<SellOrderItemLineDto>
                {
                    new()
                    {
                        SellOrderItemId = LineId,
                        SellOrderId = ctx.Order.Id,
                        SellOrderCode = ctx.Order.SellOrderCode ?? string.Empty,
                        CustomerId = ctx.Order.CustomerId,
                        CustomerName = ctx.Order.CustomerName
                    }
                },
                TotalCount = 1,
                PageIndex = 1,
                PageSize = 20
            }));

        ctx.Service = new SalesOrderService(
            ctx.OrderRepository,
            ctx.OrderItemRepository,
            ctx.SoItemExtendRepository,
            ctx.PoRepository,
            ctx.PoItemRepository,
            ctx.PrRepository,
            ctx.CustomerRepository,
            ctx.QuoteItemRepository,
            Substitute.For<IDataPermissionService>(),
            Substitute.For<ISerialNumberService>(),
            Substitute.For<IFinanceExchangeRateService>(),
            Substitute.For<IOrderJourneyLogService>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<ISellOrderMainStatusSyncService>(),
            Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(),
            Substitute.For<ISellOrderExtendLineSeqService>(),
            Substitute.For<IUserService>(),
            ctx.SalesOrderListQuery,
            ctx.SalesOrderItemLineListQuery,
            Substitute.For<ILogOperationAppendService>(),
            ctx.UnitOfWork,
            NullLogger<SalesOrderService>.Instance,
            Substitute.For<IQuoteStatusSyncService>());

        return ctx;
    }

    private static SellOrder CloneOrder(SellOrder source) => new()
    {
        Id = source.Id,
        SellOrderCode = source.SellOrderCode,
        CustomerId = source.CustomerId,
        CustomerName = source.CustomerName
    };

    private sealed class TestContext
    {
        public SellOrder Order { get; set; } = null!;
        public List<CustomerInfo> Customers { get; } = new();
        public IRepository<SellOrder> OrderRepository { get; } = Substitute.For<IRepository<SellOrder>>();
        public IRepository<SellOrderItem> OrderItemRepository { get; } = Substitute.For<IRepository<SellOrderItem>>();
        public IRepository<SellOrderItemExtend> SoItemExtendRepository { get; } = Substitute.For<IRepository<SellOrderItemExtend>>();
        public IRepository<PurchaseOrder> PoRepository { get; } = Substitute.For<IRepository<PurchaseOrder>>();
        public IRepository<PurchaseOrderItem> PoItemRepository { get; } = Substitute.For<IRepository<PurchaseOrderItem>>();
        public IRepository<PurchaseRequisition> PrRepository { get; } = Substitute.For<IRepository<PurchaseRequisition>>();
        public IRepository<CustomerInfo> CustomerRepository { get; } = Substitute.For<IRepository<CustomerInfo>>();
        public IRepository<QuoteItem> QuoteItemRepository { get; } = Substitute.For<IRepository<QuoteItem>>();
        public ISalesOrderListQuery SalesOrderListQuery { get; } = Substitute.For<ISalesOrderListQuery>();
        public ISalesOrderItemLineListQuery SalesOrderItemLineListQuery { get; } = Substitute.For<ISalesOrderItemLineListQuery>();
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public SalesOrderService Service { get; set; } = null!;

        public TestContext()
        {
            UnitOfWork.SaveChangesAsync().Returns(1);
            OrderItemRepository.FindAsync(Arg.Any<Expression<Func<SellOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SellOrderItem>>(Array.Empty<SellOrderItem>()));
        }
    }
}
