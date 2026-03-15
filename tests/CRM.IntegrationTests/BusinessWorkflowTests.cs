using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.IntegrationTests
{
    /// <summary>
    /// 核心业务流集成测试
    /// 测试流程: RFQ(询价) -> Quote(报价) -> SalesOrder(订单)
    /// </summary>
    public class BusinessWorkflowTests
    {
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IRepository<Quote> _quoteRepository;
        private readonly IRepository<SellOrder> _salesOrderRepository;

        private readonly RFQService _rfqService;
        private readonly QuoteService _quoteService;
        private readonly SalesOrderService _salesOrderService;

        public BusinessWorkflowTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _salesOrderRepository = Substitute.For<IRepository<SellOrder>>();

            _rfqService = new RFQService(_rfqRepository);
            _quoteService = new QuoteService(_quoteRepository);
            _salesOrderService = new SalesOrderService(_salesOrderRepository);
        }

        /// <summary>
        /// 完整业务流程测试: 创建询价单 -> 创建报价单 -> 审批报价单 -> 创建销售订单
        /// </summary>
        [Fact]
        public async Task CompleteWorkflow_FromRFQToSalesOrder_ShouldSucceed()
        {
            // ========== 步骤1: 创建RFQ询价单 ==========
            var rfqRequest = new CreateRFQRequest
            {
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                RFQDate = DateTime.UtcNow
            };

            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());

            RFQ? createdRFQ = null;
            _rfqRepository.When(r => r.AddAsync(Arg.Any<RFQ>()))
                .Do(call => createdRFQ = call.Arg<RFQ>());

            var rfq = await _rfqService.CreateAsync(rfqRequest);

            Assert.NotNull(rfq);
            Assert.Equal("RFQ-2024-001", rfq.RFQCode);
            Assert.Equal(0, rfq.Status); // 草稿
            Assert.NotNull(rfq.Id);

            // ========== 步骤2: 基于询价单创建报价单 ==========
            var quoteRequest = new CreateQuoteRequest
            {
                QuoteCode = "QT-2024-001",
                CustomerId = rfq.CustomerId,
                SalesUserId = rfq.SalesUserId,
                QuoteDate = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(30),
                TotalAmount = 10000m,
                TaxAmount = 1300m,
                GrandTotal = 11300m
            };

            _quoteRepository.GetAllAsync().Returns(new List<Quote>());

            Quote? createdQuote = null;
            _quoteRepository.When(r => r.AddAsync(Arg.Any<Quote>()))
                .Do(call => createdQuote = call.Arg<Quote>());

            var quote = await _quoteService.CreateAsync(quoteRequest);

            Assert.NotNull(quote);
            Assert.Equal("QT-2024-001", quote.QuoteCode);
            Assert.Equal(rfq.CustomerId, quote.CustomerId);
            Assert.Equal(11300m, quote.TotalAmountWithTax);
            Assert.Equal(0, quote.Status); // 草稿

            // ========== 步骤3: 审批报价单 ==========
            _quoteRepository.GetByIdAsync(quote.Id).Returns(quote);

            // 使用UpdateStatusAsync(1)表示审批
            await _quoteService.UpdateStatusAsync(quote.Id, 1);

            // 验证状态已更新
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 1));

            // ========== 步骤4: 基于报价单创建销售订单 ==========
            var orderRequest = new CreateSalesOrderRequest
            {
                OrderCode = "SO-2024-001",
                CustomerId = quote.CustomerId,
                SalesUserId = quote.SalesUserId,
                DeliveryDate = DateTime.UtcNow.AddDays(7),
                TotalAmount = quote.TotalAmount,
                GrandTotal = quote.TotalAmountWithTax ?? 0m,
                Currency = 1, // CNY
                PaymentTerms = "30天账期"
            };

            _salesOrderRepository.GetAllAsync().Returns(new List<SellOrder>());

            SellOrder? createdOrder = null;
            _salesOrderRepository.When(r => r.AddAsync(Arg.Any<SellOrder>()))
                .Do(call => createdOrder = call.Arg<SellOrder>());

            var order = await _salesOrderService.CreateAsync(orderRequest);

            Assert.NotNull(order);
            Assert.Equal("SO-2024-001", order.SellOrderCode);
            Assert.Equal(quote.TotalAmount, order.Total);
            Assert.Equal(quote.TotalAmountWithTax, order.ConvertTotal);
            Assert.Equal(0, order.Status); // 草稿

            // ========== 步骤5: 确认订单 ==========
            _salesOrderRepository.GetByIdAsync(order.Id).Returns(order);

            // 使用UpdateStatusAsync(1)表示确认订单
            await _salesOrderService.UpdateStatusAsync(order.Id, 1);

            // 验证状态已更新
            await _salesOrderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 1));
        }

        /// <summary>
        /// 测试报价单被拒绝的场景
        /// </summary>
        [Fact]
        public async Task Workflow_QuoteRejected_ShouldNotCreateOrder()
        {
            // 创建RFQ
            var rfq = new RFQ
            {
                Id = "RFQ-001",
                RFQCode = "RFQ-2024-002",
                CustomerId = "CUST-002",
                Status = 0
            };

            // 创建报价单
            var quote = new Quote
            {
                Id = "QT-001",
                QuoteCode = "QT-2024-002",
                CustomerId = rfq.CustomerId,
                TotalAmount = 5000m,
                TotalAmountWithTax = 5650m,
                Status = 0
            };

            _quoteRepository.GetByIdAsync(quote.Id).Returns(quote);

            // 拒绝报价单 - 使用状态码2表示已拒绝
            await _quoteService.UpdateStatusAsync(quote.Id, 2);

            // 验证状态为已拒绝
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 2));

            // 被拒绝的报价单不应该创建订单
            Assert.NotEqual(1, quote.Status); // 不是已审批状态
        }

        /// <summary>
        /// 测试查询客户相关的所有业务单据
        /// </summary>
        [Fact]
        public async Task GetCustomerDocuments_ShouldReturnAllRelatedDocuments()
        {
            // Arrange
            var customerId = "CUST-003";

            var rfqs = new List<RFQ>
            {
                new() { Id = "RFQ-1", RFQCode = "RFQ-001", CustomerId = customerId },
                new() { Id = "RFQ-2", RFQCode = "RFQ-002", CustomerId = customerId }
            };

            var quotes = new List<Quote>
            {
                new() { Id = "QT-1", QuoteCode = "QT-001", CustomerId = customerId },
                new() { Id = "QT-2", QuoteCode = "QT-002", CustomerId = customerId },
                new() { Id = "QT-3", QuoteCode = "QT-003", CustomerId = "OTHER" }
            };

            var orders = new List<SellOrder>
            {
                new() { Id = "SO-1", SellOrderCode = "SO-001", CustomerId = customerId }
            };

            _rfqRepository.GetAllAsync().Returns(rfqs);
            _quoteRepository.GetAllAsync().Returns(quotes);
            _salesOrderRepository.GetAllAsync().Returns(orders);

            // Act
            var customerRFQs = await _rfqService.GetByCustomerIdAsync(customerId);
            var allQuotes = await _quoteService.GetAllAsync();
            var customerQuotes = allQuotes.Where(q => q.CustomerId == customerId).ToList();
            var allOrders = await _salesOrderService.GetAllAsync();
            var customerOrders = allOrders.Where(o => o.CustomerId == customerId).ToList();

            // Assert
            Assert.Equal(2, customerRFQs.Count());
            Assert.Equal(2, customerQuotes.Count());
            Assert.Single(customerOrders);
        }

        /// <summary>
        /// 测试订单状态流转
        /// </summary>
        [Fact]
        public async Task OrderStatusFlow_DraftToCompleted_ShouldSucceed()
        {
            // Arrange - 创建草稿订单
            var orderId = "SO-001";
            var order = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-003",
                CustomerId = "CUST-001",
                Status = 0 // 草稿
            };

            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);

            // Act & Assert - 状态流转

            // 1. 草稿 -> 已确认 (状态码1)
            await _salesOrderService.UpdateStatusAsync(orderId, 1);
            Assert.Equal(1, order.Status);

            // 更新mock为确认状态
            order.Status = 1;
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);

            // 2. 已确认 -> 已发货 (状态码2)
            await _salesOrderService.UpdateStatusAsync(orderId, 2);
            Assert.Equal(2, order.Status);

            // 更新mock为发货状态
            order.Status = 2;
            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);

            // 3. 已发货 -> 已完成 (状态码3)
            await _salesOrderService.UpdateStatusAsync(orderId, 3);
            Assert.Equal(3, order.Status);

            // 验证UpdateAsync总共被调用了3次
            await _salesOrderRepository.Received(3).UpdateAsync(Arg.Any<SellOrder>());
        }

        /// <summary>
        /// 测试重复单据号校验
        /// </summary>
        [Fact]
        public async Task CreateDuplicateDocument_ShouldThrowException()
        {
            // Arrange
            var existingRFQs = new List<RFQ>
            {
                new() { Id = "1", RFQCode = "RFQ-DUP", CustomerId = "C1" }
            };

            _rfqRepository.GetAllAsync().Returns(existingRFQs);

            // Act & Assert
            var request = new CreateRFQRequest
            {
                RFQCode = "RFQ-DUP", // 重复的单据号
                CustomerId = "C2"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _rfqService.CreateAsync(request));

            Assert.Contains("RFQ-DUP", exception.Message);
        }

        /// <summary>
        /// 测试订单出库申请流程
        /// </summary>
        [Fact]
        public async Task RequestStockOut_ForConfirmedOrder_ShouldSucceed()
        {
            // Arrange
            var orderId = "SO-123";
            var order = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-004",
                CustomerId = "CUST-001",
                Status = 1, // 已确认
                StockOutStatus = 0 // 未出库
            };

            _salesOrderRepository.GetByIdAsync(orderId).Returns(order);

            // Act
            await _salesOrderService.RequestStockOutAsync(orderId, "USER-001");

            // Assert - StockOutStatus 设置为 1 表示待出库
            await _salesOrderRepository.Received(1).UpdateAsync(
                Arg.Is<SellOrder>(o => o.StockOutStatus == 1));
        }
    }
}
