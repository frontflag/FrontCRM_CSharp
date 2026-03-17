using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services
{
    /// <summary>
    /// RFQ询价单服务测试
    /// 注意: RFQ 实体使用 RfqCode (camelCase) 而非 RFQCode
    /// </summary>
    public class RFQServiceTests
    {
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IRepository<RFQItem> _rfqItemRepository;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RFQService _rfqService;

        public RFQServiceTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _rfqItemRepository = Substitute.For<IRepository<RFQItem>>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _unitOfWork = Substitute.For<IUnitOfWork>();

            // 默认序列号生成
            _serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");

            _rfqService = new RFQService(
                _rfqRepository,
                _rfqItemRepository,
                null!,
                _unitOfWork,
                _serialNumberService);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateRFQ()
        {
            // Arrange
            var request = new CreateRFQRequest
            {
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                RfqDate = DateTime.UtcNow
            };
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

            // Act
            var result = await _rfqService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("RF20260001", result.RfqCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(0, result.Status); // 草稿状态
            Assert.NotNull(result.Id);
            await _rfqRepository.Received(1).AddAsync(Arg.Any<RFQ>());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnRFQ()
        {
            // Arrange
            var rfqId = "RFQ-123";
            var expectedRFQ = new RFQ
            {
                Id = rfqId,
                RfqCode = "RF20260001",
                CustomerId = "CUST-001",
                Status = 1
            };
            _rfqRepository.GetByIdAsync(rfqId).Returns(expectedRFQ);

            // Act
            var result = await _rfqService.GetByIdAsync(rfqId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(rfqId, result.Id);
            Assert.Equal(expectedRFQ.RfqCode, result.RfqCode);
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
                RfqCode = "RF20260001",
                CustomerId = "CUST-001",
                Status = 0
            };
            _rfqRepository.GetByIdAsync("RFQ-123").Returns(existingRFQ);
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

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
                RfqCode = "RF20260001",
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
                RfqCode = "RF20260001"
            };
            _rfqRepository.GetByIdAsync(rfqId).Returns(existingRFQ);
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

            // Act
            await _rfqService.DeleteAsync(rfqId);

            // Assert
            await _rfqRepository.Received(1).DeleteAsync(rfqId);
        }

        [Fact]
        public async Task GetByCustomerIdAsync_ShouldReturnCustomerRFQs()
        {
            // Arrange
            var customerId = "CUST-001";
            var rfqs = new List<RFQ>
            {
                new() { Id = "1", RfqCode = "RF001", CustomerId = customerId },
                new() { Id = "2", RfqCode = "RF002", CustomerId = customerId },
                new() { Id = "3", RfqCode = "RF003", CustomerId = "CUST-002" }
            };
            _rfqRepository.GetAllAsync().Returns(rfqs);

            // Act
            var pagedRequest = new RFQQueryRequest { CustomerId = customerId, PageIndex = 1, PageSize = 20 };
            var result = await _rfqService.GetPagedAsync(pagedRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
        }
    }
}
