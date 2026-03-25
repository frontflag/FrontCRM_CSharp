using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services
{
    public class SalesOrderServiceTests
    {
        private readonly IRepository<SellOrder> _orderRepository;
        private readonly IRepository<SellOrderItem> _orderItemRepository;
        private readonly IRepository<PurchaseOrder> _poRepository;
        private readonly IRepository<PurchaseOrderItem> _poItemRepository;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ISerialNumberService _serialNumberService;
        private readonly SalesOrderService _orderService;

        public SalesOrderServiceTests()
        {
            _orderRepository = Substitute.For<IRepository<SellOrder>>();
            _orderItemRepository = Substitute.For<IRepository<SellOrderItem>>();
            _poRepository = Substitute.For<IRepository<PurchaseOrder>>();
            _poItemRepository = Substitute.For<IRepository<PurchaseOrderItem>>();
            _dataPermissionService = Substitute.For<IDataPermissionService>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _serialNumberService.GenerateNextAsync(ModuleCodes.SalesOrder).Returns("SO2603240001");
            _orderService = new SalesOrderService(
                _orderRepository,
                _orderItemRepository,
                _poRepository,
                _poItemRepository,
                _dataPermissionService,
                _serialNumberService);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateSalesOrder()
        {
            // Arrange
            var request = new CreateSalesOrderRequest
            {
                SellOrderCode = "SO-2024-001",
                CustomerId = "CUST-001",
                CustomerName = "测试客户",
                SalesUserId = "USER-001",
                Type = 1,
                Currency = 1,
                Items = new List<CreateSalesOrderItemRequest>
                {
                    new() { PN = "STM32F103", Brand = "ST", Qty = 100, Price = 5.5m }
                }
            };
            _orderRepository.GetAllAsync().Returns(new List<SellOrder>());
            _orderRepository.AddAsync(Arg.Any<SellOrder>()).Returns(Task.CompletedTask);
            _orderItemRepository.AddAsync(Arg.Any<SellOrderItem>()).Returns(Task.CompletedTask);
            _orderRepository.UpdateAsync(Arg.Any<SellOrder>()).Returns(Task.CompletedTask);

            // Act
            var result = await _orderService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SO2603240001", result.SellOrderCode);
            await _serialNumberService.Received(1).GenerateNextAsync(ModuleCodes.SalesOrder);
            Assert.Equal("CUST-001", result.CustomerId);
            await _orderRepository.Received(1).AddAsync(Arg.Any<SellOrder>());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnSalesOrder()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001" };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            var result = await _orderService.GetByIdAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateSalesOrder()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001" };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);
            _orderRepository.UpdateAsync(Arg.Any<SellOrder>()).Returns(Task.CompletedTask);
            var updateRequest = new UpdateSalesOrderRequest { Comment = "订单已更新" };

            // Act
            var result = await _orderService.UpdateAsync(orderId, updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("订单已更新", result.Comment);
            await _orderRepository.Received(1).UpdateAsync(Arg.Any<SellOrder>());
        }

        [Fact]
        public async Task UpdateStatusAsync_ConfirmOrder_ShouldSetConfirmedStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001", Status = SellOrderMainStatus.New };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.PendingAudit);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.PendingAudit));
        }

        [Fact]
        public async Task UpdateStatusAsync_ApprovedFromPendingAudit_ShouldSetApprovedAndClearAuditRemark()
        {
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = SellOrderMainStatus.PendingAudit,
                AuditRemark = "old"
            };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.Approved);

            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o =>
                o.Status == SellOrderMainStatus.Approved && o.AuditRemark == null));
        }

        [Fact]
        public async Task UpdateStatusAsync_AuditFailedFromPendingAudit_ShouldSaveRemark()
        {
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = SellOrderMainStatus.PendingAudit
            };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.AuditFailed, "价格异常");

            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o =>
                o.Status == SellOrderMainStatus.AuditFailed && o.AuditRemark == "价格异常"));
        }

        [Fact]
        public async Task UpdateStatusAsync_ToInProgress_ShouldSetStatus3()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001", Status = SellOrderMainStatus.Approved };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.InProgress);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.InProgress));
        }

        [Fact]
        public async Task UpdateStatusAsync_CompleteOrder_ShouldSetCompletedStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001", Status = SellOrderMainStatus.InProgress };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.Completed);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.Completed));
        }

        [Fact]
        public async Task UpdateStatusAsync_CancelOrder_ShouldSetCancelledStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001", Status = SellOrderMainStatus.New };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.UpdateStatusAsync(orderId, SellOrderMainStatus.Cancelled);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.Cancelled));
        }

        [Fact]
        public async Task RequestStockOutAsync_ValidOrder_ShouldSetInProgressStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001", Status = SellOrderMainStatus.Approved };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.RequestStockOutAsync(orderId, "USER-001");

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == SellOrderMainStatus.InProgress));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllOrders()
        {
            // Arrange
            var orders = new List<SellOrder>
            {
                new() { Id = "1", SellOrderCode = "SO-001", CustomerId = "C1" },
                new() { Id = "2", SellOrderCode = "SO-002", CustomerId = "C2" },
                new() { Id = "3", SellOrderCode = "SO-003", CustomerId = "C3" }
            };
            _orderRepository.GetAllAsync().Returns(orders);

            // Act
            var result = await _orderService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ShouldDeleteSalesOrder()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder { Id = orderId, SellOrderCode = "SO-2024-001" };
            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);
            _orderItemRepository.GetAllAsync().Returns(new List<SellOrderItem>());

            // Act
            await _orderService.DeleteAsync(orderId);

            // Assert
            await _orderRepository.Received(1).DeleteAsync(orderId);
        }
    }
}
