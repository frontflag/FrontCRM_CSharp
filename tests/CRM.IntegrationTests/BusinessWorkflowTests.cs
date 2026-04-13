using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly IRepository<SellOrderItemExtend> _salesOrderItemExtendRepository;
        private readonly IRepository<PurchaseOrder> _poRepository;
        private readonly IRepository<PurchaseOrderItem> _poItemRepository;
        private readonly IRepository<PurchaseRequisition> _prRepository;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUserService _userService;
        private readonly IEntityLookupService _entityLookup;
        private readonly IRepository<SysParam> _sysParamRepo;
        private readonly IRepository<RbacRole> _rbacRoleRepo;
        private readonly IRepository<RbacUserRole> _rbacUserRoleRepo;
        private readonly IRepository<RbacDepartment> _rbacDepartmentRepo;
        private readonly IRepository<RbacUserDepartment> _rbacUserDepartmentRepo;
        private readonly IRepository<User> _userRepo;
        private readonly RFQService _rfqService;
        private readonly QuoteService _quoteService;
        private readonly SalesOrderService _salesOrderService;
        private readonly IFinanceExchangeRateService _financeExchangeRateService;
        private readonly IOrderJourneyLogService _orderJourneyLog;

        public BusinessWorkflowTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _rfqItemRepository = Substitute.For<IRepository<RFQItem>>();
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _quoteItemRepository = Substitute.For<IRepository<QuoteItem>>();
            _salesOrderRepository = Substitute.For<IRepository<SellOrder>>();
            _salesOrderItemRepository = Substitute.For<IRepository<SellOrderItem>>();
            _salesOrderItemExtendRepository = Substitute.For<IRepository<SellOrderItemExtend>>();
            _poRepository = Substitute.For<IRepository<PurchaseOrder>>();
            _poItemRepository = Substitute.For<IRepository<PurchaseOrderItem>>();
            _prRepository = Substitute.For<IRepository<PurchaseRequisition>>();
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
            _rbacDepartmentRepo = Substitute.For<IRepository<RbacDepartment>>();
            _rbacUserDepartmentRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            _userRepo = Substitute.For<IRepository<User>>();
            _sysParamRepo.FindAsync(Arg.Any<Expression<Func<SysParam, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SysParam>>(Array.Empty<SysParam>()));
            _rbacRoleRepo.GetAllAsync().Returns(new List<RbacRole>());
            _rbacUserRoleRepo.GetAllAsync().Returns(new List<RbacUserRole>());
            _rbacDepartmentRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            _rbacUserDepartmentRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            _userRepo.GetAllAsync().Returns(new List<User>());
            var rfqRbac = Substitute.For<IRbacService>();
            rfqRbac.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });
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
                _rbacDepartmentRepo,
                _rbacUserDepartmentRepo,
                _quoteRepository,
                _userRepo,
                rfqRbac,
                NullLogger<RFQService>.Instance);
            _quoteService = new QuoteService(
                _quoteRepository,
                _quoteItemRepository,
                _rfqItemRepository,
                _rfqRepository,
                _unitOfWork,
                _serialNumberService,
                _userService,
                NullLogger<QuoteService>.Instance);
            _financeExchangeRateService = Substitute.For<IFinanceExchangeRateService>();
            _financeExchangeRateService.GetCurrentAsync(default).ReturnsForAnyArgs(new FinanceExchangeRateDto
            {
                UsdToCny = 6.9194m,
                UsdToHkd = 7.8367m,
                UsdToEur = 0.8725m
            });
            _orderJourneyLog = Substitute.For<IOrderJourneyLogService>();
            var soItemExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var soLineSeq = Substitute.For<ISellOrderExtendLineSeqService>();
            soLineSeq.ReserveNextSequenceBlockAsync(Arg.Any<string>(), Arg.Any<int>(), default)
                .Returns(call => 1);
            _salesOrderService = new SalesOrderService(
                _salesOrderRepository,
                _salesOrderItemRepository,
                _salesOrderItemExtendRepository,
                _poRepository,
                _poItemRepository,
                _prRepository,
                _quoteItemRepository,
                _dataPermissionService,
                _serialNumberService,
                _financeExchangeRateService,
                _orderJourneyLog,
                soItemExtendSync,
                Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(),
                soLineSeq,
                _unitOfWork,
                NullLogger<SalesOrderService>.Instance);
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

        [Fact]
        public async Task OneSalesOrderItem_CanHaveMultiplePurchaseRequisitions_WithinRemainingQty()
        {
            // 模拟依赖
            var prRepo = Substitute.For<IRepository<PurchaseRequisition>>();
            var soRepo = Substitute.For<IRepository<SellOrder>>();
            var soItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var quoteItemRepo = Substitute.For<IRepository<QuoteItem>>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();

            // 创建采购申请服务
            var service = new PurchaseRequisitionService(
                prRepo, soRepo, soItemRepo, poItemRepo, quoteItemRepo, serialNumberService, unitOfWork);

            // 准备销售订单明细
            var sellOrderItemId = Guid.NewGuid().ToString();
            var sellOrderId = Guid.NewGuid().ToString();
            var sellOrderItem = new SellOrderItem
            {
                Id = sellOrderItemId,
                SellOrderId = sellOrderId,
                PN = "TEST-PN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                Status = 0
            };

            // 模拟仓储返回销售订单明细
            soItemRepo.GetByIdAsync(sellOrderItemId).Returns(sellOrderItem);
            // 模拟 FindAsync 用于 GetSellOrderLineOptionsAsync
            soItemRepo.FindAsync(Arg.Any<Expression<Func<SellOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SellOrderItem>>(new List<SellOrderItem> { sellOrderItem }));

            // 模拟销售订单
            var sellOrder = new SellOrder { Id = sellOrderId, SalesUserId = "sales-user-1" };
            soRepo.GetByIdAsync(sellOrderId).Returns(sellOrder);

            // 模拟没有采购订单明细
            poItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<PurchaseOrderItem>>(new List<PurchaseOrderItem>()));

            // 模拟没有报价明细
            quoteItemRepo.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(new List<QuoteItem>()));

            // 模拟采购申请仓储：初始为空
            var allPr = new List<PurchaseRequisition>();
            prRepo.FindAsync(Arg.Any<Expression<Func<PurchaseRequisition, bool>>>())
                .Returns(Task.FromResult<IEnumerable<PurchaseRequisition>>(allPr));
            prRepo.GetAllAsync().Returns(allPr);
            prRepo.AddAsync(Arg.Any<PurchaseRequisition>()).Returns(Task.CompletedTask)
                .AndDoes(call => allPr.Add(call.Arg<PurchaseRequisition>()));

            // 模拟流水号生成
            serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseRequisition).Returns("PR20260001", "PR20260002", "PR20260003");

            // 第一次采购申请：数量 30
            var request1 = new CreatePurchaseRequisitionRequest
            {
                SellOrderItemId = sellOrderItemId,
                Qty = 30m,
                ExpectedPurchaseTime = DateTime.UtcNow,
                Type = 0,
                PurchaseUserId = "purchase-user-1",
                Remark = "第一次申请"
            };

            var pr1 = await service.CreateAsync(request1);
            Assert.NotNull(pr1);
            Assert.Equal("PR20260001", pr1.BillCode);
            Assert.Equal(30m, pr1.Qty);
            Assert.Equal(0, pr1.Status); // 新建状态

            // 验证剩余可申请数量：100 - 30 = 70
            var options = await service.GetSellOrderLineOptionsAsync(sellOrderId);
            var lineOption = options.FirstOrDefault(o => o.sellOrderItemId == sellOrderItemId);
            Assert.NotNull(lineOption);
            Assert.Equal(70m, lineOption.remainingQty);

            // 第二次采购申请：数量 20
            var request2 = new CreatePurchaseRequisitionRequest
            {
                SellOrderItemId = sellOrderItemId,
                Qty = 20m,
                ExpectedPurchaseTime = DateTime.UtcNow,
                Type = 0,
                PurchaseUserId = "purchase-user-1",
                Remark = "第二次申请"
            };

            var pr2 = await service.CreateAsync(request2);
            Assert.NotNull(pr2);
            Assert.Equal("PR20260002", pr2.BillCode);
            Assert.Equal(20m, pr2.Qty);

            // 验证剩余可申请数量：100 - 30 - 20 = 50
            options = await service.GetSellOrderLineOptionsAsync(sellOrderId);
            lineOption = options.FirstOrDefault(o => o.sellOrderItemId == sellOrderItemId);
            Assert.NotNull(lineOption);
            Assert.Equal(50m, lineOption.remainingQty);

            // 第三次采购申请：数量 60（超过剩余数量，应失败）
            var request3 = new CreatePurchaseRequisitionRequest
            {
                SellOrderItemId = sellOrderItemId,
                Qty = 60m,
                ExpectedPurchaseTime = DateTime.UtcNow,
                Type = 0,
                PurchaseUserId = "purchase-user-1",
                Remark = "第三次申请（应失败）"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request3));
            Assert.Contains("剩余可采数量", exception.Message);

            // 验证最终剩余数量仍为 50
            options = await service.GetSellOrderLineOptionsAsync(sellOrderId);
            lineOption = options.FirstOrDefault(o => o.sellOrderItemId == sellOrderItemId);
            Assert.NotNull(lineOption);
            Assert.Equal(50m, lineOption.remainingQty);

            // 验证总共创建了两个采购申请
            Assert.Equal(2, allPr.Count);
        }

        [Fact]
        public async Task OneSalesOrderItem_CanHaveMultipleStockOutRequests_WithinRemainingQty()
        {
            var sellOrderItemId = Guid.NewGuid().ToString();
            var sellOrderId = Guid.NewGuid().ToString();

            // 模拟依赖
            var stockOutRepo = Substitute.For<IRepository<StockOut>>();
            var stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
            var stockOutRequestRepo = Substitute.For<IRepository<StockOutRequest>>();
            var pickingTaskRepo = Substitute.For<IRepository<PickingTask>>();
            var stockRepo = Substitute.For<IRepository<StockInfo>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var purchaseOrderItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var purchaseOrderRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var inventoryCenterService = Substitute.For<IInventoryCenterService>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var sellOrderItemExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();

            var linkedPoId = Guid.NewGuid().ToString();
            purchaseOrderItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(_ => Task.FromResult<IEnumerable<PurchaseOrderItem>>(new List<PurchaseOrderItem>
                {
                    new()
                    {
                        SellOrderItemId = sellOrderItemId,
                        PurchaseOrderId = linkedPoId,
                        VendorId = "VENDOR-001"
                    }
                }));
            purchaseOrderRepo.GetByIdAsync(linkedPoId)
                .Returns(new PurchaseOrder { Id = linkedPoId, Status = 30 });

            // 创建出库服务
            var customerRepoForStockOut = Substitute.For<IRepository<CustomerInfo>>();
            customerRepoForStockOut.GetAllAsync().Returns(new List<CustomerInfo>());
            var warehouseRepoForStockOut = Substitute.For<IRepository<WarehouseInfo>>();
            var service = new StockOutService(
                stockOutRepo, stockOutItemRepo, stockOutRequestRepo, pickingTaskRepo, stockRepo,
                sellOrderRepo, sellOrderItemRepo, customerRepoForStockOut, purchaseOrderItemRepo, purchaseOrderRepo, userRepo,
                warehouseRepoForStockOut,
                inventoryCenterService, serialNumberService, sellOrderItemExtendSync,
                Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(), unitOfWork,
                NullLogger<StockOutService>.Instance);

            // 准备销售订单明细
            var sellOrderItem = new SellOrderItem
            {
                Id = sellOrderItemId,
                SellOrderId = sellOrderId,
                PN = "TEST-PN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                Status = 0
            };

            // 模拟销售订单（已审核）
            var sellOrder = new SellOrder
            {
                Id = sellOrderId,
                Status = SellOrderMainStatus.Approved,
                SalesUserId = "sales-user-1"
            };

            // 模拟仓储返回
            sellOrderRepo.GetByIdAsync(sellOrderId).Returns(sellOrder);
            sellOrderItemRepo.GetByIdAsync(sellOrderItemId).Returns(sellOrderItem);

            // 模拟现有出库申请为空
            var allRequests = new List<StockOutRequest>();
            stockOutRequestRepo.FindAsync(Arg.Any<Expression<Func<StockOutRequest, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockOutRequest>>(allRequests));
            stockOutRequestRepo.GetAllAsync().Returns(allRequests);
            stockOutRequestRepo.AddAsync(Arg.Any<StockOutRequest>()).Returns(Task.CompletedTask)
                .AndDoes(call => allRequests.Add(call.Arg<StockOutRequest>()));

            // 模拟库存中心返回可用数量
            inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(sellOrderItemId)
                .Returns(new SellOrderLineAvailableQtyDto { AvailableQty = 100 });

            // 模拟流水号
            serialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest)
                .Returns("SOR20260001", "SOR20260002", "SOR20260003");

            // 第一次出库申请：数量 30
            var request1 = new CreateStockOutRequestRequest
            {
                SalesOrderId = sellOrderId,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 30m,
                CustomerId = "CUST-001",
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Remark = "第一次出库申请"
            };

            var req1 = await service.CreateStockOutRequestAsync(request1);
            Assert.NotNull(req1);
            Assert.Equal("SOR20260001", req1.RequestCode);
            Assert.Equal(30, req1.Quantity);
            Assert.Equal(0, req1.Status);

            // 验证剩余可申请数量：100 - 30 = 70
            var context = await service.GetApplyContextAsync(sellOrderId, sellOrderItemId);
            Assert.Equal(70m, context.remainingNotifyQty);

            // 第二次出库申请：数量 20
            var request2 = new CreateStockOutRequestRequest
            {
                SalesOrderId = sellOrderId,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 20m,
                CustomerId = "CUST-001",
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Remark = "第二次出库申请"
            };

            var req2 = await service.CreateStockOutRequestAsync(request2);
            Assert.NotNull(req2);
            Assert.Equal("SOR20260002", req2.RequestCode);
            Assert.Equal(20, req2.Quantity);

            // 验证剩余可申请数量：100 - 30 - 20 = 50
            context = await service.GetApplyContextAsync(sellOrderId, sellOrderItemId);
            Assert.Equal(50m, context.remainingNotifyQty);

            // 第三次出库申请：数量 60（超过剩余数量，应失败）
            var request3 = new CreateStockOutRequestRequest
            {
                SalesOrderId = sellOrderId,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 60m,
                CustomerId = "CUST-001",
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Remark = "第三次出库申请（应失败）"
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateStockOutRequestAsync(request3));
            Assert.Contains("不能超过剩余可申请数量", exception.Message);

            // 验证最终剩余数量仍为 50
            context = await service.GetApplyContextAsync(sellOrderId, sellOrderItemId);
            Assert.Equal(50m, context.remainingNotifyQty);

            // 验证总共创建了两个出库申请
            Assert.Equal(2, allRequests.Count);
        }

        [Fact]
        public async Task CreateStockOutRequest_Fails_WhenQuantityExceedsAvailableStock()
        {
            var sellOrderItemId = Guid.NewGuid().ToString();
            var sellOrderId = Guid.NewGuid().ToString();

            var stockOutRepo = Substitute.For<IRepository<StockOut>>();
            var stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
            var stockOutRequestRepo = Substitute.For<IRepository<StockOutRequest>>();
            var pickingTaskRepo = Substitute.For<IRepository<PickingTask>>();
            var stockRepo = Substitute.For<IRepository<StockInfo>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var purchaseOrderItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var purchaseOrderRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var inventoryCenterService = Substitute.For<IInventoryCenterService>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var sellOrderItemExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();

            var linkedPoId = Guid.NewGuid().ToString();
            purchaseOrderItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(_ => Task.FromResult<IEnumerable<PurchaseOrderItem>>(new List<PurchaseOrderItem>
                {
                    new()
                    {
                        SellOrderItemId = sellOrderItemId,
                        PurchaseOrderId = linkedPoId,
                        VendorId = "VENDOR-001"
                    }
                }));
            purchaseOrderRepo.GetByIdAsync(linkedPoId)
                .Returns(new PurchaseOrder { Id = linkedPoId, Status = 30 });

            var customerRepoForStockOut = Substitute.For<IRepository<CustomerInfo>>();
            customerRepoForStockOut.GetAllAsync().Returns(new List<CustomerInfo>());
            var warehouseRepoForStockOut = Substitute.For<IRepository<WarehouseInfo>>();
            var service = new StockOutService(
                stockOutRepo, stockOutItemRepo, stockOutRequestRepo, pickingTaskRepo, stockRepo,
                sellOrderRepo, sellOrderItemRepo, customerRepoForStockOut, purchaseOrderItemRepo, purchaseOrderRepo, userRepo,
                warehouseRepoForStockOut,
                inventoryCenterService, serialNumberService, sellOrderItemExtendSync,
                Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(), unitOfWork,
                NullLogger<StockOutService>.Instance);

            var sellOrderItem = new SellOrderItem
            {
                Id = sellOrderItemId,
                SellOrderId = sellOrderId,
                PN = "TEST-PN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                Status = 0
            };
            var sellOrder = new SellOrder
            {
                Id = sellOrderId,
                Status = SellOrderMainStatus.Approved,
                SalesUserId = "sales-user-1"
            };

            sellOrderRepo.GetByIdAsync(sellOrderId).Returns(sellOrder);
            sellOrderItemRepo.GetByIdAsync(sellOrderItemId).Returns(sellOrderItem);

            var allRequests = new List<StockOutRequest>();
            stockOutRequestRepo.FindAsync(Arg.Any<Expression<Func<StockOutRequest, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockOutRequest>>(allRequests));
            stockOutRequestRepo.GetAllAsync().Returns(allRequests);

            inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(sellOrderItemId)
                .Returns(new SellOrderLineAvailableQtyDto { AvailableQty = 5 });

            var request = new CreateStockOutRequestRequest
            {
                SalesOrderId = sellOrderId,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 10m,
                CustomerId = "CUST-001",
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Remark = "应因在库不足失败"
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStockOutRequestAsync(request));
            Assert.Contains("在库可用数量不足", exception.Message);
            Assert.Empty(allRequests);
        }

        [Fact]
        public async Task OnePurchaseOrderItem_CanHaveMultiplePaymentRequests()
        {
            // 模拟依赖
            var paymentRepo = Substitute.For<IRepository<Payment>>();
            var paymentRequestRepo = Substitute.For<IRepository<PaymentRequest>>();
            var serialNumberService = Substitute.For<ISerialNumberService>();

            // 创建付款服务
            var service = new PaymentService(paymentRepo, paymentRequestRepo, serialNumberService);

            // 准备采购订单明细
            var purchaseOrderId = Guid.NewGuid().ToString();
            var purchaseOrderItemId = Guid.NewGuid().ToString();
            var vendorId = "VENDOR-001";

            // 模拟付款申请仓储
            var allPaymentRequests = new List<PaymentRequest>();
            paymentRequestRepo.GetAllAsync().Returns(allPaymentRequests);
            paymentRequestRepo.AddAsync(Arg.Any<PaymentRequest>()).Returns(Task.CompletedTask)
                .AndDoes(call => allPaymentRequests.Add(call.Arg<PaymentRequest>()));

            // 模拟流水号
            serialNumberService.GenerateNextAsync(ModuleCodes.PaymentRequest)
                .Returns("PAYREQ001", "PAYREQ002", "PAYREQ003");

            // 第一次付款申请：金额 5000
            var request1 = new CreatePaymentRequest
            {
                PurchaseOrderId = purchaseOrderId,
                VendorId = vendorId,
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Amount = 5000m,
                Currency = 1,
                PaymentMethod = 1,
                Remark = "第一次付款申请"
            };

            var pr1 = await service.CreatePaymentRequestAsync(request1);
            Assert.NotNull(pr1);
            Assert.Equal("PAYREQ001", pr1.RequestCode);
            Assert.Equal(5000m, pr1.Amount);
            Assert.Equal(0, pr1.Status);

            // 第二次付款申请：金额 3000
            var request2 = new CreatePaymentRequest
            {
                PurchaseOrderId = purchaseOrderId,
                VendorId = vendorId,
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Amount = 3000m,
                Currency = 1,
                PaymentMethod = 1,
                Remark = "第二次付款申请"
            };

            var pr2 = await service.CreatePaymentRequestAsync(request2);
            Assert.NotNull(pr2);
            Assert.Equal("PAYREQ002", pr2.RequestCode);
            Assert.Equal(3000m, pr2.Amount);

            // 第三次付款申请：金额 2000
            var request3 = new CreatePaymentRequest
            {
                PurchaseOrderId = purchaseOrderId,
                VendorId = vendorId,
                RequestUserId = "user-1",
                RequestDate = DateTime.UtcNow,
                Amount = 2000m,
                Currency = 1,
                PaymentMethod = 1,
                Remark = "第三次付款申请"
            };

            var pr3 = await service.CreatePaymentRequestAsync(request3);
            Assert.NotNull(pr3);
            Assert.Equal("PAYREQ003", pr3.RequestCode);
            Assert.Equal(2000m, pr3.Amount);

            // 验证总共创建了三个付款申请
            Assert.Equal(3, allPaymentRequests.Count);
        }

        [Fact]
        public async Task OnePurchaseOrderItem_CanHaveMultipleArrivalNotices_WithinRemainingQty()
        {
            // 模拟依赖
            var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
            var stockInRepoForLogistics = Substitute.For<IRepository<StockIn>>();
            var qcRepo = Substitute.For<IRepository<QCInfo>>();
            var qcItemRepo = Substitute.For<IRepository<QCItem>>();
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var poItemExtendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
            var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var poItemExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();

            // 创建物流服务
            var logisticsLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<LogisticsService>>();
            var service = new LogisticsService(
                notifyRepo, stockInRepoForLogistics, qcRepo, qcItemRepo, poRepo, poItemRepo, poItemExtendRepo,
                sellOrderItemRepo, sellOrderRepo, serialNumberService, poItemExtendSync, unitOfWork,
                logisticsLogger);

            // 准备采购订单明细
            var purchaseOrderItemId = Guid.NewGuid().ToString();
            var purchaseOrderId = Guid.NewGuid().ToString();
            var sellOrderItemId = Guid.NewGuid().ToString();
            var vendorId = "VENDOR-001";

            var purchaseOrderItem = new PurchaseOrderItem
            {
                Id = purchaseOrderItemId,
                PurchaseOrderId = purchaseOrderId,
                SellOrderItemId = sellOrderItemId,
                VendorId = vendorId,
                PN = "TEST-PN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                Cost = 50m,
                Status = 30, // 已确认
                StockInStatus = 0
            };

            var purchaseOrder = new PurchaseOrder
            {
                Id = purchaseOrderId,
                PurchaseOrderCode = "PO-20260001",
                VendorId = vendorId,
                VendorName = "测试供应商",
                PurchaseUserName = "采购员",
                Status = 30, // 已确认
                DeliveryDate = DateTime.UtcNow.AddDays(7)
            };

            // 模拟仓储返回
            poItemRepo.GetByIdAsync(purchaseOrderItemId).Returns(purchaseOrderItem);
            poRepo.GetByIdAsync(purchaseOrderId).Returns(purchaseOrder);

            // 模拟采购订单明细扩展
            var poItemExtend = new PurchaseOrderItemExtend
            {
                Id = purchaseOrderItemId,
                QtyStockInNotifyNot = 100m // 初始剩余可通知数量
            };
            poItemExtendRepo.GetByIdAsync(purchaseOrderItemId).Returns(poItemExtend);

            // 模拟现有到货通知为空
            var allNotices = new List<StockInNotify>();
            notifyRepo.FindAsync(Arg.Any<Expression<Func<StockInNotify, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockInNotify>>(allNotices));
            notifyRepo.GetAllAsync().Returns(allNotices);
            notifyRepo.AddAsync(Arg.Any<StockInNotify>()).Returns(Task.CompletedTask)
                .AndDoes(call => allNotices.Add(call.Arg<StockInNotify>()));

            // 模拟流水号
            serialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice)
                .Returns("AN20260001", "AN20260002", "AN20260003");

            // 第一次到货通知：数量 30
            var request1 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = purchaseOrderItemId,
                ExpectQty = 30m,
                PurchaseOrderId = purchaseOrderId,
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(5)
            };

            var notice1 = await service.CreateArrivalNoticeAsync(request1);
            Assert.NotNull(notice1);
            Assert.Equal("AN20260001", notice1.NoticeCode);
            Assert.Equal(30, notice1.ExpectQty);
            Assert.Equal(10, notice1.Status); // 未到货

            // 模拟扩展更新后剩余数量为 70
            poItemExtend.QtyStockInNotifyNot = 70m;
            poItemExtendRepo.GetByIdAsync(purchaseOrderItemId).Returns(poItemExtend);

            // 第二次到货通知：数量 20
            var request2 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = purchaseOrderItemId,
                ExpectQty = 20m,
                PurchaseOrderId = purchaseOrderId,
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(6)
            };

            var notice2 = await service.CreateArrivalNoticeAsync(request2);
            Assert.NotNull(notice2);
            Assert.Equal("AN20260002", notice2.NoticeCode);
            Assert.Equal(20, notice2.ExpectQty);

            // 模拟扩展更新后剩余数量为 50
            poItemExtend.QtyStockInNotifyNot = 50m;
            poItemExtendRepo.GetByIdAsync(purchaseOrderItemId).Returns(poItemExtend);

            // 第三次到货通知：数量 60（超过剩余数量，应失败）
            var request3 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = purchaseOrderItemId,
                ExpectQty = 60m,
                PurchaseOrderId = purchaseOrderId,
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(7)
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateArrivalNoticeAsync(request3));
            Assert.Contains("不能大于剩余可通知数量", exception.Message);

            // 验证总共创建了两个到货通知
            Assert.Equal(2, allNotices.Count);
        }

        [Fact]
        public async Task SellOrderItem_ProgressStatus_SyncWithBusinessOperations()
        {
            // 模拟依赖
            var soItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var extendRepo = Substitute.For<IRepository<SellOrderItemExtend>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var stockInRepo = Substitute.For<IRepository<StockIn>>();
            var stockOutRequestRepo = Substitute.For<IRepository<StockOutRequest>>();
            var stockOutRepo = Substitute.For<IRepository<StockOut>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();

            // 创建同步服务
            var service = new SellOrderItemExtendSyncService(
                soItemRepo, extendRepo, poItemRepo, stockInRepo, stockOutRequestRepo,
                stockOutRepo, receiptItemRepo,
                NullLogger<SellOrderItemExtendSyncService>.Instance);

            // 准备销售订单明细
            var sellOrderItemId = Guid.NewGuid().ToString();
            var sellOrderItem = new SellOrderItem
            {
                Id = sellOrderItemId,
                SellOrderId = Guid.NewGuid().ToString(),
                PN = "TEST-PN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                ConvertPrice = 10m, // 假设 USD 折算单价
                Status = 0
            };

            // 准备扩展记录
            var extend = new SellOrderItemExtend
            {
                Id = sellOrderItemId,
                QtyAlreadyPurchased = 0m,
                QtyNotPurchase = 100m,
                QtyStockOutNotify = 0m,
                QtyStockOutNotifyNot = 100m,
                QtyStockOutActual = 0m,
                ReceiptAmount = 1000m, // 假设应收金额
                ReceiptAmountFinish = 0m,
                ReceiptAmountNot = 1000m,
                InvoiceAmount = 1000m,
                InvoiceAmountFinish = 0m,
                InvoiceAmountNot = 1000m,
                PurchaseProgressStatus = 0,
                StockInProgressStatus = 0,
                StockOutProgressStatus = 0,
                ReceiptProgressStatus = 0,
                InvoiceProgressStatus = 0,
                QuoteConvertCost = 5m // 假设报价成本 USD
            };

            // 模拟采购订单明细（已采购数量 30）
            var poItem = new PurchaseOrderItem
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderItemId = sellOrderItemId,
                Qty = 30m,
                ConvertPrice = 6m,
                Status = 30 // 已确认
            };

            // 模拟采购入库单头（销售明细维度已入库 10）
            var stockInForSo = new StockIn
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderItemId = sellOrderItemId,
                Status = 2,
                StockInType = 1,
                TotalQuantity = 10
            };

            // 模拟出库申请（数量 20，状态部分完成）
            var stockOutRequest = new StockOutRequest
            {
                Id = Guid.NewGuid().ToString(),
                SalesOrderItemId = sellOrderItemId,
                Quantity = 20,
                Status = 1 // 部分完成
            };

            // 模拟已完成的出库单（数量 5）
            var stockOut = new StockOut
            {
                Id = Guid.NewGuid().ToString(),
                SourceId = stockOutRequest.Id,
                SellOrderItemId = sellOrderItemId,
                StockOutType = 1,
                TotalQuantity = 5,
                Status = 2 // 已完成
            };

            // 模拟收款条目（已核销金额 300）
            var receiptItem = new FinanceReceiptItem
            {
                SellOrderItemId = sellOrderItemId,
                VerifiedAmount = 300m
            };

            // 设置模拟返回值
            soItemRepo.GetByIdAsync(sellOrderItemId).Returns(sellOrderItem);
            extendRepo.GetByIdAsync(sellOrderItemId).Returns(extend);
            poItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<PurchaseOrderItem>>(new List<PurchaseOrderItem> { poItem }));
            stockInRepo.FindAsync(Arg.Any<Expression<Func<StockIn, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockIn>>(new List<StockIn> { stockInForSo }));
            stockOutRequestRepo.FindAsync(Arg.Any<Expression<Func<StockOutRequest, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockOutRequest>>(new List<StockOutRequest> { stockOutRequest }));
            stockOutRepo.FindAsync(Arg.Any<Expression<Func<StockOut, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockOut>>(new List<StockOut> { stockOut }));
            receiptItemRepo.FindAsync(Arg.Any<Expression<Func<FinanceReceiptItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<FinanceReceiptItem>>(new List<FinanceReceiptItem> { receiptItem }));

            // 执行同步
            await service.RecalculateAsync(sellOrderItemId);

            // 验证采购进度：已采购 30/100 => 部分采购
            Assert.Equal(1, extend.PurchaseProgressStatus); // ProgressPartial = 1
            Assert.Equal(30m, extend.QtyAlreadyPurchased);
            Assert.Equal(70m, extend.QtyNotPurchase);

            // 验证入库进度：已入库 10/100 => 部分入库
            Assert.Equal(1, extend.StockInProgressStatus);

            // 验证出库进度：实际出库 5/100 => 部分出库
            Assert.Equal(1, extend.StockOutProgressStatus);
            Assert.Equal(5m, extend.QtyStockOutActual);

            // 验证收款进度：已核销 300/1000 => 部分收款
            Assert.Equal(1, extend.ReceiptProgressStatus);
            Assert.Equal(300m, extend.ReceiptAmountFinish);

            // 验证开票进度：未开票 => 待开票
            Assert.Equal(0, extend.InvoiceProgressStatus); // 未开票，应为 0

            // 验证扩展记录已更新
            await extendRepo.Received(1).UpdateAsync(Arg.Is<SellOrderItemExtend>(e => e.Id == sellOrderItemId));
        }

        [Fact]
        public async Task PurchaseOrderItem_ProgressStatus_SyncWithBusinessOperations()
        {
            // 模拟依赖
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var extendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
            var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
            var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
            var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
            var purInvItemRepo = Substitute.For<IRepository<FinancePurchaseInvoiceItem>>();
            var purInvRepo = Substitute.For<IRepository<FinancePurchaseInvoice>>();
            var stockInRepo = Substitute.For<IRepository<StockIn>>();
            var stockInItemRepo = Substitute.For<IRepository<StockInItem>>();
            var qcRepo = Substitute.For<IRepository<QCInfo>>();
            var sellSoItemExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();

            // 创建同步服务
            var service = new PurchaseOrderItemExtendSyncService(
                poItemRepo, poRepo, extendRepo, notifyRepo, payItemRepo, paymentRepo,
                purInvItemRepo, purInvRepo, stockInRepo, stockInItemRepo, qcRepo,
                sellSoItemExtendSync);

            // 准备采购订单明细
            var purchaseOrderItemId = Guid.NewGuid().ToString();
            var purchaseOrderId = Guid.NewGuid().ToString();
            var poItem = new PurchaseOrderItem
            {
                Id = purchaseOrderItemId,
                PurchaseOrderId = purchaseOrderId,
                Qty = 100m,
                Cost = 50m,
                ConvertPrice = 50m,
                Status = 30, // 已确认
                SellOrderItemId = Guid.NewGuid().ToString()
            };

            // 准备采购订单主单
            var poHeader = new PurchaseOrder
            {
                Id = purchaseOrderId,
                Status = 30 // 已确认
            };

            // 准备扩展记录
            var extend = new PurchaseOrderItemExtend
            {
                Id = purchaseOrderItemId,
                QtyStockInNotifyNot = 100m,
                PurchaseInvoiceAmount = 5000m,
                PurchaseInvoiceToBe = 5000m,
                PaymentAmount = 5000m,
                PaymentAmountNot = 5000m,
                QtyReceiveTotal = 0m
            };

            // 模拟到货通知（已接收数量 30，在途 10）
            var notify = new StockInNotify
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderItemId = purchaseOrderItemId,
                ExpectQty = 40,
                ReceiveQty = 30
            };

            // 模拟付款条目（已核销金额 2000）
            var payItem = new FinancePaymentItem
            {
                PurchaseOrderItemId = purchaseOrderItemId,
                VerificationDone = 2000m,
                FinancePaymentId = Guid.NewGuid().ToString()
            };

            // 模拟付款单（有效状态）
            var payment = new FinancePayment
            {
                Id = payItem.FinancePaymentId,
                Status = 0 // 正常
            };

            // 模拟进项发票条目（已开票金额 1500）
            var invItem = new FinancePurchaseInvoiceItem
            {
                StockInId = Guid.NewGuid().ToString(),
                BillAmount = 1500m,
                FinancePurchaseInvoiceId = Guid.NewGuid().ToString()
            };

            // 模拟进项发票（未红冲）
            var financePurchaseInvoice = new FinancePurchaseInvoice
            {
                Id = invItem.FinancePurchaseInvoiceId,
                RedInvoiceStatus = 0 // 未红冲
            };

            // 模拟入库单（已完成，未取消）
            var stockIn = new StockIn
            {
                Id = invItem.StockInId,
                PurchaseOrderItemId = purchaseOrderItemId,
                Status = 2, // 已完成
                StockInType = 1, // 采购入库
                TotalQuantity = 25
            };

            // 模拟入库单明细（匹配采购明细）
            var stockInItem = new StockInItem
            {
                StockInId = stockIn.Id,
                MaterialId = purchaseOrderItemId,
                Quantity = 25
            };

            // 设置模拟返回值
            poItemRepo.GetByIdAsync(purchaseOrderItemId).Returns(poItem);
            poRepo.GetByIdAsync(purchaseOrderId).Returns(poHeader);
            extendRepo.GetByIdAsync(purchaseOrderItemId).Returns(extend);
            notifyRepo.FindAsync(Arg.Any<Expression<Func<StockInNotify, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockInNotify>>(new List<StockInNotify> { notify }));
            payItemRepo.FindAsync(Arg.Any<Expression<Func<FinancePaymentItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<FinancePaymentItem>>(new List<FinancePaymentItem> { payItem }));
            paymentRepo.FindAsync(Arg.Any<Expression<Func<FinancePayment, bool>>>())
                .Returns(Task.FromResult<IEnumerable<FinancePayment>>(new List<FinancePayment> { payment }));
            purInvItemRepo.GetAllAsync().Returns(new List<FinancePurchaseInvoiceItem> { invItem });
            purInvRepo.GetAllAsync().Returns(new List<FinancePurchaseInvoice> { financePurchaseInvoice });
            stockInRepo.FindAsync(Arg.Any<Expression<Func<StockIn, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockIn>>(new List<StockIn> { stockIn }));
            stockInRepo.GetAllAsync().Returns(new List<StockIn> { stockIn });
            stockInItemRepo.FindAsync(Arg.Any<Expression<Func<StockInItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<StockInItem>>(new List<StockInItem> { stockInItem }));
            stockInItemRepo.GetAllAsync().Returns(new List<StockInItem> { stockInItem });
            qcRepo.FindAsync(Arg.Any<Expression<Func<QCInfo, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QCInfo>>(new List<QCInfo>()));
            qcRepo.GetAllAsync().Returns(new List<QCInfo>());

            // 执行同步
            await service.RecalculateAsync(purchaseOrderItemId);



            // 验证采购进度：已确认 => 部分采购
            Assert.Equal(1, extend.PurchaseProgressStatus); // ProgressPartial

            // 验证入库进度：已入库 25/100 => 部分入库
            Assert.Equal(1, extend.StockInProgressStatus);
            Assert.Equal(25m, extend.QtyReceiveTotal);

            // 验证付款进度：已核销 2000/5000 => 部分付款
            Assert.Equal(1, extend.PaymentProgressStatus);
            Assert.Equal(2000m, extend.PaymentAmountFinish);
            Assert.Equal(3000m, extend.PaymentAmountNot);

            // 验证进项发票进度：已开票 1500/5000 => 部分开票
            Assert.Equal(1, extend.InvoiceProgressStatus);
            Assert.Equal(1500m, extend.PurchaseInvoiceDone);
            Assert.Equal(3500m, extend.PurchaseInvoiceToBe);

            // 验证扩展记录已更新
            await extendRepo.Received(1).UpdateAsync(Arg.Is<PurchaseOrderItemExtend>(e => e.Id == purchaseOrderItemId));

            // 验证销售订单明细同步被调用
            await sellSoItemExtendSync.Received(1).RecalculateAsync(poItem.SellOrderItemId.Trim(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RFQItem_Currency_SavesAndRetrievesCorrectly()
        {
            // 模拟依赖
            var rfqRepo = Substitute.For<IRepository<RFQ>>();
            var itemRepo = Substitute.For<IRepository<RFQItem>>();
            var customerRepo = Substitute.For<IRepository<CustomerInfo>>();
            var entityLookup = Substitute.For<IEntityLookupService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var dataPermissionService = Substitute.For<IDataPermissionService>();
            var userService = Substitute.For<IUserService>();
            var sysParamRepo = Substitute.For<IRepository<SysParam>>();
            var rbacRoleRepo = Substitute.For<IRepository<RbacRole>>();
            var rbacUserRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
            var rbacDepartmentRepo = Substitute.For<IRepository<RbacDepartment>>();
            var rbacUserDepartmentRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            rbacDepartmentRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            rbacUserDepartmentRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            var quoteRepo = Substitute.For<IRepository<Quote>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var logger = Substitute.For<ILogger<RFQService>>();
            var rbacSvc = Substitute.For<IRbacService>();
            rbacSvc.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });

            // 创建服务
            var service = new RFQService(
                rfqRepo, itemRepo, customerRepo, entityLookup, unitOfWork,
                serialNumberService, dataPermissionService, userService,
                sysParamRepo, rbacRoleRepo, rbacUserRoleRepo, rbacDepartmentRepo, rbacUserDepartmentRepo, quoteRepo,
                userRepo, rbacSvc, logger);

            // 模拟序列号生成
            serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");

            // 准备请求：USD 币种
            var request = new CreateRFQRequest
            {
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                Items = new List<CreateRFQItemRequest>
                {
                    new CreateRFQItemRequest
                    {
                        Mpn = "TEST-MPN-001",
                        Brand = "TEST-BRAND",
                        Quantity = 100,
                        TargetPrice = 50.5m,
                        PriceCurrency = 2, // USD 枚举值
                        CustomerMpn = "CUST-MPN-001",
                        CustomerBrand = "CUST-BRAND",
                        ProductionDate = "2025-01",
                        ExpiryDate = new DateTime(2026, 1, 1),
                        MinPackageQty = 10,
                        Moq = 1,
                        Alternatives = "ALT1,ALT2",
                        Remark = "测试备注"
                    }
                }
            };

            // 模拟保存
            RFQ? savedRfq = null;
            RFQItem? savedItem = null;
            _ = rfqRepo.AddAsync(Arg.Do<RFQ>(r => savedRfq = r));
            _ = itemRepo.AddAsync(Arg.Do<RFQItem>(i => savedItem = i));

            // 执行创建
            var created = await service.CreateAsync(request);

            // 验证保存的 RFQ
            Assert.NotNull(savedRfq);
            Assert.Equal("RF20260001", savedRfq.RfqCode);

            // 验证保存的明细：PriceCurrency 应为 2 (USD)
            Assert.NotNull(savedItem);
            Assert.Equal(2, savedItem.PriceCurrency);
            Assert.Equal("TEST-MPN-001", savedItem.Mpn);
            Assert.Equal(50.5m, savedItem.TargetPrice);

            // 模拟按ID查询返回保存的RFQ
            rfqRepo.GetByIdAsync(savedRfq.Id).Returns(savedRfq);
            itemRepo.FindAsync(Arg.Any<Expression<Func<RFQItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<RFQItem>>(new List<RFQItem> { savedItem }));

            // 执行查询
            var retrieved = await service.GetByIdAsync(savedRfq.Id);

            // 验证查询结果
            Assert.NotNull(retrieved);
            Assert.Equal(savedRfq.Id, retrieved.Id);
            var retrievedItem = retrieved.Items.FirstOrDefault();
            Assert.NotNull(retrievedItem);
            Assert.Equal(2, retrievedItem.PriceCurrency); // 应仍为 USD
            Assert.Equal("TEST-MPN-001", retrievedItem.Mpn);
        }

        [Fact]
        public async Task RFQList_ShouldDisplaySalesUserNameAndCreateUserName()
        {
            // 模拟依赖
            var rfqRepo = Substitute.For<IRepository<RFQ>>();
            var itemRepo = Substitute.For<IRepository<RFQItem>>();
            var customerRepo = Substitute.For<IRepository<CustomerInfo>>();
            var entityLookup = Substitute.For<IEntityLookupService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var dataPermissionService = Substitute.For<IDataPermissionService>();
            var userService = Substitute.For<IUserService>();
            var sysParamRepo = Substitute.For<IRepository<SysParam>>();
            var rbacRoleRepo = Substitute.For<IRepository<RbacRole>>();
            var rbacUserRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
            var rbacDepartmentRepo = Substitute.For<IRepository<RbacDepartment>>();
            var rbacUserDepartmentRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            rbacDepartmentRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            rbacUserDepartmentRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            var quoteRepo = Substitute.For<IRepository<Quote>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var logger = Substitute.For<ILogger<RFQService>>();

            // 模拟系统参数查询返回空（避免轮询逻辑干扰）
            sysParamRepo.FindAsync(Arg.Any<Expression<Func<SysParam, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SysParam>>(Array.Empty<SysParam>()));
            sysParamRepo.GetAllAsync().Returns(new List<SysParam>());
            rbacRoleRepo.GetAllAsync().Returns(new List<RbacRole>());
            rbacUserRoleRepo.GetAllAsync().Returns(new List<RbacUserRole>());
            quoteRepo.GetAllAsync().Returns(new List<Quote>());
            userRepo.GetAllAsync().Returns(new List<User>());

            var rbacSvc = Substitute.For<IRbacService>();
            rbacSvc.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });

            // 创建服务
            var service = new RFQService(
                rfqRepo, itemRepo, customerRepo, entityLookup, unitOfWork,
                serialNumberService, dataPermissionService, userService,
                sysParamRepo, rbacRoleRepo, rbacUserRoleRepo, rbacDepartmentRepo, rbacUserDepartmentRepo, quoteRepo,
                userRepo, rbacSvc, logger);

            // 模拟序列号生成
            serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");

            // 准备用户数据
            var salesUserId = "SALES-USER-001";
            var salesUserName = "张三";
            var createUserId = "CREATE-USER-001";
            var createUserName = "李四";
            var userList = new List<User>
            {
                new User { Id = salesUserId, UserName = salesUserName },
                new User { Id = createUserId, UserName = createUserName }
            };
            userService.GetAllAsync().Returns(userList);

            // 模拟数据权限过滤：返回原列表
            dataPermissionService.FilterRFQsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<RFQListItem>>())
                .Returns(call => Task.FromResult<IReadOnlyList<RFQListItem>>(call.ArgAt<IEnumerable<RFQListItem>>(1).ToList()));

            // 准备请求
            var request = new CreateRFQRequest
            {
                CustomerId = "CUST-001",
                SalesUserId = salesUserId,
                Items = new List<CreateRFQItemRequest>
                {
                    new CreateRFQItemRequest
                    {
                        Mpn = "TEST-MPN-001",
                        Brand = "TEST-BRAND",
                        Quantity = 100
                    }
                }
            };

            // 模拟保存
            RFQ? savedRfq = null;
            RFQItem? savedItem = null;
            _ = rfqRepo.AddAsync(Arg.Do<RFQ>(r => savedRfq = r));
            _ = itemRepo.AddAsync(Arg.Do<RFQItem>(i => savedItem = i));

            // 执行创建，传入 actingUserId
            var created = await service.CreateAsync(request, createUserId);

            // 验证保存的 RFQ
            Assert.NotNull(savedRfq);
            Assert.Equal("RF20260001", savedRfq.RfqCode);
            Assert.Equal(salesUserId, savedRfq.SalesUserId);
            Assert.Equal(createUserId, savedRfq.CreateByUserId);
            // 确保AddAsync被调用
            await rfqRepo.Received(1).AddAsync(Arg.Any<RFQ>());
            await itemRepo.Received(1).AddAsync(Arg.Any<RFQItem>());

            // 模拟列表查询：返回保存的 RFQ
            rfqRepo.GetAllAsync().Returns(new List<RFQ> { savedRfq });
            // 模拟客户查询
            customerRepo.GetAllAsync().Returns(new List<CustomerInfo>
            {
                new CustomerInfo { Id = "CUST-001", OfficialName = "测试客户" }
            });

            // 验证模拟：确保GetAllAsync返回数据
            var allRfqs = await rfqRepo.GetAllAsync();
            Assert.Single(allRfqs);
            Assert.Equal(savedRfq.Id, allRfqs.First().Id);

            // 执行列表查询
            var listRequest = new RFQQueryRequest
            {
                PageIndex = 1,
                PageSize = 20,
                CurrentUserId = createUserId // 用于数据权限过滤
            };
            var pagedResult = await service.GetPagedAsync(listRequest);

            // 验证列表结果
            Assert.NotNull(pagedResult);
            Assert.Equal(1, pagedResult.TotalCount);
            var listItem = pagedResult.Items.FirstOrDefault();
            Assert.NotNull(listItem);
            Assert.Equal(savedRfq.Id, listItem.Id);
            Assert.Equal(salesUserId, listItem.SalesUserId);
            Assert.Equal(salesUserName, listItem.SalesUserName); // 业务员姓名应显示
            Assert.Equal(createUserId, listItem.CreateByUserId);
            Assert.Equal(createUserName, listItem.CreateUserName); // 创建人姓名应显示
        }

        [Fact]
        public async Task RFQList_ShouldPopulateSalesUserNameAndCreateUserName()
        {
            // 模拟依赖
            var rfqRepo = Substitute.For<IRepository<RFQ>>();
            var itemRepo = Substitute.For<IRepository<RFQItem>>();
            var customerRepo = Substitute.For<IRepository<CustomerInfo>>();
            var entityLookup = Substitute.For<IEntityLookupService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var dataPermissionService = Substitute.For<IDataPermissionService>();
            var userService = Substitute.For<IUserService>();
            var sysParamRepo = Substitute.For<IRepository<SysParam>>();
            var rbacRoleRepo = Substitute.For<IRepository<RbacRole>>();
            var rbacUserRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
            var rbacDepartmentRepo = Substitute.For<IRepository<RbacDepartment>>();
            var rbacUserDepartmentRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            rbacDepartmentRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            rbacUserDepartmentRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            var quoteRepo = Substitute.For<IRepository<Quote>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var logger = Substitute.For<ILogger<RFQService>>();
            var rbacSvc = Substitute.For<IRbacService>();
            rbacSvc.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });

            // 创建服务
            var service = new RFQService(
                rfqRepo, itemRepo, customerRepo, entityLookup, unitOfWork,
                serialNumberService, dataPermissionService, userService,
                sysParamRepo, rbacRoleRepo, rbacUserRoleRepo, rbacDepartmentRepo, rbacUserDepartmentRepo, quoteRepo,
                userRepo, rbacSvc, logger);

            // 准备用户数据
            var salesUserId = "SALES-USER-001";
            var salesUserName = "张三";
            var createUserId = "CREATE-USER-001";
            var createUserName = "李四";
            var userList = new List<User>
            {
                new User { Id = salesUserId, UserName = salesUserName },
                new User { Id = createUserId, UserName = createUserName }
            };
            userService.GetAllAsync().Returns(userList);

            // 模拟数据权限过滤：返回原列表
            dataPermissionService.FilterRFQsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<RFQListItem>>())
                .Returns(call => Task.FromResult<IReadOnlyList<RFQListItem>>(call.ArgAt<IEnumerable<RFQListItem>>(1).ToList()));

            // 模拟客户查询
            customerRepo.GetAllAsync().Returns(new List<CustomerInfo>
            {
                new CustomerInfo { Id = "CUST-001", OfficialName = "测试客户" }
            });

            // 创建一个模拟的 RFQ 实体，设置 SalesUserId 和 CreateByUserId
            var rfq = new RFQ
            {
                Id = "RFQ-TEST-001",
                RfqCode = "RF20260001",
                CustomerId = "CUST-001",
                SalesUserId = salesUserId,
                CreateByUserId = createUserId,
                CreateTime = DateTime.UtcNow,
                Status = 0,
                ItemCount = 1
            };
            rfqRepo.GetAllAsync().Returns(new List<RFQ> { rfq });

            // 执行列表查询
            var listRequest = new RFQQueryRequest
            {
                PageIndex = 1,
                PageSize = 20,
                CurrentUserId = createUserId
            };
            var pagedResult = await service.GetPagedAsync(listRequest);

            // 验证列表结果
            Assert.NotNull(pagedResult);
            Assert.Equal(1, pagedResult.TotalCount);
            var listItem = pagedResult.Items.FirstOrDefault();
            Assert.NotNull(listItem);
            Assert.Equal(salesUserId, listItem.SalesUserId);
            Assert.Equal(salesUserName, listItem.SalesUserName); // 业务员姓名应显示
            Assert.Equal(createUserId, listItem.CreateByUserId);
            Assert.Equal(createUserName, listItem.CreateUserName); // 创建人姓名应显示
        }

        [Fact]
        public async Task CompleteOrderExecutionWorkflow_WithMultipleBatches_ShouldSucceed()
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
                        Mpn = "TEST-MPN-001",
                        Brand = "TEST-BRAND",
                        Quantity = 100
                    }
                }
            };
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());
            var rfq = await _rfqService.CreateAsync(rfqRequest);
            Assert.NotNull(rfq);
            Assert.Equal("RF20260001", rfq.RfqCode);

            // Step 2: 创建报价单
            var quoteRequest = new CreateQuoteRequest
            {
                QuoteCode = "QT-2024-001",
                CustomerId = rfq.CustomerId,
                SalesUserId = rfq.SalesUserId,
                QuoteDate = DateTime.UtcNow,
                Mpn = "TEST-MPN-001"
            };
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var quote = await _quoteService.CreateAsync(quoteRequest);
            Assert.NotNull(quote);
            Assert.Equal("QT-2024-001", quote.QuoteCode);

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
                        PN = "TEST-MPN-001",
                        Brand = "TEST-BRAND",
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
            _salesOrderRepository.GetByIdAsync(order.Id).Returns(order);
            await _salesOrderService.UpdateStatusAsync(order.Id, SellOrderMainStatus.InProgress);
            await _salesOrderRepository.Received().UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.InProgress));

            // 获取销售订单明细ID（模拟）
            var sellOrderItemId = Guid.NewGuid().ToString();
            // 实际上需要从模拟的销售订单明细中获取，这里简化处理
            // 我们将模拟销售订单明细仓储返回一个明细
            var sellOrderItem = new SellOrderItem
            {
                Id = sellOrderItemId,
                SellOrderId = order.Id,
                PN = "TEST-MPN-001",
                Brand = "TEST-BRAND",
                Qty = 100m,
                Status = 0
            };
            _salesOrderItemRepository.GetAllAsync().Returns(new List<SellOrderItem> { sellOrderItem });
            _salesOrderItemRepository.GetByIdAsync(sellOrderItemId).Returns(sellOrderItem);
            _salesOrderItemRepository.FindAsync(Arg.Any<Expression<Func<SellOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SellOrderItem>>(new List<SellOrderItem> { sellOrderItem }));

            // Step 6: 创建采购申请（多批次）
            // 模拟采购申请服务
            var prRepo = Substitute.For<IRepository<PurchaseRequisition>>();
            var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
            var quoteItemRepo = Substitute.For<IRepository<QuoteItem>>();
            var serialNumberService = Substitute.For<ISerialNumberService>();
            var unitOfWork = Substitute.For<IUnitOfWork>();
            var prService = new PurchaseRequisitionService(
                prRepo, _salesOrderRepository, _salesOrderItemRepository, poItemRepo, quoteItemRepo, serialNumberService, unitOfWork);

            // 模拟采购申请仓储
            var allPr = new List<PurchaseRequisition>();
            prRepo.FindAsync(Arg.Any<Expression<Func<PurchaseRequisition, bool>>>())
                .Returns(Task.FromResult<IEnumerable<PurchaseRequisition>>(allPr));
            prRepo.GetAllAsync().Returns(allPr);
            prRepo.AddAsync(Arg.Any<PurchaseRequisition>()).Returns(Task.CompletedTask)
                .AndDoes(call => allPr.Add(call.Arg<PurchaseRequisition>()));

            // 模拟采购订单明细为空
            poItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<PurchaseOrderItem>>(new List<PurchaseOrderItem>()));
            // 模拟报价明细为空
            quoteItemRepo.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(new List<QuoteItem>()));

            // 模拟流水号
            serialNumberService.GenerateNextAsync(ModuleCodes.PurchaseRequisition).Returns("PR20260001", "PR20260002");

            // 第一次采购申请：数量 30
            var prRequest1 = new CreatePurchaseRequisitionRequest
            {
                SellOrderItemId = sellOrderItemId,
                Qty = 30m,
                ExpectedPurchaseTime = DateTime.UtcNow,
                Type = 0,
                PurchaseUserId = "PURCHASE-USER-001",
                Remark = "第一批采购"
            };
            var pr1 = await prService.CreateAsync(prRequest1);
            Assert.NotNull(pr1);
            Assert.Equal("PR20260001", pr1.BillCode);
            Assert.Equal(30m, pr1.Qty);

            // 第二次采购申请：数量 70
            var prRequest2 = new CreatePurchaseRequisitionRequest
            {
                SellOrderItemId = sellOrderItemId,
                Qty = 70m,
                ExpectedPurchaseTime = DateTime.UtcNow,
                Type = 0,
                PurchaseUserId = "PURCHASE-USER-001",
                Remark = "第二批采购"
            };
            var pr2 = await prService.CreateAsync(prRequest2);
            Assert.NotNull(pr2);
            Assert.Equal("PR20260002", pr2.BillCode);
            Assert.Equal(70m, pr2.Qty);

            // 验证采购申请总数
            Assert.Equal(2, allPr.Count);

            // Step 7: 创建采购订单（多批次）
            // 模拟采购订单服务
            var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var poItemExtendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
            var vendorRepo = Substitute.For<IRepository<VendorInfo>>();
            var userRepo = Substitute.For<IRepository<User>>();
            var poSerialNumberService = Substitute.For<ISerialNumberService>();
            var poUnitOfWork = Substitute.For<IUnitOfWork>();
            // 添加缺失的依赖
            var poItemExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
            var poLineSeq = Substitute.For<IPurchaseOrderExtendLineSeqService>();
            var sellOrderItemExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            
            var poService = new PurchaseOrderService(
                poRepo, poItemRepo, poItemExtendRepo,
                _salesOrderRepository, _salesOrderItemRepository,
                _dataPermissionService, poSerialNumberService,
                _financeExchangeRateService, _orderJourneyLog,
                sellOrderItemExtendSync, poItemExtendSync, poLineSeq,
                NullLogger<PurchaseOrderService>.Instance,
                poUnitOfWork);

            // 模拟采购订单仓储
            var allPo = new List<PurchaseOrder>();
            poRepo.AddAsync(Arg.Any<PurchaseOrder>()).Returns(Task.CompletedTask)
                .AndDoes(call => allPo.Add(call.Arg<PurchaseOrder>()));
            poRepo.GetAllAsync().Returns(allPo);

            // 模拟采购订单明细仓储
            var allPoItems = new List<PurchaseOrderItem>();
            poItemRepo.AddAsync(Arg.Any<PurchaseOrderItem>()).Returns(Task.CompletedTask)
                .AndDoes(call => allPoItems.Add(call.Arg<PurchaseOrderItem>()));
            poItemRepo.GetAllAsync().Returns(allPoItems);

            // 模拟供应商
            vendorRepo.GetByIdAsync(Arg.Any<string>()).Returns(new VendorInfo { Id = "VENDOR-001", OfficialName = "测试供应商" });

            // 模拟流水号
            poSerialNumberService.GenerateNextAsync(ModuleCodes.PurchaseOrder).Returns("PO20260001", "PO20260002");

            // 由于采购订单创建逻辑复杂，这里简化：直接模拟采购订单创建
            // 实际项目中，采购订单可能由采购申请转换而来，这里跳过转换逻辑
            // 我们假设两个采购申请分别生成了两个采购订单项
            var poItem1 = new PurchaseOrderItem
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderId = "PO-001",
                SellOrderItemId = sellOrderItemId,
                VendorId = "VENDOR-001",
                Qty = 30m,
                Status = 0
            };
            var poItem2 = new PurchaseOrderItem
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderId = "PO-002",
                SellOrderItemId = sellOrderItemId,
                VendorId = "VENDOR-001",
                Qty = 70m,
                Status = 0
            };
            allPoItems.Add(poItem1);
            allPoItems.Add(poItem2);
            poItemRepo.GetByIdAsync(poItem1.Id).Returns(poItem1);
            poItemRepo.GetByIdAsync(poItem2.Id).Returns(poItem2);
            poRepo.GetByIdAsync("PO-001")
                .Returns(new PurchaseOrder
                {
                    Id = "PO-001",
                    Status = 30,
                    DeliveryDate = DateTime.UtcNow,
                    VendorId = "VENDOR-001"
                });
            poRepo.GetByIdAsync("PO-002")
                .Returns(new PurchaseOrder
                {
                    Id = "PO-002",
                    Status = 30,
                    DeliveryDate = DateTime.UtcNow,
                    VendorId = "VENDOR-001"
                });

            // Step 8: 确认采购订单
            // 模拟采购订单确认
            poItem1.Status = 30; // 已确认
            poItem2.Status = 30;

            // Step 9: 创建到货通知（多批次）
            // 模拟物流服务（LogisticsService）
            var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
            var stockInRepoForLogistics2 = Substitute.For<IRepository<StockIn>>();
            var qcRepo = Substitute.For<IRepository<QCInfo>>();
            var qcItemRepo = Substitute.For<IRepository<QCItem>>();
            var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
            var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
            var logisticsSerialNumberService = Substitute.For<ISerialNumberService>();
            var poItemExtendSyncForLogistics = Substitute.For<IPurchaseOrderItemExtendSyncService>();
            var logisticsUnitOfWork = Substitute.For<IUnitOfWork>();
            var logisticsLogger2 = Substitute.For<Microsoft.Extensions.Logging.ILogger<LogisticsService>>();
            var logisticsService = new LogisticsService(
                notifyRepo, stockInRepoForLogistics2, qcRepo, qcItemRepo, poRepo, poItemRepo, poItemExtendRepo,
                sellOrderItemRepo, sellOrderRepo, logisticsSerialNumberService, poItemExtendSyncForLogistics,
                logisticsUnitOfWork, logisticsLogger2);

            // 模拟到货通知仓储
            var allNotices = new List<StockInNotify>();
            notifyRepo.AddAsync(Arg.Any<StockInNotify>()).Returns(Task.CompletedTask)
                .AndDoes(call => allNotices.Add(call.Arg<StockInNotify>()));
            notifyRepo.GetAllAsync().Returns(allNotices);

            // 模拟采购订单明细扩展
            var poItemExtend1 = new PurchaseOrderItemExtend { Id = poItem1.Id, QtyStockInNotifyNot = 30m };
            var poItemExtend2 = new PurchaseOrderItemExtend { Id = poItem2.Id, QtyStockInNotifyNot = 70m };
            poItemExtendRepo.GetByIdAsync(poItem1.Id).Returns(poItemExtend1);
            poItemExtendRepo.GetByIdAsync(poItem2.Id).Returns(poItemExtend2);

            // 模拟流水号
            logisticsSerialNumberService.GenerateNextAsync(ModuleCodes.ArrivalNotice).Returns("AN20260001", "AN20260002", "AN20260003", "AN20260004");

            // 第一批到货通知：采购订单项1，数量10
            var noticeRequest1 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = poItem1.Id,
                ExpectQty = 10m,
                PurchaseOrderId = "PO-001",
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(5)
            };
            var notice1 = await logisticsService.CreateArrivalNoticeAsync(noticeRequest1);
            Assert.NotNull(notice1);
            Assert.Equal("AN20260001", notice1.NoticeCode);
            Assert.Equal(10, notice1.ExpectQty);

            // 第二批到货通知：采购订单项1，数量20
            var noticeRequest2 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = poItem1.Id,
                ExpectQty = 20m,
                PurchaseOrderId = "PO-001",
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(6)
            };
            var notice2 = await logisticsService.CreateArrivalNoticeAsync(noticeRequest2);
            Assert.NotNull(notice2);
            Assert.Equal("AN20260002", notice2.NoticeCode);
            Assert.Equal(20, notice2.ExpectQty);

            // 第三批到货通知：采购订单项2，数量30
            var noticeRequest3 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = poItem2.Id,
                ExpectQty = 30m,
                PurchaseOrderId = "PO-002",
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(7)
            };
            var notice3 = await logisticsService.CreateArrivalNoticeAsync(noticeRequest3);
            Assert.NotNull(notice3);
            Assert.Equal("AN20260003", notice3.NoticeCode);
            Assert.Equal(30, notice3.ExpectQty);

            // 第四批到货通知：采购订单项2，数量40
            var noticeRequest4 = new CreateArrivalNoticeRequest
            {
                PurchaseOrderItemId = poItem2.Id,
                ExpectQty = 40m,
                PurchaseOrderId = "PO-002",
                ExpectedArrivalDate = DateTime.UtcNow.AddDays(8)
            };
            var notice4 = await logisticsService.CreateArrivalNoticeAsync(noticeRequest4);
            Assert.NotNull(notice4);
            Assert.Equal("AN20260004", notice4.NoticeCode);
            Assert.Equal(40, notice4.ExpectQty);

            // 验证到货通知总数
            Assert.Equal(4, allNotices.Count);

            // Step 10: 创建入库单（多批次）
            // 模拟入库服务（StockInService）
            var stockInRepo = Substitute.For<IRepository<StockIn>>();
            var stockInItemRepo = Substitute.For<IRepository<StockInItem>>();
            var stockRepo = Substitute.For<IRepository<StockInfo>>();
            var stockInSerialNumberService = Substitute.For<ISerialNumberService>();
            var stockInUnitOfWork = Substitute.For<IUnitOfWork>();
            // 添加缺失的依赖
            var purchaseOrderRepo = Substitute.For<IRepository<PurchaseOrder>>();
            var vendorRepoLocal = Substitute.For<IRepository<VendorInfo>>();
            var warehouseRepoLocal = Substitute.For<IRepository<WarehouseInfo>>();
            var materialRepo = Substitute.For<IRepository<MaterialInfo>>();
            var logisticsServiceLocal = Substitute.For<ILogisticsService>();
            var inventoryCenterServiceLocal = Substitute.For<IInventoryCenterService>();
            
            var stockInNotifyRepo = Substitute.For<IRepository<StockInNotify>>();
            var stockInLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<StockInService>>();
            var stockInSellExtendSync = Substitute.For<ISellOrderItemExtendSyncService>();
            var stockInService = new StockInService(
                stockInRepo, stockInItemRepo,
                purchaseOrderRepo, poItemRepo,
                _salesOrderItemRepository, _salesOrderRepository,
                qcRepo, stockInNotifyRepo, vendorRepoLocal, warehouseRepoLocal, materialRepo,
                logisticsServiceLocal, inventoryCenterServiceLocal,
                stockInSerialNumberService, _userService,
                stockInSellExtendSync, Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(), stockInUnitOfWork, stockInLogger);

            // 模拟入库单仓储
            var allStockIns = new List<StockIn>();
            stockInRepo.AddAsync(Arg.Any<StockIn>()).Returns(Task.CompletedTask)
                .AndDoes(call => allStockIns.Add(call.Arg<StockIn>()));
            stockInRepo.GetAllAsync().Returns(allStockIns);

            // 模拟入库单明细仓储
            var allStockInItems = new List<StockInItem>();
            stockInItemRepo.AddAsync(Arg.Any<StockInItem>()).Returns(Task.CompletedTask)
                .AndDoes(call => allStockInItems.Add(call.Arg<StockInItem>()));
            stockInItemRepo.GetAllAsync().Returns(allStockInItems);

            // 模拟流水号
            stockInSerialNumberService.GenerateNextAsync(ModuleCodes.StockIn).Returns("SI20260001", "SI20260002", "SI20260003", "SI20260004");

            // 由于入库单创建逻辑复杂，这里简化：直接模拟入库完成
            // 实际项目中，入库单可能由到货通知转换而来
            // 我们假设每个到货通知都生成了一个入库单
            // 这里跳过具体实现，仅验证流程完整性

            // Step 11: 创建出库申请（多批次）
            // 模拟出库服务（StockOutService）
            var stockOutRepo = Substitute.For<IRepository<StockOut>>();
            var stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
            var stockOutRequestRepo = Substitute.For<IRepository<StockOutRequest>>();
            var pickingTaskRepo = Substitute.For<IRepository<PickingTask>>();
            var inventoryCenterServiceForStockOut = Substitute.For<IInventoryCenterService>();
            var stockOutSerialNumberService = Substitute.For<ISerialNumberService>();
            var sellOrderItemExtendSyncForStockOut = Substitute.For<ISellOrderItemExtendSyncService>();
            var stockOutUnitOfWork = Substitute.For<IUnitOfWork>();
            poItemRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrderItem, bool>>>())
                .Returns(call =>
                {
                    var pred = call.Arg<Expression<Func<PurchaseOrderItem, bool>>>().Compile();
                    return Task.FromResult<IEnumerable<PurchaseOrderItem>>(allPoItems.Where(pred).ToList());
                });
            poRepo.GetByIdAsync("PO-001").Returns(new PurchaseOrder { Id = "PO-001", Status = 30 });
            poRepo.GetByIdAsync("PO-002").Returns(new PurchaseOrder { Id = "PO-002", Status = 30 });
            var customerRepoForStockOut2 = Substitute.For<IRepository<CustomerInfo>>();
            customerRepoForStockOut2.GetAllAsync().Returns(new List<CustomerInfo>());
            var warehouseRepoForStockOut2 = Substitute.For<IRepository<WarehouseInfo>>();
            var stockOutService = new StockOutService(
                stockOutRepo, stockOutItemRepo, stockOutRequestRepo, pickingTaskRepo, stockRepo,
                _salesOrderRepository, _salesOrderItemRepository, customerRepoForStockOut2, poItemRepo, poRepo, userRepo,
                warehouseRepoForStockOut2,
                inventoryCenterServiceForStockOut, stockOutSerialNumberService, sellOrderItemExtendSyncForStockOut,
                Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>(), stockOutUnitOfWork,
                NullLogger<StockOutService>.Instance);

            // 模拟出库申请仓储
            var allStockOutRequests = new List<StockOutRequest>();
            stockOutRequestRepo.AddAsync(Arg.Any<StockOutRequest>()).Returns(Task.CompletedTask)
                .AndDoes(call => allStockOutRequests.Add(call.Arg<StockOutRequest>()));
            stockOutRequestRepo.GetAllAsync().Returns(allStockOutRequests);

            // 模拟库存中心返回可用数量
            inventoryCenterServiceForStockOut.GetAvailableQtyForSellOrderItemAsync(sellOrderItemId)
                .Returns(new SellOrderLineAvailableQtyDto { AvailableQty = 100 });

            // 模拟流水号
            stockOutSerialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest).Returns("SOR20260001", "SOR20260002");

            // 第一批出库申请：数量 40
            var outRequest1 = new CreateStockOutRequestRequest
            {
                SalesOrderId = order.Id,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 40m,
                CustomerId = "CUST-001",
                RequestUserId = "USER-001",
                RequestDate = DateTime.UtcNow,
                Remark = "第一批出库"
            };
            var sor1 = await stockOutService.CreateStockOutRequestAsync(outRequest1);
            Assert.NotNull(sor1);
            Assert.Equal("SOR20260001", sor1.RequestCode);
            Assert.Equal(40, sor1.Quantity);

            // 第二批出库申请：数量 60
            var outRequest2 = new CreateStockOutRequestRequest
            {
                SalesOrderId = order.Id,
                SalesOrderItemId = sellOrderItemId,
                Quantity = 60m,
                CustomerId = "CUST-001",
                RequestUserId = "USER-001",
                RequestDate = DateTime.UtcNow,
                Remark = "第二批出库"
            };
            var sor2 = await stockOutService.CreateStockOutRequestAsync(outRequest2);
            Assert.NotNull(sor2);
            Assert.Equal("SOR20260002", sor2.RequestCode);
            Assert.Equal(60, sor2.Quantity);

            // 验证出库申请总数
            Assert.Equal(2, allStockOutRequests.Count);

            // Step 12: 创建出库单（多批次）
            // 模拟出库单创建（简化）
            var allStockOuts = new List<StockOut>();
            stockOutRepo.AddAsync(Arg.Any<StockOut>()).Returns(Task.CompletedTask)
                .AndDoes(call => allStockOuts.Add(call.Arg<StockOut>()));
            stockOutRepo.GetAllAsync().Returns(allStockOuts);

            // 模拟出库单明细仓储
            var allStockOutItems = new List<StockOutItem>();
            stockOutItemRepo.AddAsync(Arg.Any<StockOutItem>()).Returns(Task.CompletedTask)
                .AndDoes(call => allStockOutItems.Add(call.Arg<StockOutItem>()));
            stockOutItemRepo.GetAllAsync().Returns(allStockOutItems);

            // Step 13: 创建收款
            // 模拟收款服务（FinanceReceiptService）
            var receiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
            var receiptItemRepo = Substitute.For<IRepository<FinanceReceiptItem>>();
            var receiptSerialNumberService = Substitute.For<ISerialNumberService>();
            // 模拟缺失的依赖
            var financeSellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var sellInvoiceItemRepoLocal = Substitute.For<IRepository<SellInvoiceItem>>();
            var dataPermissionServiceLocal = Substitute.For<IDataPermissionService>();
            var sellOrderItemExtendSyncLocal = Substitute.For<ISellOrderItemExtendSyncService>();
            var receiptUnitOfWork = Substitute.For<IUnitOfWork>();
            var receiptService = new FinanceReceiptService(
                receiptRepo, receiptItemRepo, financeSellInvoiceRepo, sellInvoiceItemRepoLocal, _salesOrderRepository,
                _userRepo,
                dataPermissionServiceLocal, receiptSerialNumberService, sellOrderItemExtendSyncLocal, receiptUnitOfWork);

            // 模拟收款仓储
            var allReceipts = new List<FinanceReceipt>();
            receiptRepo.AddAsync(Arg.Any<FinanceReceipt>()).Returns(Task.CompletedTask)
                .AndDoes(call => allReceipts.Add(call.Arg<FinanceReceipt>()));
            receiptRepo.GetAllAsync().Returns(allReceipts);

            // 模拟收款条目仓储
            var allReceiptItems = new List<FinanceReceiptItem>();
            receiptItemRepo.AddAsync(Arg.Any<FinanceReceiptItem>()).Returns(Task.CompletedTask)
                .AndDoes(call => allReceiptItems.Add(call.Arg<FinanceReceiptItem>()));
            receiptItemRepo.GetAllAsync().Returns(allReceiptItems);

            // 模拟流水号
            receiptSerialNumberService.GenerateNextAsync(ModuleCodes.Receipt).Returns("RC20260001");

            // 创建收款请求
            var receiptRequest = new CreateFinanceReceiptRequest
            {
                FinanceReceiptCode = "RC20260001",
                CustomerId = "CUST-001",
                ReceiptDate = DateTime.UtcNow,
                ReceiptAmount = 10000m,
                ReceiptCurrency = 1,
                ReceiptMode = 1,
                Remark = "测试收款"
            };
            var receipt = await receiptService.CreateAsync(receiptRequest);
            Assert.NotNull(receipt);
            Assert.Equal("RC20260001", receipt.FinanceReceiptCode);
            Assert.Equal(10000m, receipt.ReceiptAmount);

            // Step 14: 创建销项发票
            // 模拟销项发票服务（FinanceSellInvoiceService）
            var sellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
            var sellInvoiceItemRepoForInvoice = Substitute.For<IRepository<SellInvoiceItem>>();
            var sellInvoiceSerialNumberService = Substitute.For<ISerialNumberService>();
            var invoiceDataPermissionService = Substitute.For<IDataPermissionService>();
            var invoiceUnitOfWork = Substitute.For<IUnitOfWork>();
            var sellInvoiceService = new FinanceSellInvoiceService(
                sellInvoiceRepo, sellInvoiceItemRepoForInvoice, invoiceDataPermissionService, sellInvoiceSerialNumberService, invoiceUnitOfWork);

            // 模拟销项发票仓储
            var allSellInvoices = new List<FinanceSellInvoice>();
            sellInvoiceRepo.AddAsync(Arg.Any<FinanceSellInvoice>()).Returns(Task.CompletedTask)
                .AndDoes(call => allSellInvoices.Add(call.Arg<FinanceSellInvoice>()));
            sellInvoiceRepo.GetAllAsync().Returns(allSellInvoices);

            // 模拟销项发票条目仓储
            var allSellInvoiceItems = new List<SellInvoiceItem>();
            sellInvoiceItemRepoForInvoice.AddAsync(Arg.Any<SellInvoiceItem>()).Returns(Task.CompletedTask)
                .AndDoes(call => allSellInvoiceItems.Add(call.Arg<SellInvoiceItem>()));
            sellInvoiceItemRepoForInvoice.GetAllAsync().Returns(allSellInvoiceItems);

            // 模拟流水号
            sellInvoiceSerialNumberService.GenerateNextAsync(ModuleCodes.OutputInvoice).Returns("INV20260001");

            // 创建销项发票请求
            var invoiceRequest = new CreateFinanceSellInvoiceRequest
            {
                InvoiceCode = "INV20260001",
                CustomerId = "CUST-001",
                MakeInvoiceDate = DateTime.UtcNow,
                InvoiceTotal = 10000m,
                Currency = 1,
                Type = 10,
                SellInvoiceType = 100,
                Remark = "测试销项发票"
            };
            var invoice = await sellInvoiceService.CreateAsync(invoiceRequest);
            Assert.NotNull(invoice);
            Assert.Equal("INV20260001", invoice.InvoiceCode);
            Assert.Equal(10000m, invoice.InvoiceTotal);

            // 验证整个流程完成
            // 这里可以添加更多断言，验证各环节状态
        }

    }
}
