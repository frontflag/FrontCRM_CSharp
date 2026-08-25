using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using FluentAssertions;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public sealed class CustomerWorkspaceServiceTests
{
    private readonly IRepository<RFQ> _rfqRepo = Substitute.For<IRepository<RFQ>>();
    private readonly IRepository<RFQItem> _itemRepo = Substitute.For<IRepository<RFQItem>>();
    private readonly IRepository<SellOrder> _soRepo = Substitute.For<IRepository<SellOrder>>();
    private readonly IRepository<SellOrderItem> _soItemRepo = Substitute.For<IRepository<SellOrderItem>>();
    private readonly IRepository<StockOutRequest> _notifyRepo = Substitute.For<IRepository<StockOutRequest>>();
    private readonly IRepository<Packing> _packingRepo = Substitute.For<IRepository<Packing>>();
    private readonly IRepository<PackingItem> _packingItemRepo = Substitute.For<IRepository<PackingItem>>();
    private readonly IRepository<StockOut> _stockOutRepo = Substitute.For<IRepository<StockOut>>();
    private readonly IRepository<StockOutItem> _stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
    private readonly IRepository<FinanceReceipt> _financeReceiptRepo = Substitute.For<IRepository<FinanceReceipt>>();
    private readonly IRepository<FinanceSellInvoice> _financeSellInvoiceRepo = Substitute.For<IRepository<FinanceSellInvoice>>();
    private readonly IRepository<FinanceReceivable> _financeReceivableRepo = Substitute.For<IRepository<FinanceReceivable>>();
    private readonly IRepository<CustomerInfo> _customerRepo = Substitute.For<IRepository<CustomerInfo>>();
    private readonly IDataPermissionService _dataPermission = Substitute.For<IDataPermissionService>();
    private readonly IRbacService _rbac = Substitute.For<IRbacService>();
    private readonly IEntityLookupService _lookup = Substitute.For<IEntityLookupService>();

    private CustomerWorkspaceService CreateSut() =>
        new(
            _rfqRepo,
            _itemRepo,
            _soRepo,
            _soItemRepo,
            _notifyRepo,
            _packingRepo,
            _packingItemRepo,
            _stockOutRepo,
            _stockOutItemRepo,
            _financeReceiptRepo,
            _financeSellInvoiceRepo,
            _financeReceivableRepo,
            _customerRepo,
            _dataPermission,
            _rbac,
            _lookup);

    private void SetupViewer(
        string userId,
        bool rfqRead,
        bool customerRead,
        bool mask521 = false,
        bool salesOrderRead = false,
        bool financeReceiptRead = false,
        bool financeSellInvoiceRead = false)
    {
        _rbac.GetUserPermissionSummaryAsync(userId).Returns(new UserPermissionSummaryDto
        {
            UserId = userId,
            IsSysAdmin = false,
            HasBizDataBypass = false,
            SaleDataScope = mask521 ? (short)4 : (short)1,
            IdentityType = mask521 ? (short)2 : (short)1,
            PermissionCodes = new[]
            {
                rfqRead ? "rfq.read" : "other.read",
                salesOrderRead ? "sales-order.read" : "other.read",
                financeReceiptRead ? "finance-receipt.read" : "other.read",
                financeSellInvoiceRead ? "finance-sell-invoice.read" : "other.read",
                customerRead ? "customer.read" : "other.read"
            }
        });
    }

    [Fact]
    public async Task GetAsync_Throws_WhenSourceUnknown()
    {
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("nope", "x", "u1");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*不支持的来源*");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenRfqMissing()
    {
        SetupViewer("u1", rfqRead: true, customerRead: true);
        _rfqRepo.GetByIdAsync("r1").Returns((RFQ?)null);
        var sut = CreateSut();
        var dto = await sut.GetAsync("rfq", "r1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCannotAccessRfq()
    {
        SetupViewer("u1", rfqRead: true, customerRead: true);
        var rfq = new RFQ { Id = "r1", CustomerId = "c1" };
        _rfqRepo.GetByIdAsync("r1").Returns(rfq);
        _dataPermission.CanAccessRFQAsync("u1", rfq).Returns(false);
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("rfq", "r1", "u1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAsync_Rfq_ReturnsRestricted_WithoutCustomerId()
    {
        SetupViewer("u1", rfqRead: true, customerRead: false);
        var rfq = new RFQ { Id = "r1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _rfqRepo.GetByIdAsync("r1").Returns(rfq);
        _dataPermission.CanAccessRFQAsync("u1", rfq).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("rfq", "r1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeFalse();
        dto.CustomerId.Should().BeNull();
        dto.CustomerCode.Should().Be("C001");
        dto.SalesUserName.Should().Be("sales.a");
        dto.ChineseName.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_RfqItem_ReturnsFull_WhenAllowed()
    {
        SetupViewer("u1", rfqRead: true, customerRead: true);
        var item = new RFQItem { Id = "i1", RfqId = "r1" };
        var rfq = new RFQ { Id = "r1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            EnglishOfficialName = "Test Co",
            SalesUserId = "s1",
            Type = 1,
            CreditLine = 1000m
        };
        _itemRepo.GetByIdAsync("i1").Returns(item);
        _rfqRepo.GetByIdAsync("r1").Returns(rfq);
        _dataPermission.CanAccessRFQAsync("u1", rfq).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("rfqItem", "i1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
        dto.EnglishName.Should().Be("Test Co");
        dto.CreditLimit.Should().Be(1000m);
    }

    [Fact]
    public async Task GetAsync_NoCustomerOnRfq_ReturnsEmpty()
    {
        SetupViewer("u1", rfqRead: true, customerRead: true);
        var rfq = new RFQ { Id = "r1", CustomerId = null };
        _rfqRepo.GetByIdAsync("r1").Returns(rfq);
        _dataPermission.CanAccessRFQAsync("u1", rfq).Returns(true);

        var sut = CreateSut();
        var dto = await sut.GetAsync("RFQ", "r1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenSellOrderMissing()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        _soRepo.GetByIdAsync("so1").Returns((SellOrder?)null);
        var sut = CreateSut();
        var dto = await sut.GetAsync("sellOrder", "so1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCannotAccessSellOrder()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(false);
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("sellOrder", "so1", "u1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAsync_SellOrder_ReturnsRestricted_WithoutCustomerId()
    {
        SetupViewer("u1", rfqRead: false, customerRead: false, salesOrderRead: true);
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("sell-order", "so1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeFalse();
        dto.CustomerId.Should().BeNull();
        dto.CustomerCode.Should().Be("C001");
        dto.SalesUserName.Should().Be("sales.a");
        dto.ChineseName.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_SellOrderItem_ReturnsFull_WhenAllowed()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var item = new SellOrderItem { Id = "i1", SellOrderId = "so1" };
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            EnglishOfficialName = "Test Co",
            SalesUserId = "s1",
            Type = 1,
            CreditLine = 1000m
        };
        _soItemRepo.GetByIdAsync("i1").Returns(item);
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("sellOrderItem", "i1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
        dto.EnglishName.Should().Be("Test Co");
        dto.CreditLimit.Should().Be(1000m);
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCannotAccessStockOutRequest()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var req = new StockOutRequest { Id = "n1", SalesOrderId = "so1", CustomerId = "c1" };
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        _notifyRepo.GetByIdAsync("n1").Returns(req);
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.IsLogisticsModuleUnrestrictedAsync("u1").Returns(false);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(false);
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("stockOutRequest", "n1", "u1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAsync_StockOutRequest_ReturnsRestricted_WithoutCustomerId()
    {
        SetupViewer("u1", rfqRead: false, customerRead: false, salesOrderRead: true);
        var req = new StockOutRequest { Id = "n1", SalesOrderId = "so1", CustomerId = "c1" };
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _notifyRepo.GetByIdAsync("n1").Returns(req);
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.IsLogisticsModuleUnrestrictedAsync("u1").Returns(false);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("stock-out-notify", "n1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeFalse();
        dto.CustomerId.Should().BeNull();
        dto.CustomerCode.Should().Be("C001");
        dto.SalesUserName.Should().Be("sales.a");
    }

    [Fact]
    public async Task GetAsync_Packing_ReturnsFull_WhenSalesSelf()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var packing = new Packing { Id = "p1", CustomerId = "c1", SalesId = "u1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            EnglishOfficialName = "Test Co",
            SalesUserId = "s1",
            CreditLine = 500m
        };
        _packingRepo.GetByIdAsync("p1").Returns(packing);
        _dataPermission.IsLogisticsModuleUnrestrictedAsync("u1").Returns(false);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("packing", "p1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
        dto.CreditLimit.Should().Be(500m);
    }

    [Fact]
    public async Task GetAsync_StockOutItem_ReturnsFull_WhenAllowed()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var item = new StockOutItem { Id = "i1", StockOutId = "o1" };
        var stockOut = new StockOut { Id = "o1", CustomerId = "c1", SellOrderItemId = "soi1" };
        var soItem = new SellOrderItem { Id = "soi1", SellOrderId = "so1" };
        var so = new SellOrder { Id = "so1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _stockOutItemRepo.GetByIdAsync("i1").Returns(item);
        _stockOutRepo.GetByIdAsync("o1").Returns(stockOut);
        _soItemRepo.GetByIdAsync("soi1").Returns(soItem);
        _soRepo.GetByIdAsync("so1").Returns(so);
        _dataPermission.IsLogisticsModuleUnrestrictedAsync("u1").Returns(false);
        _dataPermission.CanAccessSalesOrderAsync("u1", so).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("stockOutItem", "i1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenFinanceReceiptMissing()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        _financeReceiptRepo.GetByIdAsync("fr1").Returns((FinanceReceipt?)null);
        var sut = CreateSut();
        var dto = await sut.GetAsync("financeReceipt", "fr1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenFinanceReceiptDeleted()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        _financeReceiptRepo.GetByIdAsync("fr1").Returns(new FinanceReceipt { Id = "fr1", CustomerId = "c1", IsDeleted = true });
        var sut = CreateSut();
        var dto = await sut.GetAsync("financeReceipt", "fr1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCannotAccessFinanceReceipt()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        var receipt = new FinanceReceipt { Id = "fr1", CustomerId = "c1" };
        _financeReceiptRepo.GetByIdAsync("fr1").Returns(receipt);
        _dataPermission.CanAccessFinanceReceiptAsync("u1", receipt).Returns(false);
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("finance-receipt", "fr1", "u1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAsync_FinanceReceipt_ReturnsRestricted_WithoutCustomerId()
    {
        SetupViewer("u1", rfqRead: false, customerRead: false, financeReceiptRead: true);
        var receipt = new FinanceReceipt { Id = "fr1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _financeReceiptRepo.GetByIdAsync("fr1").Returns(receipt);
        _dataPermission.CanAccessFinanceReceiptAsync("u1", receipt).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("receipt", "fr1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeFalse();
        dto.CustomerId.Should().BeNull();
        dto.CustomerCode.Should().Be("C001");
        dto.SalesUserName.Should().Be("sales.a");
        dto.ChineseName.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_FinanceReceipt_ReturnsFull_WhenAllowed()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        var receipt = new FinanceReceipt { Id = "fr1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            EnglishOfficialName = "Test Co",
            SalesUserId = "s1",
            Type = 1,
            CreditLine = 1000m
        };
        _financeReceiptRepo.GetByIdAsync("fr1").Returns(receipt);
        _dataPermission.CanAccessFinanceReceiptAsync("u1", receipt).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("financeReceipt", "fr1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenPackingItemMissing()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        _packingItemRepo.GetByIdAsync("pi1").Returns((PackingItem?)null);
        var sut = CreateSut();
        var dto = await sut.GetAsync("packingItem", "pi1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_PackingItem_ReturnsFull_FromPackingHeader()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, salesOrderRead: true);
        var item = new PackingItem { Id = "pi1", PackingId = "p1" };
        var packing = new Packing { Id = "p1", CustomerId = "c1", SalesId = "u1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _packingItemRepo.GetByIdAsync("pi1").Returns(item);
        _packingRepo.GetByIdAsync("p1").Returns(packing);
        _dataPermission.IsLogisticsModuleUnrestrictedAsync("u1").Returns(false);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("packing-item", "pi1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
    }

    [Fact]
    public async Task GetAsync_Throws_WhenCannotAccessSellInvoice()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeSellInvoiceRead: true);
        var invoice = new FinanceSellInvoice { Id = "si1", CustomerId = "c1" };
        _financeSellInvoiceRepo.GetByIdAsync("si1").Returns(invoice);
        _dataPermission.CanAccessFinanceSellInvoiceAsync("u1", invoice).Returns(false);
        var sut = CreateSut();
        var act = async () => await sut.GetAsync("financeSellInvoice", "si1", "u1");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAsync_SellInvoice_ReturnsRestricted_WithoutCustomerId()
    {
        SetupViewer("u1", rfqRead: false, customerRead: false, financeSellInvoiceRead: true);
        var invoice = new FinanceSellInvoice { Id = "si1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _financeSellInvoiceRepo.GetByIdAsync("si1").Returns(invoice);
        _dataPermission.CanAccessFinanceSellInvoiceAsync("u1", invoice).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("sell-invoice", "si1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeFalse();
        dto.CustomerId.Should().BeNull();
        dto.CustomerCode.Should().Be("C001");
        dto.SalesUserName.Should().Be("sales.a");
        dto.ChineseName.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenReceivableMissing()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        _financeReceivableRepo.GetByIdAsync("ar1").Returns((FinanceReceivable?)null);
        var sut = CreateSut();
        var dto = await sut.GetAsync("financeReceivable", "ar1", "u1");
        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Receivable_ReturnsFull_WhenAllowed()
    {
        SetupViewer("u1", rfqRead: false, customerRead: true, financeReceiptRead: true);
        var receivable = new FinanceReceivable { Id = "ar1", CustomerId = "c1" };
        var customer = new CustomerInfo
        {
            Id = "c1",
            CustomerCode = "C001",
            OfficialName = "测试客户",
            SalesUserId = "s1"
        };
        _financeReceivableRepo.GetByIdAsync("ar1").Returns(receivable);
        _dataPermission.CanAccessFinanceReceivableAsync("u1", receivable).Returns(true);
        _customerRepo.GetByIdAsync("c1").Returns(customer);
        _dataPermission.CanAccessCustomerAsync("u1", customer).Returns(true);
        _lookup.GetUserLoginNameAsync("s1").Returns("sales.a");

        var sut = CreateSut();
        var dto = await sut.GetAsync("receivable", "ar1", "u1");

        dto.Should().NotBeNull();
        dto!.HasCustomer.Should().BeTrue();
        dto.CanViewFull.Should().BeTrue();
        dto.CustomerId.Should().Be("c1");
        dto.ChineseName.Should().Be("测试客户");
    }
}
