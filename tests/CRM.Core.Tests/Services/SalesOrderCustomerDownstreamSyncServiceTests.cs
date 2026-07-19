using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class SalesOrderCustomerDownstreamSyncServiceTests
{
    private readonly IRepository<SellOrder> _soRepo;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IRepository<FinanceSellInvoice> _sellInvoiceRepo;
    private readonly IRepository<SellInvoiceItem> _sellInvoiceItemRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalesOrderCustomerDownstreamSyncService _service;

    public SalesOrderCustomerDownstreamSyncServiceTests()
    {
        _soRepo = Substitute.For<IRepository<SellOrder>>();
        _soItemRepo = Substitute.For<IRepository<SellOrderItem>>();
        _notifyRepo = Substitute.For<IRepository<StockOutRequest>>();
        _packingRepo = Substitute.For<IRepository<Packing>>();
        _packingItemRepo = Substitute.For<IRepository<PackingItem>>();
        _stockOutRepo = Substitute.For<IRepository<StockOut>>();
        _receivableRepo = Substitute.For<IRepository<FinanceReceivable>>();
        _sellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
        _sellInvoiceItemRepo = Substitute.For<IRepository<SellInvoiceItem>>();
        _stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
        _customerRepo = Substitute.For<IRepository<CustomerInfo>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.SaveChangesAsync().Returns(1);
        _service = new SalesOrderCustomerDownstreamSyncService(
            _soRepo,
            _soItemRepo,
            _notifyRepo,
            _packingRepo,
            _packingItemRepo,
            _stockOutRepo,
            _receivableRepo,
            _sellInvoiceRepo,
            _sellInvoiceItemRepo,
            _stockOutItemRepo,
            _customerRepo,
            _unitOfWork,
            NullLogger<SalesOrderCustomerDownstreamSyncService>.Instance);
    }

    [Fact]
    public async Task PreviewAsync_WhenNoMismatch_ShouldNoOp()
    {
        const string orderId = "SO-1";
        const string customerId = "CUST-OK";
        _soRepo.GetByIdAsync(orderId).Returns(new SellOrder
        {
            Id = orderId,
            SellOrderCode = "SO001",
            CustomerId = customerId,
            CustomerName = "正确客户"
        });
        _customerRepo.GetByIdAsync(customerId).Returns(new CustomerInfo
        {
            Id = customerId,
            OfficialName = "正确客户"
        });
        _soItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SellOrderItem, bool>>>())
            .Returns(Array.Empty<SellOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOutRequest, bool>>>())
            .Returns(Array.Empty<StockOutRequest>());
        _packingItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PackingItem, bool>>>())
            .Returns(Array.Empty<PackingItem>());
        _stockOutRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOut, bool>>>())
            .Returns(Array.Empty<StockOut>());
        _receivableRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceivable, bool>>>())
            .Returns(Array.Empty<FinanceReceivable>());

        var preview = await _service.PreviewAsync(orderId);

        Assert.True(preview.CanSync);
        Assert.True(preview.NoOp);
    }

    [Fact]
    public async Task ApplyAsync_ShouldSyncPendingStockOutNotify()
    {
        const string orderId = "SO-1";
        const string customerId = "CUST-NEW";
        var order = new SellOrder
        {
            Id = orderId,
            SellOrderCode = "SO001",
            CustomerId = customerId,
            CustomerName = "新客户"
        };
        var notify = new StockOutRequest
        {
            Id = "STOR-1",
            SalesOrderId = orderId,
            RequestCode = "STOR00001",
            CustomerId = "CUST-OLD",
            Status = StockOutRequestStatusCode.PendingPacking
        };

        _soRepo.GetByIdAsync(orderId).Returns(order);
        _customerRepo.GetByIdAsync(customerId).Returns(new CustomerInfo { Id = customerId, OfficialName = "新客户" });
        _soItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SellOrderItem, bool>>>())
            .Returns(Array.Empty<SellOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOutRequest, bool>>>())
            .Returns(new[] { notify });
        _packingItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PackingItem, bool>>>())
            .Returns(Array.Empty<PackingItem>());
        _stockOutRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOut, bool>>>())
            .Returns(Array.Empty<StockOut>());
        _receivableRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceivable, bool>>>())
            .Returns(Array.Empty<FinanceReceivable>());

        var result = await _service.ApplyAsync(order);

        Assert.True(result.Applied);
        Assert.Equal(customerId, notify.CustomerId);
        await _notifyRepo.Received(1).UpdateAsync(notify);
    }

    [Fact]
    public async Task PreviewAsync_WhenNotifyStockedOutMismatch_ShouldBlock()
    {
        const string orderId = "SO-1";
        _soRepo.GetByIdAsync(orderId).Returns(new SellOrder
        {
            Id = orderId,
            SellOrderCode = "SO001",
            CustomerId = "CUST-NEW"
        });
        _customerRepo.GetByIdAsync("CUST-NEW").Returns(new CustomerInfo { Id = "CUST-NEW", OfficialName = "新客户" });
        _soItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SellOrderItem, bool>>>())
            .Returns(Array.Empty<SellOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOutRequest, bool>>>())
            .Returns(new[]
            {
                new StockOutRequest
                {
                    Id = "STOR-1",
                    SalesOrderId = orderId,
                    RequestCode = "STOR00001",
                    CustomerId = "CUST-OLD",
                    Status = StockOutRequestStatusCode.StockedOut
                }
            });
        _packingItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PackingItem, bool>>>())
            .Returns(Array.Empty<PackingItem>());
        _stockOutRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOut, bool>>>())
            .Returns(Array.Empty<StockOut>());
        _receivableRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceivable, bool>>>())
            .Returns(Array.Empty<FinanceReceivable>());

        var preview = await _service.PreviewAsync(orderId);

        Assert.False(preview.CanSync);
        Assert.Contains(preview.BlockingDocuments, x => x.Contains("STOR00001"));
    }

    [Fact]
    public async Task PreviewAsync_WhenMismatchPending_ShouldListSyncItemsWithWrongCustomerName()
    {
        const string orderId = "SO-1";
        const string customerId = "CUST-NEW";
        const string oldCustomerId = "CUST-OLD";
        _soRepo.GetByIdAsync(orderId).Returns(new SellOrder
        {
            Id = orderId,
            SellOrderCode = "SO0022A",
            CustomerId = customerId,
            CustomerName = "新客户"
        });
        _customerRepo.GetByIdAsync(customerId).Returns(new CustomerInfo { Id = customerId, OfficialName = "深圳市固连制造科技有限公司" });
        _customerRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CustomerInfo, bool>>>())
            .Returns(callInfo =>
            {
                return new[]
                {
                    new CustomerInfo { Id = oldCustomerId, OfficialName = "错误客户有限公司" }
                };
            });
        _soItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SellOrderItem, bool>>>())
            .Returns(Array.Empty<SellOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOutRequest, bool>>>())
            .Returns(new[]
            {
                new StockOutRequest
                {
                    Id = "STOR-1",
                    SalesOrderId = orderId,
                    RequestCode = "STOR00001",
                    CustomerId = oldCustomerId,
                    Status = StockOutRequestStatusCode.PendingPacking
                }
            });
        _packingItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PackingItem, bool>>>())
            .Returns(Array.Empty<PackingItem>());
        _stockOutRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockOut, bool>>>())
            .Returns(Array.Empty<StockOut>());
        _receivableRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinanceReceivable, bool>>>())
            .Returns(Array.Empty<FinanceReceivable>());

        var preview = await _service.PreviewAsync(orderId);

        Assert.True(preview.CanSync);
        Assert.False(preview.NoOp);
        Assert.Single(preview.SyncItems);
        Assert.Equal("stockOutNotify", preview.SyncItems[0].Category);
        Assert.Equal("STOR00001", preview.SyncItems[0].DocumentCode);
        Assert.Equal("错误客户有限公司", preview.SyncItems[0].CustomerName);
        Assert.True(preview.SyncItems[0].IsMismatch);
    }
}
