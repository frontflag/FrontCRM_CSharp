using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.IntegrationTests
{
    public class BusinessWorkflowTests
    {
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IRepository<RFQItem> _rfqItemRepository;
        private readonly IRepository<Quote> _quoteRepository;
        private readonly IRepository<QuoteItem> _quoteItemRepository;
        private readonly IRepository<SellOrder> _salesOrderRepository;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RFQService _rfqService;
        private readonly QuoteService _quoteService;
        private readonly SalesOrderService _salesOrderService;

        public BusinessWorkflowTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _rfqItemRepository = Substitute.For<IRepository<RFQItem>>();
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _quoteItemRepository = Substitute.For<IRepository<QuoteItem>>();
            _salesOrderRepository = Substitute.For<IRepository<SellOrder>>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");
            _rfqService = new RFQService(_rfqRepository, _rfqItemRepository, null!, _unitOfWork, _serialNumberService);
            _quoteService = new QuoteService(_quoteRepository, _quoteItemRepository);
            _salesOrderService = new SalesOrderService(_salesOrderRepository);
        }

        [Fact]
        public async Task CompleteWorkflow_FromRFQToSalesOrder_ShouldSucceed()
        {
            var rfqRequest = new CreateRFQRequest { CustomerId = "CUST-001", SalesUserId = "USER-001", RfqDate = DateTime.UtcNow };
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());
            var rfq = await _rfqService.CreateAsync(rfqRequest);
            Assert.NotNull(rfq);
            Assert.Equal("RF20260001", rfq.RfqCode);
            Assert.Equal(0, rfq.Status);

            var quoteRequest = new CreateQuoteRequest { QuoteCode = "QT-2024-001", CustomerId = rfq.CustomerId, SalesUserId = rfq.SalesUserId, QuoteDate = DateTime.UtcNow, Mpn = "REF3430QDBVRQ1" };
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var quote = await _quoteService.CreateAsync(quoteRequest);
            Assert.NotNull(quote);
            Assert.Equal("QT-2024-001", quote.QuoteCode);
            Assert.Equal(0, quote.Status);

            _quoteRepository.GetByIdAsync(quote.Id).Returns(quote);
            await _quoteService.UpdateStatusAsync(quote.Id, 2);
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 2));

            var orderRequest = new CreateSalesOrderRequest { OrderCode = "SO-2024-001", CustomerId = quote.CustomerId, SalesUserId = quote.SalesUserId, DeliveryDate = DateTime.UtcNow.AddDays(7), TotalAmount = 10000m, GrandTotal = 11300m, Currency = 1, PaymentTerms = "30天账期" };
            _salesOrderRepository.GetAllAsync().Returns(new List<SellOrder>());
            var order = await _salesOrderService.CreateAsync(orderRequest);
            Assert.NotNull(order);
            Assert.Equal("SO-2024-001", order.SellOrderCode);
            Assert.Equal(10000m, order.Total);
            Assert.Equal(0, order.Status);

            _salesOrderRepository.GetByIdAsync(order.Id).Returns(order);
            await _salesOrderService.UpdateStatusAsync(order.Id, 1);
            await _salesOrderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 1));
        }

        [Fact]
        public async Task Workflow_QuoteRejected_ShouldNotCreateOrder()
        {
            var quote = new Quote { Id = "QT-001", QuoteCode = "QT-2024-002", CustomerId = "CUST-002", Status = 0 };
            _quoteRepository.GetByIdAsync(quote.Id).Returns(quote);
            await _quoteService.UpdateStatusAsync(quote.Id, 5);
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 5));
            Assert.NotEqual(2, quote.Status);
        }

        [Fact]
        public async Task GetCustomerDocuments_ShouldReturnAllRelatedDocuments()
        {
            var customerId = "CUST-003";
            var rfqs = new List<RFQ> { new() { Id = "RFQ-1", RfqCode = "RF001", CustomerId = customerId }, new() { Id = "RFQ-2", RfqCode = "RF002", CustomerId = customerId } };
            var quotes = new List<Quote> { new() { Id = "QT-1", QuoteCode = "QT-001", CustomerId = customerId }, new() { Id = "QT-2", QuoteCode = "QT-002", CustomerId = customerId }, new() { Id = "QT-3", QuoteCode = "QT-003", CustomerId = "OTHER" } };
            var orders = new List<SellOrder> { new() { Id = "SO-1", SellOrderCode = "SO-001", CustomerId = customerId } };
            _rfqRepository.GetAllAsync().Returns(rfqs);
            _quoteRepository.GetAllAsync().Returns(quotes);
            _salesOrderRepository.GetAllAsync().Returns(orders);
            var pagedRequest = new RFQQueryRequest { CustomerId = customerId, PageIndex = 1, PageSize = 20 };
            var rfqResult = await _rfqService.GetPagedAsync(pagedRequest);
            var allQuotes = await _quoteService.GetAllAsync();
            var customerQuotes = allQuotes.Where(q => q.CustomerId == customerId).ToList();
            var allOrders = await _salesOrderService.GetAllAsync();
            var customerOrders = allOrders.Where(o => o.CustomerId == customerId).ToList();
            Assert.Equal(2, rfqResult.TotalCount);
            Assert.Equal(2, customerQuotes.Count);
            Assert.Single(customerOrders);
        }

        [Fact]
        public async Task OrderStatusFlow_DraftToCompleted_ShouldSucceed()
        {
            var orderId = "SO-001";
            var order = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-003", CustomerId = "CUST-001", Status = 0 };
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);
            await _salesOrderService.UpdateStatusAsync(orderId, 1);
            Assert.Equal(1, order.Status);
            order.Status = 1;
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);
            await _salesOrderService.UpdateStatusAsync(orderId, 2);
            Assert.Equal(2, order.Status);
            order.Status = 2;
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);
            await _salesOrderService.UpdateStatusAsync(orderId, 3);
            Assert.Equal(3, order.Status);
            await _salesOrderRepository.Received(3).UpdateAsync(Arg.Any<SellOrder>());
        }

        [Fact]
        public async Task CreateDuplicateDocument_ShouldThrowException()
        {
            var existingQuotes = new List<Quote> { new() { Id = "1", QuoteCode = "QT-DUP", CustomerId = "C1" } };
            _quoteRepository.GetAllAsync().Returns(existingQuotes);
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var request = new CreateQuoteRequest { QuoteCode = "QT-DUP", CustomerId = "C2" };
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _quoteService.CreateAsync(request));
            Assert.Contains("QT-DUP", exception.Message);
        }

        [Fact]
        public async Task RequestStockOut_ForConfirmedOrder_ShouldSucceed()
        {
            var orderId = "SO-123";
            var order = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-004", CustomerId = "CUST-001", Status = 1, StockOutStatus = 0 };
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);
            await _salesOrderService.RequestStockOutAsync(orderId, "USER-001");
            await _salesOrderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.StockOutStatus == 1));
        }
    }
}
