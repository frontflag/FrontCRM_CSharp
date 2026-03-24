using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services
{
    /// <summary>
    /// Quote报价单服务测试
    /// </summary>
    public class QuoteServiceTests
    {
        private readonly IRepository<Quote> _quoteRepository;
        private readonly IRepository<QuoteItem> _quoteItemRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly QuoteService _quoteService;

        public QuoteServiceTests()
        {
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _quoteItemRepository = Substitute.For<IRepository<QuoteItem>>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _quoteService = new QuoteService(_quoteRepository, _quoteItemRepository, _unitOfWork);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateQuote()
        {
            // Arrange
            var request = new CreateQuoteRequest
            {
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                PurchaseUserId = "USER-002",
                QuoteDate = DateTime.UtcNow,
                Mpn = "REF3430QDBVRQ1",
                Remark = "测试报价"
            };
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            // Act
            var result = await _quoteService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.QuoteCode, result.QuoteCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(request.Mpn, result.Mpn);
            Assert.Equal(0, result.Status); // 草稿状态
            await _quoteRepository.Received(1).AddAsync(Arg.Any<Quote>());
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_DuplicateQuoteCode_ShouldThrowException()
        {
            // Arrange
            var existingQuotes = new List<Quote>
            {
                new() { Id = "1", QuoteCode = "QT-2024-001", CustomerId = "CUST-001" }
            };
            _quoteRepository.GetAllAsync().Returns(existingQuotes);

            var request = new CreateQuoteRequest
            {
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-002"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _quoteService.CreateAsync(request));
            Assert.Contains("QT-2024-001", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task CreateAsync_EmptyQuoteCode_ShouldThrowException(string quoteCode)
        {
            // Arrange
            var request = new CreateQuoteRequest
            {
                QuoteCode = quoteCode,
                CustomerId = "CUST-001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _quoteService.CreateAsync(request));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnQuote()
        {
            // Arrange
            var quoteId = "QT-123";
            var expectedQuote = new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                Status = 1
            };
            _quoteRepository.GetByIdAsync(quoteId).Returns(expectedQuote);

            // Act
            var result = await _quoteService.GetByIdAsync(quoteId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(quoteId, result.Id);
            Assert.Equal(expectedQuote.Status, result.Status);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateQuote()
        {
            // Arrange
            var existingQuote = new Quote
            {
                Id = "QT-123",
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                Status = 0
            };
            _quoteRepository.GetByIdAsync("QT-123").Returns(existingQuote);
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            var updateRequest = new UpdateQuoteRequest
            {
                Remark = "报价已更新"
            };

            // Act
            var result = await _quoteService.UpdateAsync("QT-123", updateRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("报价已更新", result.Remark);
            await _quoteRepository.Received(1).UpdateAsync(Arg.Any<Quote>());
        }

        [Fact]
        public async Task UpdateStatusAsync_ApproveQuote_ShouldSetApprovedStatus()
        {
            // Arrange
            var quoteId = "QT-123";
            var existingQuote = new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001",
                Status = 0 // 草稿
            };
            _quoteRepository.GetByIdAsync(quoteId).Returns(existingQuote);

            // Act
            await _quoteService.UpdateStatusAsync(quoteId, 1);

            // Assert
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 1));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateStatusAsync_RejectQuote_ShouldSetRejectedStatus()
        {
            // Arrange
            var quoteId = "QT-123";
            var existingQuote = new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001",
                Status = 0 // 草稿
            };
            _quoteRepository.GetByIdAsync(quoteId).Returns(existingQuote);

            // Act
            await _quoteService.UpdateStatusAsync(quoteId, 5); // 5 = 已拒绝

            // Assert
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 5));
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ShouldDeleteQuote()
        {
            // Arrange
            var quoteId = "QT-123";
            var existingQuote = new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001"
            };
            _quoteRepository.GetByIdAsync(quoteId).Returns(existingQuote);
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            // Act
            await _quoteService.DeleteAsync(quoteId);

            // Assert
            await _quoteRepository.Received(1).DeleteAsync(quoteId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllQuotes()
        {
            // Arrange
            var quotes = new List<Quote>
            {
                new() { Id = "1", QuoteCode = "QT-001", CustomerId = "C1" },
                new() { Id = "2", QuoteCode = "QT-002", CustomerId = "C2" },
                new() { Id = "3", QuoteCode = "QT-003", CustomerId = "C3" }
            };
            _quoteRepository.GetAllAsync().Returns(quotes);

            // Act
            var result = await _quoteService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
        }
    }
}
