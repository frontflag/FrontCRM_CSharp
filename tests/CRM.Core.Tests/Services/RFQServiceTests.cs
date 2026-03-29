using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.System;
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
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUserService _userService;
        private readonly IEntityLookupService _entityLookup;
        private readonly IRepository<SysParam> _sysParamRepo;
        private readonly IRepository<RbacRole> _rbacRoleRepo;
        private readonly IRepository<RbacUserRole> _rbacUserRoleRepo;
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<User> _userRepo;
        private readonly RFQService _rfqService;

        public RFQServiceTests()
        {
            _rfqRepository = Substitute.For<IRepository<RFQ>>();
            _rfqItemRepository = Substitute.For<IRepository<RFQItem>>();
            _serialNumberService = Substitute.For<ISerialNumberService>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _dataPermissionService = Substitute.For<IDataPermissionService>();
            _userService = Substitute.For<IUserService>();
            _userService.GetAllAsync().Returns(new List<CRM.Core.Models.User>());
            _entityLookup = Substitute.For<IEntityLookupService>();
            _sysParamRepo = Substitute.For<IRepository<SysParam>>();
            _rbacRoleRepo = Substitute.For<IRepository<RbacRole>>();
            _rbacUserRoleRepo = Substitute.For<IRepository<RbacUserRole>>();
            _quoteRepo = Substitute.For<IRepository<Quote>>();
            _userRepo = Substitute.For<IRepository<User>>();
            _sysParamRepo.FindAsync(Arg.Any<Expression<Func<SysParam, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SysParam>>(Array.Empty<SysParam>()));
            _rbacRoleRepo.GetAllAsync().Returns(new List<RbacRole>());
            _rbacUserRoleRepo.GetAllAsync().Returns(new List<RbacUserRole>());
            _quoteRepo.GetAllAsync().Returns(new List<Quote>());
            _userRepo.GetAllAsync().Returns(new List<User>());

            // 默认序列号生成
            _serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");

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
                _quoteRepo,
                _userRepo);
        }

        private static CreateRFQRequest BuildValidCreateRequest(Action<CreateRFQRequest>? tweak = null)
        {
            var r = new CreateRFQRequest
            {
                CustomerId = "CUST-001",
                SalesUserId = "USER-001",
                Items =
                {
                    new CreateRFQItemRequest
                    {
                        Mpn = "MPN-001",
                        Brand = "Brand-A",
                        Quantity = 1
                    }
                }
            };
            tweak?.Invoke(r);
            return r;
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_ShouldCreateRFQ()
        {
            var request = BuildValidCreateRequest();
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

            var result = await _rfqService.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal("RF20260001", result.RfqCode);
            Assert.Equal(request.CustomerId, result.CustomerId);
            Assert.Equal(0, result.Status);
            Assert.NotNull(result.Id);
            await _rfqRepository.Received(1).AddAsync(Arg.Any<RFQ>());
            await _rfqItemRepository.Received(1).AddAsync(Arg.Any<RFQItem>());
            await _unitOfWork.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CreateAsync_ItemExpiryDateUnspecified_IsPassedAsUtcKind()
        {
            var expiry = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var request = BuildValidCreateRequest();
            request.Items[0].ExpiryDate = expiry;
            _rfqRepository.GetAllAsync().Returns(new List<RFQ>());
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

            await _rfqService.CreateAsync(request);

            await _rfqItemRepository.Received(1).AddAsync(Arg.Is<RFQItem>(i =>
                i.ExpiryDate.HasValue &&
                i.ExpiryDate.Value.Kind == DateTimeKind.Utc &&
                i.ExpiryDate.Value.Ticks == expiry.Ticks));
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
