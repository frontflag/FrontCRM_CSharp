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
        private readonly QuoteService _quoteService;

        public QuoteServiceTests()
        {
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _quoteService = new QuoteService(_quoteRepository);
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
                ValidUntil = DateTime.UtcNow.AddDays(30),
                TotalAmount = 1000m,
                TaxAmount = 130m,
                GrandTotal = 1130m
            };

            _quoteRepository.GetAllAsync().Returns(new List<Quote>());

            Quote? capturedQuote = null;
            _quoteRepository.When(r => r.AddAsync(Arg.Any<Quote>()))
                .Do(call => capturedQuote = call.Arg<Quote>());

            // Act
            var result = await _quoteService.CreateAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.QuoteCode, result.QuoteCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(request.TotalAmount, result.TotalAmount);
            Assert.Equal(request.TaxAmount, result.TaxAmount);
            Assert.Equal(request.GrandTotal, result.TotalAmountWithTax);
            Assert.Equal(0, result.Status); // 草稿状态
            await _quoteRepository.Received(1).AddAsync(Arg.Any<Quote>());
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ShouldThrowException()
        {
            // Arrange
            var existingQuote = new Quote
            {
                Id = "1",
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001"
            };

            _quoteRepository.GetAllAsync().Returns(new List<Quote> { existingQuote });

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
                TotalAmount = 5000m,
                Status = 1 // 已审批
            };

            _quoteRepository.GetByIdAsync(quoteId).Returns(expectedQuote);

            // Act
            var result = await _quoteService.GetByIdAsync(quoteId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(quoteId, result.Id);
            Assert.Equal(expectedQuote.TotalAmount, result.TotalAmount);
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
                TotalAmount = 1000m,
                Status = 0
            };

            _quoteRepository.GetByIdAsync("QT-123").Returns(existingQuote);

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
                Status = 0, // 草稿
                TotalAmount = 1000m
            };

            _quoteRepository.GetByIdAsync(quoteId).Returns(existingQuote);

            // Act - 使用状态码1表示已审批
            await _quoteService.UpdateStatusAsync(quoteId, 1);

            // Assert
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 1));
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

            // Act - 使用状态码2表示已拒绝
            await _quoteService.UpdateStatusAsync(quoteId, 2);

            // Assert
            await _quoteRepository.Received(1).UpdateAsync(Arg.Is<Quote>(q => q.Status == 2));
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

            // Act
            await _quoteService.DeleteAsync(quoteId);

            // Assert
            await _quoteRepository.Received(1).DeleteAsync(quoteId);
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
