using System.Linq.Expressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.System;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using CRM.TestCommon.Biz;
using CRM.TestCommon.Rfq;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly IRepository<RbacDepartment> _rbacDepartmentRepo;
        private readonly IRepository<RbacUserDepartment> _rbacUserDepartmentRepo;
        private readonly IRepository<Quote> _quoteRepo;
        private readonly IRepository<RfqCloseRecord> _closeRecordRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRbacService _rbacService;
        private readonly IRfqMainListQuery _rfqMainListQuery;
        private readonly IRfqItemListQuery _rfqItemListQuery;
        private readonly IRfqPurchaserAssignmentOrchestrator _purchaserAssignmentOrchestrator;
        private readonly RFQService _rfqService;

        private static IPurchaseQuoterPoolService CreatePoolServiceWithUsers(params string[] userIds)
        {
            var svc = Substitute.For<IPurchaseQuoterPoolService>();
            svc.GetOrderedActivePoolUserIdsAsync(Arg.Any<CancellationToken>())
                .Returns(userIds.ToList());
            svc.GetAssigneeCountAsync(Arg.Any<CancellationToken>()).Returns(2);
            return svc;
        }

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
            _rbacDepartmentRepo = Substitute.For<IRepository<RbacDepartment>>();
            _rbacUserDepartmentRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            _quoteRepo = Substitute.For<IRepository<Quote>>();
            _closeRecordRepo = Substitute.For<IRepository<RfqCloseRecord>>();
            _userRepo = Substitute.For<IRepository<User>>();
            _sysParamRepo.FindAsync(Arg.Any<Expression<Func<SysParam, bool>>>())
                .Returns(Task.FromResult<IEnumerable<SysParam>>(Array.Empty<SysParam>()));
            _rbacRoleRepo.GetAllAsync().Returns(new List<RbacRole>());
            _rbacUserRoleRepo.GetAllAsync().Returns(new List<RbacUserRole>());
            _rbacDepartmentRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            _rbacUserDepartmentRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            _quoteRepo.GetAllAsync().Returns(new List<Quote>());
            _userRepo.GetAllAsync().Returns(new List<User>());

            // 默认序列号生成
            _serialNumberService.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001");

            _rbacService = Substitute.For<IRbacService>();
            _rbacService.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });

            _rfqMainListQuery = Substitute.For<IRfqMainListQuery>();
            _rfqMainListQuery.GetPagedWithAggregatesAsync(Arg.Any<RFQQueryRequest>(), Arg.Any<CancellationToken>())
                .Returns(async ci =>
                {
                    var req = ci.Arg<RFQQueryRequest>();
                    var all = (await _rfqRepository.GetAllAsync()).ToList();
                    IEnumerable<RFQ> q = all;
                    if (!string.IsNullOrWhiteSpace(req.CustomerId))
                        q = q.Where(r => r.CustomerId == req.CustomerId);
                    var list = q.ToList();
                    var page = req.PageIndex < 1 ? 1 : req.PageIndex;
                    var ps = req.PageSize < 1 ? 20 : req.PageSize;
                    var slice = list.Skip((page - 1) * ps).Take(ps).ToList();
                    return new RfqMainListQueryPage
                    {
                        Items = slice,
                        TotalCount = list.Count,
                        PageIndex = page,
                        PageSize = ps,
                        Aggregates = new RfqMainListAggregates
                        {
                            Total = list.Count,
                            Pending = list.Count(r => r.Status == 0),
                            Processing = list.Count(r => r.Status == 1 || r.Status == 2),
                            Quoted = list.Count(r => r.Status == 3 || r.Status == 4 || r.Status == 5)
                        }
                    };
                });
            _rfqItemListQuery = Substitute.For<IRfqItemListQuery>();
            _rfqItemListQuery.GetPagedAsync(Arg.Any<RFQItemQueryRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new PagedResult<RFQItemListItem>
                {
                    Items = Array.Empty<RFQItemListItem>(),
                    TotalCount = 0,
                    PageIndex = 1,
                    PageSize = 20
                }));

            _purchaserAssignmentOrchestrator = RfqAssignmentTestFactory.CreateEmptyItemRoundRobinOrchestrator(_sysParamRepo);

            _rfqService = new RFQService(
                _rfqRepository,
                _rfqItemRepository,
                null!,
                _entityLookup,
                _unitOfWork,
                _serialNumberService,
                _dataPermissionService,
                _userService,
                _quoteRepo,
                _closeRecordRepo,
                _userRepo,
                _rbacService,
                _purchaserAssignmentOrchestrator,
                _rfqMainListQuery,
                _rfqItemListQuery,
                NullLogger<RFQService>.Instance,
                Substitute.For<ILogOperationAppendService>(),
                BizBrandTestSubstitute.Create(new Dictionary<long, string> { [1] = "Brand-A", [2] = "B2" }),
                Substitute.For<IRfqTagService>(),
                Substitute.For<IQuoteStatusSyncService>());
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
                        BrandId = 1,
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

        /// <summary>有报价员池时：主单已分配、每条明细独立轮询一对采购员、全局游标每条明细 +N。</summary>
        [Fact]
        public async Task CreateAsync_WithPurchaserPool_AssignsPairPerLineAndAdvancesCursor()
        {
            var rfqRepo = Substitute.For<IRepository<RFQ>>();
            var itemRepo = Substitute.For<IRepository<RFQItem>>();
            var serial = Substitute.For<ISerialNumberService>();
            serial.GenerateNextAsync(Arg.Any<string>()).Returns("RF20260001", "RF20260002");
            rfqRepo.GetAllAsync().Returns(new List<RFQ>());
            itemRepo.GetAllAsync().Returns(new List<RFQItem>());

            var sysParamRepo = new MemoryRepository<SysParam>();
            await sysParamRepo.AddAsync(new SysParam
            {
                Id = "sp-cursor",
                ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
                ParamName = "cursor",
                ValueString = "0",
                Status = 1
            });

            var purchaseQuoterPool = CreatePoolServiceWithUsers("U-A", "U-M", "U-Z");
            var orchestrator = RfqAssignmentTestFactory.CreateItemRoundRobinOrchestrator(
                purchaseQuoterPool,
                sysParamRepo);

            var rbacDeptRepo = Substitute.For<IRepository<RbacDepartment>>();
            var rbacUserDeptRepo = Substitute.For<IRepository<RbacUserDepartment>>();
            rbacDeptRepo.GetAllAsync().Returns(new List<RbacDepartment>());
            rbacUserDeptRepo.GetAllAsync().Returns(new List<RbacUserDepartment>());
            var rbacSvc = Substitute.For<IRbacService>();
            rbacSvc.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });
            var rfqMain = Substitute.For<IRfqMainListQuery>();
            rfqMain.GetPagedWithAggregatesAsync(Arg.Any<RFQQueryRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RfqMainListQueryPage
                {
                    Items = Array.Empty<RFQ>(),
                    TotalCount = 0,
                    PageIndex = 1,
                    PageSize = 20,
                    Aggregates = new RfqMainListAggregates()
                }));
            var rfqItem = Substitute.For<IRfqItemListQuery>();
            rfqItem.GetPagedAsync(Arg.Any<RFQItemQueryRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new PagedResult<RFQItemListItem>
                {
                    Items = Array.Empty<RFQItemListItem>(),
                    TotalCount = 0,
                    PageIndex = 1,
                    PageSize = 20
                }));
            var svc = new RFQService(
                rfqRepo,
                itemRepo,
                null!,
                Substitute.For<IEntityLookupService>(),
                Substitute.For<IUnitOfWork>(),
                serial,
                Substitute.For<IDataPermissionService>(),
                Substitute.For<IUserService>(),
                Substitute.For<IRepository<Quote>>(),
                Substitute.For<IRepository<RfqCloseRecord>>(),
                Substitute.For<IRepository<User>>(),
                rbacSvc,
                orchestrator,
                rfqMain,
                rfqItem,
                NullLogger<RFQService>.Instance,
                Substitute.For<ILogOperationAppendService>(),
                BizBrandTestSubstitute.Create(new Dictionary<long, string> { [1] = "Brand-A", [2] = "B2" }),
                Substitute.For<IRfqTagService>(),
                Substitute.For<IQuoteStatusSyncService>());

            var req = BuildValidCreateRequest(r =>
            {
                r.AssignMethod = RfqAssignMethodCodes.ItemRoundRobin;
                r.Items.Add(new CreateRFQItemRequest { Mpn = "MPN-002", BrandId = 2, Brand = "B2", Quantity = 1 });
            });

            var first = await svc.CreateAsync(req);
            Assert.Equal(1, first.Status);
            Assert.Equal(2, first.AssignMethod);
            await itemRepo.Received(1).AddAsync(Arg.Is<RFQItem>(i =>
                i.AssignedPurchaserUserId1 == "U-A" && i.AssignedPurchaserUserId2 == "U-M"));
            await itemRepo.Received(1).AddAsync(Arg.Is<RFQItem>(i =>
                i.AssignedPurchaserUserId1 == "U-Z" && i.AssignedPurchaserUserId2 == "U-A"));

            var cursorRow = (await sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor)).First();
            Assert.Equal("4", cursorRow.ValueString);

            await svc.CreateAsync(BuildValidCreateRequest());
            cursorRow = (await sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor)).First();
            Assert.Equal("6", cursorRow.ValueString);
            await itemRepo.Received(1).AddAsync(Arg.Is<RFQItem>(i =>
                i.AssignedPurchaserUserId1 == "U-M" && i.AssignedPurchaserUserId2 == "U-Z"));
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
            await _unitOfWork.Received().ExecuteAsync(Arg.Is<string>(s =>
                s.Contains("log_change_fldval") && s.Contains("remark") && s.Contains("更新备注")));
        }

        [Fact]
        public async Task CreateAsync_WithItems_ShouldWriteLineAddedChangeLog()
        {
            _rfqItemRepository.GetAllAsync().Returns(new List<RFQItem>());

            var request = BuildValidCreateRequest();
            await _rfqService.CreateAsync(request);

            await _unitOfWork.Received().ExecuteAsync(Arg.Is<string>(s =>
                s.Contains("log_change_fldval") && s.Contains("lineAdded")));
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
        public async Task UpdateStatusAsync_LegacyStatus6_NormalizesToClosed7()
        {
            var existingRFQ = new RFQ
            {
                Id = "RFQ-123",
                RfqCode = "RF20260001",
                Status = 0
            };
            _rfqRepository.GetByIdAsync("RFQ-123").Returns(existingRFQ);

            await _rfqService.UpdateStatusAsync("RFQ-123", (short)RfqMainStatus.LegacyObsoleteClosed);

            await _rfqRepository.Received(1).UpdateAsync(Arg.Is<RFQ>(r => r.Status == (short)RfqMainStatus.Closed));
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
            _rfqItemRepository.FindAsync(Arg.Any<Expression<Func<RFQItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<RFQItem>>(Array.Empty<RFQItem>()));

            // Act
            await _rfqService.DeleteAsync(rfqId, "user-1");

            // Assert
            await _rfqRepository.Received(1).DeleteAsync(rfqId);
        }

        [Fact]
        public async Task RestoreAsync_DoesNotReviveEarlierDeletedItems()
        {
            var rfqId = "RFQ-DEL";
            var headerTime = DateTime.UtcNow;
            var rfq = new RFQ
            {
                Id = rfqId,
                RfqCode = "RF20260099",
                IsDeleted = true,
                ModifyTime = headerTime,
                SalesUserId = "USER-001"
            };
            var keep = new RFQItem
            {
                Id = "ITEM-A",
                RfqId = rfqId,
                LineNo = 1,
                IsDeleted = true,
                ModifyTime = headerTime
            };
            var earlier = new RFQItem
            {
                Id = "ITEM-C",
                RfqId = rfqId,
                LineNo = 2,
                IsDeleted = true,
                ModifyTime = headerTime.AddDays(-3)
            };

            _rfqRepository.FindIgnoreFiltersAsync(Arg.Any<Expression<Func<RFQ, bool>>>())
                .Returns(Task.FromResult<IEnumerable<RFQ>>(new[] { rfq }));
            _rfqItemRepository.FindIgnoreFiltersAsync(Arg.Any<Expression<Func<RFQItem, bool>>>())
                .Returns(Task.FromResult<IEnumerable<RFQItem>>(new[] { keep, earlier }));
            _dataPermissionService.CanAccessRFQAsync(Arg.Any<string>(), Arg.Any<RFQ>()).Returns(true);
            _rfqMainListQuery.GetLatestRfqHeaderDeleteLogsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<string, RfqHeaderDeleteLogInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    [rfqId] = new RfqHeaderDeleteLogInfo
                    {
                        OperationTime = headerTime,
                        ExtraInfo = keep.Id
                    }
                });

            await _rfqService.RestoreAsync(rfqId, "user-1");

            Assert.False(rfq.IsDeleted);
            Assert.False(keep.IsDeleted);
            Assert.True(earlier.IsDeleted);
            await _rfqItemRepository.Received(1).UpdateAsync(Arg.Is<RFQItem>(i => i.Id == keep.Id && !i.IsDeleted));
            await _rfqItemRepository.DidNotReceive().UpdateAsync(Arg.Is<RFQItem>(i => i.Id == earlier.Id));
        }

        [Fact]
        public async Task GetDeletedPagedAsync_Throws_WhenNoRecycleAccess()
        {
            _rbacService.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(new UserPermissionSummaryDto
                {
                    UserId = "sales-1",
                    IdentityType = 1,
                    RoleCodes = new[] { "DEPT_EMPLOYEE" }
                });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _rfqService.GetDeletedPagedAsync(new RFQQueryRequest { CurrentUserId = "sales-1" }));
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

        [Fact]
        public async Task MarkNoQuoteAsync_PendingItemWithoutQuotes_SetsStatus5()
        {
            var itemId = "ITEM-001";
            var rfqId = "RFQ-123";
            var item = new RFQItem
            {
                Id = itemId,
                RfqId = rfqId,
                Status = (short)RfqItemStatus.Pending,
                IsDeleted = false
            };
            var rfq = new RFQ
            {
                Id = rfqId,
                RfqCode = "RF20260001",
                Status = (short)RfqMainStatus.Assigned
            };

            _rfqItemRepository.GetByIdAsync(itemId).Returns(item);
            _quoteRepo.FindAsync(Arg.Any<Expression<Func<Quote, bool>>>())
                .Returns(Task.FromResult<IEnumerable<Quote>>(Array.Empty<Quote>()));
            _rfqRepository.GetByIdAsync(rfqId).Returns(rfq);
            _dataPermissionService.CanAccessRFQAsync(Arg.Any<string>(), Arg.Any<RFQ>()).Returns(true);

            var result = await _rfqService.MarkNoQuoteAsync(itemId, "USER-001");

            Assert.Equal((short)RfqItemStatus.NoQuoteFound, result.Status);
            await _rfqItemRepository.Received(1).UpdateAsync(Arg.Is<RFQItem>(i =>
                i.Id == itemId && i.Status == (short)RfqItemStatus.NoQuoteFound));
        }
    }
}
