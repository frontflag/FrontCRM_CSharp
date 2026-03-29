using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
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
        private readonly IRepository<SellOrderItem> _salesOrderItemRepository;
        private readonly IRepository<PurchaseOrder> _poRepository;
        private readonly IRepository<PurchaseOrderItem> _poItemRepository;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUserService _userService;
        private readonly IEntityLookupService _entityLookup;
        private readonly IRepository<SysParam> _sysParamRepo;
        private readonly IRepository<RbacRole> _rbacRoleRepo;
        private readonly IRepository<RbacUserRole> _rbacUserRoleRepo;
        private readonly IRepository<User> _userRepo;
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
            _salesOrderItemRepository = Substitute.For<IRepository<SellOrderItem>>();
            _poRepository = Substitute.For<IRepository<PurchaseOrder>>();
            _poItemRepository = Substitute.For<IRepository<PurchaseOrderItem>>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _dataPermissionService = Substitute.For<IDataPermissionService>();
            _userService = Substitute.For<IUserService>();
            _userService.GetAllAsync().Returns(new List<User>());
            _serialNumberService.GenerateNextAsync(ModuleCodes.RFQ).Returns("RF20260001");
            _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation).Returns("QT-2024-001");
            _serialNumberService.GenerateNextAsync(ModuleCodes.SalesOrder).Returns("SO-2024-001");
            _entityLookup = Substitute.For<IEntityLookupService>();
            _sysParamRepo = Substitute.For<IRepository<SysParam>>();
            _rbacRoleRepo = Substitute.For<IRepository<RbacRole>>();
            _rbacUserRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
            _userRepo = Substitute.For<IRepository<User>>();
            _sysParamRepo.FindAsync(Arg.Any<Expression<Func<SysParam, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SysParam>>(Array.Empty<SysParam>()));
            _rbacRoleRepo.GetAllAsync().Returns(new List<RbacRole>());
            _rbacUserRoleRepo.GetAllAsync().Returns(new List<RbacUserRole>());
            _userRepo.GetAllAsync().Returns(new List<User>());
            _rfqService = new RFQService(
                _rfqRepository,
                _rfqItemRepository,
                null!,
                _entityLookup,
                _unitOfWork,
                _serialNumberService,
                _dataPermissionService,
                _userService,
                _sysParamRepo,
                _rbacRoleRepo,
                _rbacUserRoleRepo,
                _quoteRepository,
                _userRepo);
            _quoteService = new QuoteService(_quoteRepository, _quoteItemRepository, _unitOfWork, _serialNumberService);
            _salesOrderService = new SalesOrderService(
                _salesOrderRepository,
                _salesOrderItemRepository,
                _poRepository,
                _poItemRepository,
                _dataPermissionService,
                _serialNumberService);
        }

        [Fact]
        public async Task CompleteWorkflow_FromRFQToSalesOrder_ShouldSucceed()
        {
            // Step 1: 创建需求单(RFQ)
            var rfqRequest = new CreateRFQRequest
            {
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                Items =
                {
                    new CreateRFQItemRequest
                    {
                        Mpn = "REF3430QDBVRQ1",
                        Brand = "TI",
                        CustomerBrand = "TI",
                        Quantity = 1
                    }
                }
            };
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());
            var rfq = await _rfqService.CreateAsync(rfqRequest);
            Assert.NotNull(rfq);
            Assert.Equal("RF20260001", rfq.RfqCode);
            Assert.Equal(0, rfq.Status);

            // Step 2: 创建报价单
            var quoteRequest = new CreateQuoteRequest
            {
                QuoteCode = "QT-2024-001",
                CustomerId = rfq.CustomerId,
                SalesUserId = rfq.SalesUserId,
                QuoteDate = DateTime.UtcNow,
                Mpn = "REF3430QDBVRQ1"
            };
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var quote = await _quoteService.CreateAsync(quoteRequest);
            Assert.NotNull(quote);
            Assert.Equal("QT-2024-001", quote.QuoteCode);
            Assert.Equal(0, quote.Status);

            // Step 3: 审批报价单
            _quoteRepository.GetByIdAsync(quote.Id).Returns(quote);
            await _quoteService.UpdateStatusAsync(quote.Id, 2);
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 2));

            // Step 4: 创建销售订单
            var orderRequest = new CreateSalesOrderRequest
            {
                SellOrderCode = "SO-2024-001",
                CustomerId = quote.CustomerId ?? string.Empty,
                CustomerName = "测试客户",
                SalesUserId = quote.SalesUserId,
                Currency = 1,
                DeliveryDate = DateTime.UtcNow.AddDays(7),
                Comment = "30天账期",
                Items = new List<CreateSalesOrderItemRequest>
                {
                    new()
                    {
                        QuoteId = quote.Id,
                        PN = "REF3430QDBVRQ1",
                        Brand = "TI",
                        Qty = 100,
                        Price = 100m,
                        Currency = 1
                    }
                }
            };
            _salesOrderRepository.GetAllAsync().Returns(new List<SellOrder>());
            _salesOrderItemRepository.GetAllAsync().Returns(new List<SellOrderItem>());
            _salesOrderRepository.AddAsync(Arg.Any<SellOrder>()).Returns(Task.CompletedTask);
            _salesOrderItemRepository.AddAsync(Arg.Any<SellOrderItem>()).Returns(Task.CompletedTask);
            _salesOrderRepository.UpdateAsync(Arg.Any<SellOrder>()).Returns(Task.CompletedTask);

            var order = await _salesOrderService.CreateAsync(orderRequest);
            Assert.NotNull(order);
            Assert.Equal("SO-2024-001", order.SellOrderCode);
            Assert.Equal(10000m, order.Total);
            Assert.Equal(SellOrderMainStatus.New, order.Status);

            // Step 5: 确认销售订单
            // Note: CreateAsync already called UpdateAsync once (to persist Total),
            // UpdateStatusAsync calls it again with Status==3, so we use AtLeastOnce.
            _salesOrderRepository.GetByIdAsync(order.Id).Returns(order);
            await _salesOrderService.UpdateStatusAsync(order.Id, SellOrderMainStatus.InProgress);
            await _salesOrderRepository.Received().UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.InProgress));
        }
    }
}
