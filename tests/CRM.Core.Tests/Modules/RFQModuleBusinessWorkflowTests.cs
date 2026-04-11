using System.Collections.Concurrent;
using System.Linq;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Rbac;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.System;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Core.Tests.Modules;

/// <summary>
/// 需求(RFQ)模块：基于内存仓储的端到端业务读写场景测试（不连真实数据库）。
/// </summary>
public sealed class RFQModuleBusinessWorkflowTests
{
    private sealed class RfqInMemoryHarness
    {
        public MemoryRepository<RFQ> RfqRepo { get; } = new();
        public MemoryRepository<RFQItem> ItemRepo { get; } = new();
        public MemoryRepository<CustomerInfo> CustomerRepo { get; } = new();
        public MemoryRepository<CustomerContactInfo> ContactRepo { get; } = new();
        public MemoryRepository<VendorInfo> VendorRepo { get; } = new();
        public MemoryRepository<VendorContactInfo> VendorContactRepo { get; } = new();
        public MemoryRepository<SysParam> SysParamRepo { get; } = new();
        public MemoryRepository<RbacRole> RbacRoleRepo { get; } = new();
        public MemoryRepository<RbacUserRole> RbacUserRoleRepo { get; } = new();
        public MemoryRepository<RbacDepartment> RbacDepartmentRepo { get; } = new();
        public MemoryRepository<RbacUserDepartment> RbacUserDepartmentRepo { get; } = new();
        public MemoryRepository<User> UserRepo { get; } = new();
        public MemoryRepository<Quote> QuoteRepo { get; } = new();
        public EntityLookupService Lookup { get; }
        public IDataPermissionService DataPermission { get; }
        public ISerialNumberService Serial { get; }
        public IUserService UserService { get; }
        public IUnitOfWork UnitOfWork { get; }
        public RFQService Service { get; }

        public RfqInMemoryHarness()
        {
            DataPermission = Substitute.For<IDataPermissionService>();
            DataPermission
                .FilterRFQsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<RFQListItem>>())
                .Returns(ci => ci.ArgAt<IEnumerable<RFQListItem>>(1).ToList());
            DataPermission
                .GetRfqItemLineVisibilityPredicateAsync(Arg.Any<string>())
                .Returns(_ => Task.FromResult<Func<RFQ, RFQItem, bool>>((__, ___) => true));

            Serial = Substitute.For<ISerialNumberService>();
            var codeSeq = new ConcurrentInt();
            Serial.GenerateNextAsync(Arg.Any<string>())
                .Returns(_ => Task.FromResult($"RF-WF-{codeSeq.Next():D4}"));

            UserService = Substitute.For<IUserService>();
            var workflowUser = new User
            {
                Id = "USER-001",
                UserName = "saler1",
                RealName = "李业务"
            };
            UserService.GetAllAsync().Returns(new List<User> { workflowUser });
            UserService.GetByIdAsync("USER-001").Returns(workflowUser);
            UserRepo.AddAsync(workflowUser).GetAwaiter().GetResult();

            UnitOfWork = Substitute.For<IUnitOfWork>();
            Lookup = new EntityLookupService(CustomerRepo, ContactRepo, VendorRepo, VendorContactRepo, UserService);
            var rbac = Substitute.For<IRbacService>();
            rbac.GetUserPermissionSummaryAsync(Arg.Any<string>())
                .Returns(ci => new UserPermissionSummaryDto
                {
                    UserId = ci.ArgAt<string>(0),
                    IsSysAdmin = true,
                    RoleCodes = Array.Empty<string>(),
                    PermissionCodes = Array.Empty<string>()
                });
            Service = new RFQService(RfqRepo, ItemRepo, CustomerRepo, Lookup, UnitOfWork, Serial, DataPermission, UserService, SysParamRepo, RbacRoleRepo, RbacUserRoleRepo, RbacDepartmentRepo, RbacUserDepartmentRepo, QuoteRepo, UserRepo, rbac, NullLogger<RFQService>.Instance);
        }

