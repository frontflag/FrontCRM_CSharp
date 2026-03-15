using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services
{
    /// <summary>
    /// SalesOrder销售订单服务测试
    /// </summary>
    public class SalesOrderServiceTests
    {
        private readonly IRepository<SellOrder> _orderRepository;
        private readonly SalesOrderService _orderService;

        public SalesOrderServiceTests()
        {
            _orderRepository = Substitute.For<IRepository<SellOrder>>();
            _orderService = new SalesOrderService(_orderRepository);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateSalesOrder()
        {
            // Arrange
            var request = new CreateSalesOrderRequest
            {
                OrderCode = "SO-2024-001",
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                DeliveryDate = DateTime.UtcNow.AddDays(7),
                TotalAmount = 5000m,
                GrandTotal = 5650m,
                Currency = 1, // CNY
                PaymentTerms = "30天账期"
            };

            _orderRepository.GetAllAsync().Returns(new List<SellOrder>());

            SellOrder? capturedOrder = null;
            _orderRepository.When(r => r.AddAsync(Arg.Any<SellOrder>()))
                .Do(call => capturedOrder = call.Arg<SellOrder>());

            // Act
            var result = await _orderService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.OrderCode, result.SellOrderCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(request.TotalAmount, result.Total);
            Assert.Equal(request.GrandTotal, result.ConvertTotal);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(0, result.Status); // 草稿状态
            await _orderRepository.Received(1).AddAsync(Arg.Any<SellOrder>());
        }

        [Fact]
        public async Task CreateAsync_DuplicateOrderCode_ShouldThrowException()
        {
            // Arrange
            var existingOrder = new SellOrder
            {
                Id = "1",
                SellOrderCode = "SO-2024-001",
                CustomerId = "CUST-001"
            };

            _orderRepository.GetAllAsync().Returns(new List<SellOrder> { existingOrder });

            var request = new CreateSalesOrderRequest
            {
                OrderCode = "SO-2024-001",
                CustomerId = "CUST-002"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orderService.CreateAsync(request));
            Assert.Contains("SO-2024-001", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task CreateAsync_EmptyOrderCode_ShouldThrowException(string orderCode)
        {
            // Arrange
            var request = new CreateSalesOrderRequest
            {
                OrderCode = orderCode,
                CustomerId = "CUST-001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _orderService.CreateAsync(request));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnSalesOrder()
        {
            // Arrange
            var orderId = "SO-123";
            var expectedOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                CustomerId = "CUST-001",
                Total = 10000m,
                Status = 1
            };

            _orderRepository.GetByIdAsync(orderId).Returns(expectedOrder);

            // Act
            var result = await _orderService.GetByIdAsync(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal(expectedOrder.Total, result.Total);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateSalesOrder()
        {
            // Arrange
            var existingOrder = new SellOrder
            {
                Id = "SO-123",
                SellOrderCode = "SO-2024-001",
                CustomerId = "CUST-001",
                Total = 5000m,
                Status = 0
            };

            _orderRepository.GetByIdAsync("SO-123").Returns(existingOrder);

            var updateRequest = new UpdateSalesOrderRequest
            {
                Remark = "订单已更新"
            };

            // Act
            var result = await _orderService.UpdateAsync("SO-123", updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("订单已更新", result.Remark);
            await _orderRepository.Received(1).UpdateAsync(Arg.Any<SellOrder>());
        }

        [Fact]
        public async Task UpdateStatusAsync_ConfirmOrder_ShouldSetConfirmedStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = 0, // 草稿
                Total = 5000m
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act - 使用状态码1表示已确认
            await _orderService.UpdateStatusAsync(orderId, 1);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 1));
        }

        [Fact]
        public async Task UpdateStatusAsync_ShipOrder_ShouldSetShippedStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = 1 // 已确认
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act - 使用状态码2表示已发货
            await _orderService.UpdateStatusAsync(orderId, 2);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 2));
        }

        [Fact]
        public async Task UpdateStatusAsync_CompleteOrder_ShouldSetCompletedStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = 2 // 已发货
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act - 使用状态码3表示已完成
            await _orderService.UpdateStatusAsync(orderId, 3);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 3));
        }

        [Fact]
        public async Task UpdateStatusAsync_CancelOrder_ShouldSetCancelledStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = 0 // 草稿
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act - 使用状态码9表示已取消
            await _orderService.UpdateStatusAsync(orderId, 9);

            // Assert
            await _orderRepository.Received(1).UpdateAsync(Arg.Is<SellOrder>(o => o.Status == 9));
        }

        [Fact]
        public async Task RequestStockOutAsync_ValidOrder_ShouldSetStockOutStatus()
        {
            // Arrange
            var orderId = "SO-123";
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001",
                Status = 1, // 已确认
                StockOutStatus = 0 // 未出库
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.RequestStockOutAsync(orderId, "USER-001");

            // Assert - StockOutStatus 设置为 1 表示待出库
            await _orderRepository.Received(1).UpdateAsync(
                Arg.Is<SellOrder>(o => o.StockOutStatus == 1));
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
            var existingOrder = new SellOrder
            {
                Id = orderId,
                SellOrderCode = "SO-2024-001"
            };

            _orderRepository.GetByIdAsync(orderId).Returns(existingOrder);

            // Act
            await _orderService.DeleteAsync(orderId);

            // Assert
            await _orderRepository.Received(1).DeleteAsync(orderId);
        }
    }
}
