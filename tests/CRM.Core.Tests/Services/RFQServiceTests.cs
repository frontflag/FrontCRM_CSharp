using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services
{
    /// <summary>
    /// RFQ询价单服务测试
    /// </summary>
    public class RFQServiceTests
    {
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly RFQService _rfqService;

        public RFQServiceTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _rfqService = new RFQService(_rfqRepository);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateRFQ()
        {
            // Arrange
            var request = new CreateRFQRequest
            {
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                RFQDate = DateTime.UtcNow
            };

            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());

            RFQ? capturedRFQ = null;
            _rfqRepository.When(r => r.AddAsync(Arg.Any<RFQ>()))
                .Do(call => capturedRFQ = call.Arg<RFQ>());

            // Act
            var result = await _rfqService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.RFQCode, result.RFQCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(request.SalesUserId, result.SalesUserId);
            Assert.Equal(0, result.Status); // 草稿状态
            Assert.NotNull(result.Id);
            await _rfqRepository.Received(1).AddAsync(Arg.Any<RFQ>());
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ShouldThrowException()
        {
            // Arrange
            var existingRFQ = new RFQ
            {
                Id = "1",
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-001"
            };

            _rfqRepository.GetAllAsync().Returns(new List<RFQ> { existingRFQ });

            var request = new CreateRFQRequest
            {
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-002"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _rfqService.CreateAsync(request));
            Assert.Contains("RFQ-2024-001", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task CreateAsync_EmptyRFQCode_ShouldThrowException(string rfqCode)
        {
            // Arrange
            var request = new CreateRFQRequest
            {
                RFQCode = rfqCode,
                CustomerId = "CUST-001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _rfqService.CreateAsync(request));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnRFQ()
        {
            // Arrange
            var rfqId = "RFQ-123";
            var expectedRFQ = new RFQ
            {
                Id = rfqId,
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-001",
                Status = 1
            };

            _rfqRepository.GetByIdAsync(rfqId).Returns(expectedRFQ);

            // Act
            var result = await _rfqService.GetByIdAsync(rfqId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rfqId, result.Id);
            Assert.Equal(expectedRFQ.RFQCode, result.RFQCode);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            _rfqRepository.GetByIdAsync("non-existing").Returns((RFQ?)null);

            // Act
            var result = await _rfqService.GetByIdAsync("non-existing");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateRFQ()
        {
            // Arrange
            var existingRFQ = new RFQ
            {
                Id = "RFQ-123",
                RFQCode = "RFQ-2024-001",
                CustomerId = "CUST-001",
                Status = 0
            };

            _rfqRepository.GetByIdAsync("RFQ-123").Returns(existingRFQ);

            var updateRequest = new UpdateRFQRequest
            {
                Remark = "更新备注"
            };

            // Act
            var result = await _rfqService.UpdateAsync("RFQ-123", updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("更新备注", result.Remark);
            await _rfqRepository.Received(1).UpdateAsync(Arg.Any<RFQ>());
        }

        [Fact]
        public async Task UpdateStatusAsync_ValidRequest_ShouldUpdateStatus()
        {
            // Arrange
            var existingRFQ = new RFQ
            {
                Id = "RFQ-123",
                RFQCode = "RFQ-2024-001",
                Status = 0
            };

            _rfqRepository.GetByIdAsync("RFQ-123").Returns(existingRFQ);

            // Act
            await _rfqService.UpdateStatusAsync("RFQ-123", 1);

            // Assert
            await _rfqRepository.Received(1).UpdateAsync(Arg.Is<RFQ>(r => r.Status == 1));
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ShouldDeleteRFQ()
        {
            // Arrange
            var rfqId = "RFQ-123";
            var existingRFQ = new RFQ
            {
                Id = rfqId,
                RFQCode = "RFQ-2024-001"
            };

            _rfqRepository.GetByIdAsync(rfqId).Returns(existingRFQ);

            // Act
            await _rfqService.DeleteAsync(rfqId);

            // Assert
            await _rfqRepository.Received(1).DeleteAsync(rfqId);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllRFQs()
        {
            // Arrange
            var rfqs = new List<RFQ>
            {
                new() { Id = "1", RFQCode = "RFQ-001", CustomerId = "C1" },
                new() { Id = "2", RFQCode = "RFQ-002", CustomerId = "C2" },
                new() { Id = "3", RFQCode = "RFQ-003", CustomerId = "C3" }
            };

            _rfqRepository.GetAllAsync().Returns(rfqs);

            // Act
            var result = await _rfqService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetByCustomerIdAsync_ShouldReturnCustomerRFQs()
        {
            // Arrange
            var customerId = "CUST-001";
            var rfqs = new List<RFQ>
            {
                new() { Id = "1", RFQCode = "RFQ-001", CustomerId = customerId },
                new() { Id = "2", RFQCode = "RFQ-002", CustomerId = customerId },
                new() { Id = "3", RFQCode = "RFQ-003", CustomerId = "CUST-002" }
            };

            _rfqRepository.GetAllAsync().Returns(rfqs);

            // Act
            var result = await _rfqService.GetByCustomerIdAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
    }
}
