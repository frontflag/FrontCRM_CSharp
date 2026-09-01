using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly IRepository<RFQItem> _rfqItemRepository;
        private readonly IRepository<RFQ> _rfqRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUserService _userService;
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IRepository<VendorInfo> _vendorRepository;
        private readonly IVendorService _vendorService;
        private readonly IVendorTradeCountQuery _vendorTradeCountQuery;
        private readonly IQuoteListQuery _quoteListQuery;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly QuoteService _quoteService;

        private const string RfqItemId = "rfq-item-1";
        private const string RfqId = "rfq-1";

        private const string ActingUserId = "user-test-1";

        public QuoteServiceTests()
        {
            _quoteRepository = Substitute.For<IRepository<Quote>>();
            _quoteItemRepository = Substitute.For<IRepository<QuoteItem>>();
            _rfqItemRepository = Substitute.For<IRepository<RFQItem>>();
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _userService = Substitute.For<IUserService>();
            _userService.GetAllAsync().Returns(new List<User>());
            _userService.GetByIdAsync(ActingUserId).Returns(new User { Id = ActingUserId, UserName = "tester" });
            _customerRepository = Substitute.For<IRepository<CustomerInfo>>();
            _customerRepository.FindAsync(Arg.Any<Expression<Func<CustomerInfo, bool>>>())
                .Returns(Task.FromResult<IEnumerable<CustomerInfo>>(Array.Empty<CustomerInfo>()));
            _vendorRepository = Substitute.For<IRepository<VendorInfo>>();
            _vendorRepository.FindAsync(Arg.Any<Expression<Func<VendorInfo, bool>>>())
                .Returns(Task.FromResult<IEnumerable<VendorInfo>>(Array.Empty<VendorInfo>()));
            _vendorService = Substitute.For<IVendorService>();
            _vendorTradeCountQuery = Substitute.For<IVendorTradeCountQuery>();
            _vendorTradeCountQuery.GetTradeCountsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var ids = call.ArgAt<IReadOnlyCollection<string>>(0) ?? Array.Empty<string>();
                    IReadOnlyDictionary<string, int> map = ids
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(x => x, _ => 0, StringComparer.OrdinalIgnoreCase);
                    return Task.FromResult(map);
                });
            _serialNumberService.GenerateNextAsync(ModuleCodes.Quotation).Returns("QT2603240001");
            _quoteListQuery = Substitute.For<IQuoteListQuery>();
            _quoteListQuery.GetPagedAsync(Arg.Any<QuoteQueryRequest>(), default)
                .Returns(Task.FromResult(new PagedResult<Quote>
                {
                    Items = Array.Empty<Quote>(),
                    TotalCount = 0,
                    PageIndex = 1,
                    PageSize = 20
                }));
            _quoteListQuery.GetQuoteCountsByRfqItemIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), default)
                .Returns(Task.FromResult((IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));
            var rbacService = Substitute.For<IRbacService>();
            rbacService.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(Task.FromResult(new UserPermissionSummaryDto { IsSysAdmin = true }));

            _rfqItemRepository.GetByIdAsync(RfqItemId).Returns(new RFQItem
            {
                Id = RfqItemId,
                RfqId = RfqId,
                LineNo = 3,
                Mpn = "REF3430QDBVRQ1",
                Brand = "TI",
                Status = (short)RfqItemStatus.Pending
            });

            _logOperationAppend = Substitute.For<ILogOperationAppendService>();
            _quoteService = new QuoteService(
                _quoteRepository,
                _quoteItemRepository,
                _rfqItemRepository,
                _rfqRepository,
                _customerRepository,
                _vendorRepository,
                _vendorService,
                _vendorTradeCountQuery,
                _unitOfWork,
                _serialNumberService,
                _userService,
                _quoteListQuery,
                rbacService,
                NullLogger<QuoteService>.Instance,
                _logOperationAppend,
                Substitute.For<IPurchaseQuoterPoolService>());
        }

        private static CreateQuoteRequest BuildValidCreateRequest() => new()
        {
            QuoteCode = "QT-2024-001",
            RFQId = RfqId,
            RFQItemId = RfqItemId,
            CustomerId = "CUST-001",
            SalesUserId = "USER-001",
            PurchaseUserId = "USER-002",
            QuoteDate = DateTime.UtcNow,
            Mpn = "REF3430QDBVRQ1",
            Remark = "测试报价",
            Items = new List<CreateQuoteItemRequest>
            {
                new() { Quantity = 1, UnitPrice = 1.5m, Mpn = "REF3430QDBVRQ1" }
            }
        };

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateQuote()
        {
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            var request = BuildValidCreateRequest();
            var result = await _quoteService.CreateAsync(request, ActingUserId);

            Assert.NotNull(result);
            Assert.Equal("QT2603240001", result.QuoteCode);
            Assert.Equal(RfqId, result.RFQId);
            Assert.Equal(RfqItemId, result.RFQItemId);
            Assert.Equal((short)QuoteMainStatus.New, result.Status);
            await _quoteRepository.Received(1).AddAsync(Arg.Any<Quote>());
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_WithoutRfqItemId_ShouldThrow()
        {
            var request = BuildValidCreateRequest();
            request.RFQItemId = null;
            await Assert.ThrowsAsync<ArgumentException>(() => _quoteService.CreateAsync(request));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnQuote()
        {
            var quoteId = "QT-123";
            var expectedQuote = new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                Status = (short)QuoteMainStatus.Won
            };
            _quoteRepository.GetByIdAsync(quoteId).Returns(expectedQuote);
            _quoteItemRepository.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(Array.Empty<QuoteItem>()));

            var result = await _quoteService.GetByIdAsync(quoteId);

            Assert.NotNull(result);
            Assert.Equal(quoteId, result.Id);
            Assert.Equal(expectedQuote.Status, result.Status);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_ShouldUpdateQuote()
        {
            var existingQuote = new Quote
            {
                Id = "QT-123",
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                RFQItemId = RfqItemId,
                PurchaseUserId = "USER-002",
                Status = (short)QuoteMainStatus.New
            };
            _quoteRepository.GetByIdAsync("QT-123").Returns(existingQuote);
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            var result = await _quoteService.UpdateAsync("QT-123", new UpdateQuoteRequest { Remark = "报价已更新" }, ActingUserId);

            Assert.Equal("报价已更新", result.Remark);
            await _quoteRepository.Received(1).UpdateAsync(Arg.Any<Quote>());
            await _unitOfWork.Received().ExecuteAsync(Arg.Is<string>(s =>
                s.Contains("log_change_fldval") && s.Contains("remark") && s.Contains("报价已更新")));
        }

        [Fact]
        public async Task CreateAsync_WithItems_ShouldWriteLineAddedChangeLog()
        {
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            var request = BuildValidCreateRequest();
            await _quoteService.CreateAsync(request, ActingUserId);

            await _unitOfWork.Received().ExecuteAsync(Arg.Is<string>(s =>
                s.Contains("log_change_fldval") && s.Contains("lineAdded")));
        }

        [Fact]
        public async Task UpdateAsync_WonQuote_ShouldThrow()
        {
            _quoteRepository.GetByIdAsync("QT-123").Returns(new Quote
            {
                Id = "QT-123",
                Status = (short)QuoteMainStatus.Won
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _quoteService.UpdateAsync("QT-123", new UpdateQuoteRequest { Remark = "x" }));
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldRejectManualChange()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _quoteService.UpdateStatusAsync("QT-123", 1));
        }

        [Fact]
        public async Task DeleteAsync_WonQuote_ShouldThrow()
        {
            _quoteRepository.GetByIdAsync("QT-123").Returns(new Quote
            {
                Id = "QT-123",
                Status = (short)QuoteMainStatus.Won
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _quoteService.DeleteAsync("QT-123"));
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ShouldDeleteQuote()
        {
            var quoteId = "QT-123";
            _quoteRepository.GetByIdAsync(quoteId).Returns(new Quote
            {
                Id = quoteId,
                QuoteCode = "QT-2024-001",
                RFQItemId = RfqItemId,
                Status = (short)QuoteMainStatus.New
            });
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            _quoteRepository.FindAsync(Arg.Any<Expression<Func<Quote, bool>>>())
                .Returns(Task.FromResult<IEnumerable<Quote>>(Array.Empty<Quote>()));

            await _quoteService.DeleteAsync(quoteId);

            await _quoteRepository.Received(1).DeleteAsync(quoteId);
            await _unitOfWork.Received(1).SaveChangesAsync();
            await _logOperationAppend.Received(1).AppendDeleteAsync(
                Arg.Is<CRM.Core.Models.System.DeleteOperationLogEntry>(e =>
                    e.ActionTypeOverride == OperationLogActionTypes.QuoteHeaderDelete
                    && e.RecordCode == "QT-2024-001"
                    && e.ExtraInfo != null
                    && e.ExtraInfo.Contains(RfqItemId)
                    && e.OperationDescOverride != null
                    && e.OperationDescOverride.Contains("行号 3")),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetDeletedQuotesByRfqItemIdsAsync_Empty_ReturnsEmpty()
        {
            var result = await _quoteService.GetDeletedQuotesByRfqItemIdsAsync(Array.Empty<string>());
            Assert.Empty(result);
            await _unitOfWork.DidNotReceive().QueryAsync<QuoteDeletedOnRfqItemDto>(Arg.Any<string>());
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllQuotes()
        {
            var quotes = new List<Quote>
            {
                new() { Id = "1", QuoteCode = "QT-001", CustomerId = "C1" },
                new() { Id = "2", QuoteCode = "QT-002", CustomerId = "C2" },
                new() { Id = "3", QuoteCode = "QT-003", CustomerId = "C3" }
            };
            _quoteRepository.GetAllAsync().Returns(quotes);

            var result = await _quoteService.GetAllAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetPagedAsync_ShouldShowRfqSalesUser_WhenQuoteSalesUserIsPurchaser()
        {
            const string purchaserId = "USER-PURCH";
            const string salesId = "USER-SALES";
            var quote = new Quote
            {
                Id = "QT-1",
                QuoteCode = "QT1",
                RFQId = RfqId,
                RFQItemId = RfqItemId,
                SalesUserId = purchaserId,
                PurchaseUserId = purchaserId
            };
            _quoteListQuery.GetPagedAsync(Arg.Any<QuoteQueryRequest>(), default)
                .Returns(new PagedResult<Quote>
                {
                    Items = new[] { quote },
                    TotalCount = 1,
                    PageIndex = 1,
                    PageSize = 20
                });
            _quoteItemRepository.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(Array.Empty<QuoteItem>()));
            _rfqRepository.FindAsync(Arg.Any<Expression<Func<RFQ, bool>>>())
                .Returns(Task.FromResult<IEnumerable<RFQ>>(new[]
                {
                    new RFQ { Id = RfqId, RfqCode = "RFQ1", SalesUserId = salesId }
                }));
            _userService.GetAllAsync().Returns(new List<User>
            {
                new() { Id = purchaserId, UserName = "Alina" },
                new() { Id = salesId, UserName = "Janetta" }
            });

            var page = await _quoteService.GetPagedAsync(new QuoteQueryRequest { Page = 1, PageSize = 20 });

            var row = Assert.Single(page.Items);
            Assert.Equal("Alina", row.PurchaseUserName);
            Assert.Equal("Janetta", row.SalesUserName);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldHydrateVendorLevel_FromVendorInfo()
        {
            var quote = new Quote { Id = "QT-1", QuoteCode = "QT1" };
            _quoteListQuery.GetPagedAsync(Arg.Any<QuoteQueryRequest>(), default)
                .Returns(new PagedResult<Quote>
                {
                    Items = new[] { quote },
                    TotalCount = 1,
                    PageIndex = 1,
                    PageSize = 20
                });
            _quoteItemRepository.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(new[]
                {
                    new QuoteItem { Id = "QI-1", QuoteId = "QT-1", VendorId = "V-1", VendorName = "Digikey" }
                }));
            _vendorRepository.FindAsync(Arg.Any<Expression<Func<VendorInfo, bool>>>())
                .Returns(Task.FromResult<IEnumerable<VendorInfo>>(new[]
                {
                    new VendorInfo { Id = "V-1", Code = "V1", Level = 1 }
                }));

            var page = await _quoteService.GetPagedAsync(new QuoteQueryRequest { Page = 1, PageSize = 20 });

            var line = Assert.Single(Assert.Single(page.Items).Items);
            Assert.Equal((short)1, line.VendorLevel);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldHydrateVendorTradeCount()
        {
            var quote = new Quote { Id = "QT-1", QuoteCode = "QT1" };
            _quoteListQuery.GetPagedAsync(Arg.Any<QuoteQueryRequest>(), default)
                .Returns(new PagedResult<Quote>
                {
                    Items = new[] { quote },
                    TotalCount = 1,
                    PageIndex = 1,
                    PageSize = 20
                });
            _quoteItemRepository.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(new[]
                {
                    new QuoteItem { Id = "QI-1", QuoteId = "QT-1", VendorId = "V-1", VendorName = "Digikey" }
                }));
            _vendorTradeCountQuery.GetTradeCountsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult((IReadOnlyDictionary<string, int>)new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["V-1"] = 3
                }));

            var page = await _quoteService.GetPagedAsync(new QuoteQueryRequest { Page = 1, PageSize = 20 });

            var line = Assert.Single(Assert.Single(page.Items).Items);
            Assert.Equal(3, line.VendorTradeCount);
        }

        [Fact]
        public async Task CreateAsync_WithoutSalesUserId_ShouldCopyFromRfq()
        {
            const string salesId = "USER-SALES";
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            _rfqRepository.GetByIdAsync(RfqId).Returns(new RFQ { Id = RfqId, SalesUserId = salesId });

            var request = BuildValidCreateRequest();
            request.SalesUserId = null;
            var result = await _quoteService.CreateAsync(request, ActingUserId);

            Assert.Equal(salesId, result.SalesUserId);
        }

        [Fact]
        public async Task CreateAsync_WithVendorLevel_ShouldApplyLevelBeforeSave()
        {
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var request = BuildValidCreateRequest();
            request.Items[0].VendorId = "V-1";
            request.Items[0].VendorLevel = 1;

            await _quoteService.CreateAsync(request, ActingUserId);

            await _vendorService.Received(1).ApplyLevelIfChangedAsync("V-1", (short)1, ActingUserId);
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_WithoutVendorLevel_ShouldNotApplyLevel()
        {
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            var request = BuildValidCreateRequest();
            request.Items[0].VendorId = "V-1";

            await _quoteService.CreateAsync(request, ActingUserId);

            await _vendorService.DidNotReceive().ApplyLevelIfChangedAsync(
                Arg.Any<string>(), Arg.Any<short?>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task CreateAsync_VendorMissing_ShouldFailBeforeSave()
        {
            _quoteRepository.GetAllAsync().Returns(new List<Quote>());
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());
            _vendorService.ApplyLevelIfChangedAsync("V-missing", Arg.Any<short?>(), Arg.Any<string?>())
                .Returns<Task>(_ => throw new ArgumentException("供应商不存在：V-missing"));
            var request = BuildValidCreateRequest();
            request.Items[0].VendorId = "V-missing";
            request.Items[0].VendorLevel = 2;

            await Assert.ThrowsAsync<ArgumentException>(() => _quoteService.CreateAsync(request, ActingUserId));
            await _unitOfWork.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_WithVendorLevel_ShouldApplyLevelBeforeSave()
        {
            _quoteRepository.GetByIdAsync("QT-123").Returns(new Quote
            {
                Id = "QT-123",
                QuoteCode = "QT-2024-001",
                CustomerId = "CUST-001",
                RFQItemId = RfqItemId,
                PurchaseUserId = "USER-002",
                Status = (short)QuoteMainStatus.New
            });
            _quoteItemRepository.FindAsync(Arg.Any<Expression<Func<QuoteItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<QuoteItem>>(Array.Empty<QuoteItem>()));
            _quoteItemRepository.GetAllAsync().Returns(new List<QuoteItem>());

            await _quoteService.UpdateAsync("QT-123", new UpdateQuoteRequest
            {
                Items = new List<CreateQuoteItemRequest>
                {
                    new() { Quantity = 1, UnitPrice = 1.5m, VendorId = "V-1", VendorLevel = 2 }
                }
            }, ActingUserId);

            await _vendorService.Received(1).ApplyLevelIfChangedAsync("V-1", (short)2, ActingUserId);
            await _unitOfWork.Received().SaveChangesAsync();
        }
    }
}