        private sealed class ConcurrentInt
        {
            private int _v;
            public int Next() => Interlocked.Increment(ref _v);
        }
    }

    private static async Task SeedCustomerAsync(RfqInMemoryHarness h, string id = "CUST-WF-001", string name = "模拟客户公司")
    {
        await h.CustomerRepo.AddAsync(new CustomerInfo
        {
            Id = id,
            CustomerCode = "WF001",
            OfficialName = name
        });
    }

    private static CreateRFQRequest BuildCreateRequest(string? customerId = null) => new()
    {
        CustomerId = customerId ?? "CUST-WF-001",
        SalesUserId = "USER-001",
        Product = "工业MCU",
        Industry = "半导体",
        Items =
        {
            new CreateRFQItemRequest
            {
                LineNo = 1,
                Mpn = "STM32F103RCT6",
                Brand = "ST",
                CustomerBrand = "ST",
                Quantity = 100
            },
            new CreateRFQItemRequest
            {
                LineNo = 2,
                Mpn = "STM32F407VGT6",
                Brand = "ST",
                CustomerBrand = "ST",
                Quantity = 50,
                CustomerMpn = "CUST-PN-407"
            }
        }
    };

    [Fact]
    public async Task FullLifecycle_Create_List_Detail_ItemList_UpdateItems_Status_Delete()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);

        var created = await h.Service.CreateAsync(BuildCreateRequest());
        created.RfqCode.Should().StartWith("RF-WF-");
        created.ItemCount.Should().Be(2);

        h.RfqRepo.Snapshot().Should().HaveCount(1);
        h.ItemRepo.Snapshot().Should().HaveCount(2);
        await h.UnitOfWork.Received(1).SaveChangesAsync();

        var detail = await h.Service.GetByIdAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Items.Should().HaveCount(2);
        detail.Items.Select(i => i.LineNo).Should().BeInAscendingOrder();

        var mainList = await h.Service.GetPagedAsync(new RFQQueryRequest
        {
            Keyword = "MCU",
            PageIndex = 1,
            PageSize = 10
        });
        mainList.TotalCount.Should().Be(1);
        mainList.Items.First().RfqCode.Should().Be(created.RfqCode);

        var itemList = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            MaterialModel = "F103",
            PageIndex = 1,
            PageSize = 20,
            CurrentUserId = null
        });
        itemList.TotalCount.Should().Be(1);
        itemList.Items.First().Mpn.Should().Contain("F103");
        itemList.Items.First().CustomerName.Should().Be("模拟客户公司");
        itemList.Items.First().SalesUserName.Should().Be("李业务");

        await h.Service.UpdateAsync(created.Id, new UpdateRFQRequest
        {
            Remark = "流程测试备注",
            Items = new List<CreateRFQItemRequest>
            {
                new()
                {
                    Mpn = "NEW-ONLY-ONE",
                    Brand = "NX",
                    CustomerBrand = "NX",
                    Quantity = 1
                }
            }
        });

        h.ItemRepo.Snapshot().Should().HaveCount(1);
        h.ItemRepo.Snapshot()[0].Mpn.Should().Be("NEW-ONLY-ONE");
        (await h.RfqRepo.GetByIdAsync(created.Id))!.ItemCount.Should().Be(1);

        await h.Service.UpdateStatusAsync(created.Id, 3);
        (await h.RfqRepo.GetByIdAsync(created.Id))!.Status.Should().Be(3);

        await h.Service.DeleteAsync(created.Id);
        h.RfqRepo.Snapshot().Should().BeEmpty();
        h.ItemRepo.Snapshot().Should().BeEmpty();
        await h.UnitOfWork.Received(4).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_WithoutCustomerId_Throws()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);
        var req = BuildCreateRequest();
        req.CustomerId = "  ";

        var act = async () => await h.Service.CreateAsync(req);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*客户*");
    }

    [Fact]
    public async Task CreateAsync_WithoutItems_Throws()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);
        var req = BuildCreateRequest();
        req.Items.Clear();

        var act = async () => await h.Service.CreateAsync(req);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*明细*");
    }

    [Fact]
    public async Task CreateAsync_ItemWithoutMpn_Throws()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);
        var req = BuildCreateRequest();
        req.Items[0].Mpn = " ";

        var act = async () => await h.Service.CreateAsync(req);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*MPN*");
    }

    [Fact]
    public async Task CreateAsync_ItemWithoutBrand_Throws()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);
        var req = BuildCreateRequest();
        req.Items[0].Brand = " ";

        var act = async () => await h.Service.CreateAsync(req);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*品牌*");
    }

    [Fact]
    public async Task GetPagedItemsAsync_FiltersByCreateDateRange()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);

        var rfqId = Guid.NewGuid().ToString();
        await h.RfqRepo.AddAsync(new RFQ
        {
            Id = rfqId,
            RfqCode = "RF-DATE-1",
            CustomerId = "CUST-WF-001",
            SalesUserId = "USER-001",
            CreateTime = new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc),
            ItemCount = 1
        });
        await h.ItemRepo.AddAsync(new RFQItem
        {
            Id = Guid.NewGuid().ToString(),
            RfqId = rfqId,
            LineNo = 1,
            Mpn = "DATE-MPN",
            Brand = "B",
            CustomerBrand = "B",
            Quantity = 1
        });

        var inRange = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 2, 28),
            PageIndex = 1,
            PageSize = 20
        });
        inRange.TotalCount.Should().Be(1);

        var outRange = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 31),
            PageIndex = 1,
            PageSize = 20
        });
        outRange.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPagedItemsAsync_FiltersByCustomerKeywordAndSalesKeyword()
    {
        var h = new RfqInMemoryHarness();
        await h.CustomerRepo.AddAsync(new CustomerInfo
        {
            Id = "C-A",
            CustomerCode = "CA",
            OfficialName = "深圳阿尔法电子"
        });
        await h.Service.CreateAsync(new CreateRFQRequest
        {
            CustomerId = "C-A",
            SalesUserId = "USER-001",
            Items =
            {
                new CreateRFQItemRequest { Mpn = "X1", Brand = "B", CustomerBrand = "B", Quantity = 1 }
            }
        });

        var byCustomer = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            CustomerKeyword = "阿尔法",
            PageIndex = 1,
            PageSize = 10
        });
        byCustomer.TotalCount.Should().Be(1);

        var bySales = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            SalesUserKeyword = "李业务",
            PageIndex = 1,
            PageSize = 10
        });
        bySales.TotalCount.Should().Be(1);

        var byUserName = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            SalesUserKeyword = "saler1",
            PageIndex = 1,
            PageSize = 10
        });
        byUserName.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedItemsAsync_HasQuotesOnly_ReturnsOnlyLinesWithQuote()
    {
        var h = new RfqInMemoryHarness();
        await SeedCustomerAsync(h);
        await h.Service.CreateAsync(BuildCreateRequest());
        var items = h.ItemRepo.Snapshot().OrderBy(i => i.LineNo).ToList();
        items.Should().HaveCount(2);
        var quotedItemId = items[0].Id;
        await h.QuoteRepo.AddAsync(new Quote
        {
            Id = Guid.NewGuid().ToString(),
            QuoteCode = "QT-WF-FILTER",
            RFQItemId = quotedItemId
        });

        var withFilter = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            HasQuotesOnly = true,
            PageIndex = 1,
            PageSize = 20
        });
        withFilter.TotalCount.Should().Be(1);
        withFilter.Items.Should().HaveCount(1);
        withFilter.Items.First().Id.Should().Be(quotedItemId);

        var noFilter = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            PageIndex = 1,
            PageSize = 20
        });
        noFilter.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedItemsAsync_WhenDataPermissionReturnsEmpty_YieldsNoRows()
    {
        var h = new RfqInMemoryHarness();
        h.DataPermission
            .FilterRFQsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<RFQListItem>>())
            .Returns(new List<RFQListItem>());
        h.DataPermission
            .GetRfqItemLineVisibilityPredicateAsync(Arg.Any<string>())
            .Returns(_ => Task.FromResult<Func<RFQ, RFQItem, bool>>((__, ___) => false));

        await SeedCustomerAsync(h);
        await h.Service.CreateAsync(BuildCreateRequest());

        var page = await h.Service.GetPagedItemsAsync(new RFQItemQueryRequest
        {
            PageIndex = 1,
            PageSize = 20,
            CurrentUserId = "USER-999"
        });
        page.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WhenRfqMissing_Throws()
    {
        var h = new RfqInMemoryHarness();
        var act = async () => await h.Service.UpdateAsync("missing-id", new UpdateRFQRequest { Remark = "x" });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenRfqMissing_Throws()
    {
        var h = new RfqInMemoryHarness();
        var act = async () => await h.Service.DeleteAsync("missing-id");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
