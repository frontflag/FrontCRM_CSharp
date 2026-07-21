using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderVendorChangeServiceTests
{
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<VendorInfo> _vendorRepo;
    private readonly IRepository<VendorContactInfo> _vendorContactRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<FinancePayment> _paymentRepo;
    private readonly IRepository<FinancePaymentItem> _paymentItemRepo;
    private readonly IRepository<FinancePurchaseInvoice> _purchaseInvoiceRepo;
    private readonly IRepository<FinancePurchaseInvoiceItem> _purchaseInvoiceItemRepo;
    private readonly PurchaseOrderVendorChangeService _service;

    public PurchaseOrderVendorChangeServiceTests()
    {
        _poRepo = Substitute.For<IRepository<PurchaseOrder>>();
        _poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
        _vendorRepo = Substitute.For<IRepository<VendorInfo>>();
        _vendorContactRepo = Substitute.For<IRepository<VendorContactInfo>>();
        _notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        _stockInRepo = Substitute.For<IRepository<StockIn>>();
        _paymentRepo = Substitute.For<IRepository<FinancePayment>>();
        _paymentItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();
        _purchaseInvoiceRepo = Substitute.For<IRepository<FinancePurchaseInvoice>>();
        _purchaseInvoiceItemRepo = Substitute.For<IRepository<FinancePurchaseInvoiceItem>>();
        var purchaseParams = Substitute.For<IPurchaseQuoterPoolService>();
        purchaseParams.GetAllowRefreshCompletedBizNodesAsync(Arg.Any<CancellationToken>()).Returns(false);
        _service = new PurchaseOrderVendorChangeService(
            _poRepo,
            _poItemRepo,
            _vendorRepo,
            _vendorContactRepo,
            _notifyRepo,
            _stockInRepo,
            _paymentRepo,
            _paymentItemRepo,
            _purchaseInvoiceRepo,
            _purchaseInvoiceItemRepo,
            purchaseParams,
            NullLogger<PurchaseOrderVendorChangeService>.Instance);
    }

    [Fact]
    public void CanChangeVendor_SysAdmin_ReturnsTrue()
    {
        var ok = PurchaseOrderVendorChangeAccessRules.CanChangeVendor(new UserPermissionSummaryDto
        {
            IsSysAdmin = true
        });
        Assert.True(ok);
    }

    [Fact]
    public void CanChangeVendor_PurchaseDirector_ReturnsTrue()
    {
        var ok = PurchaseOrderVendorChangeAccessRules.CanChangeVendor(new UserPermissionSummaryDto
        {
            IdentityType = 2,
            RoleCodes = new List<string> { "DEPT_DIRECTOR" }
        });
        Assert.True(ok);
    }

    [Fact]
    public async Task PreviewAsync_SameVendor_ReturnsNoOp()
    {
        const string poId = "po-1";
        const string vendorId = "v-old";
        _poRepo.GetByIdAsync(poId).Returns(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00001",
            VendorId = vendorId,
            VendorName = "正确供应商"
        });
        _vendorRepo.GetByIdAsync(vendorId).Returns(new VendorInfo
        {
            Id = vendorId,
            OfficialName = "正确供应商"
        });
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>());
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());

        var preview = await _service.PreviewAsync(poId, vendorId);

        Assert.True(preview.NoOp);
        Assert.True(preview.CanChange);
        Assert.True(preview.SameVendorId);
        Assert.Equal(0, preview.PoVendorNameToSync);
    }

    [Fact]
    public async Task PreviewAsync_SameVendor_StaleName_ShouldNeedRefresh()
    {
        const string poId = "po-1";
        const string vendorId = "v-ok";
        _poRepo.GetByIdAsync(poId).Returns(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00009",
            VendorId = vendorId,
            VendorName = "过期快照名"
        });
        _vendorRepo.GetByIdAsync(vendorId).Returns(new VendorInfo
        {
            Id = vendorId,
            OfficialName = "主数据正式名"
        });
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>());
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());

        var preview = await _service.PreviewAsync(poId, vendorId);

        Assert.True(preview.CanChange);
        Assert.False(preview.NoOp);
        Assert.True(preview.SameVendorId);
        Assert.Equal(1, preview.PoVendorNameToSync);
        Assert.Equal("主数据正式名", preview.NewVendorName);
    }

    [Fact]
    public async Task ApplyAsync_SameVendor_StaleName_ShouldRefreshHeaderName()
    {
        const string poId = "po-1";
        const string vendorId = "v-ok";
        var order = new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00010",
            VendorId = vendorId,
            VendorName = "过期快照名"
        };
        _poRepo.GetByIdAsync(poId).Returns(order);
        _vendorRepo.GetByIdAsync(vendorId).Returns(new VendorInfo
        {
            Id = vendorId,
            OfficialName = "主数据正式名",
            Code = "VOK"
        });
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>());
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());

        var result = await _service.ApplyAsync(order, vendorId);

        Assert.True(result.Applied);
        Assert.Equal(vendorId, order.VendorId);
        Assert.Equal("主数据正式名", order.VendorName);
        Assert.Equal("VOK", order.VendorCode);
    }

    [Fact]
    public async Task PreviewAsync_PostedStockIn_SameVendor_ShouldNotBlockNameRefresh()
    {
        const string poId = "po-1";
        const string vendorId = "v-ok";
        const string notifyId = "n-1";
        _poRepo.GetByIdAsync(poId).Returns(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00011",
            VendorId = vendorId,
            VendorName = "过期快照名"
        });
        _vendorRepo.GetByIdAsync(vendorId).Returns(new VendorInfo
        {
            Id = vendorId,
            OfficialName = "主数据正式名"
        });
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>
            {
                new()
                {
                    Id = notifyId,
                    NoticeCode = "STIR00001",
                    PurchaseOrderId = poId,
                    VendorId = vendorId,
                    VendorName = "过期快照名",
                    Status = 100,
                    IsDeleted = false
                }
            });
        _stockInRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockIn, bool>>>())
            .Returns(new List<StockIn>
            {
                new()
                {
                    Id = "si-1",
                    StockInCode = "STI00001",
                    SourceId = notifyId,
                    VendorId = vendorId,
                    Status = StockInHeaderStatusCode.Posted,
                    IsDeleted = false
                }
            });
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());

        var preview = await _service.PreviewAsync(poId, vendorId);

        Assert.True(preview.CanChange);
        Assert.False(preview.NoOp);
        Assert.Equal(1, preview.PoVendorNameToSync);
        Assert.Empty(preview.BlockingDocuments);
    }

    [Fact]
    public async Task PreviewAsync_PostedStockIn_BlocksChange()
    {
        const string poId = "po-1";
        const string notifyId = "n-1";
        _poRepo.GetByIdAsync(poId).Returns(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00002",
            VendorId = "v-old",
            VendorName = "旧供应商"
        });
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem>());
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>
            {
                new() { Id = notifyId, NoticeCode = "STIR00001", PurchaseOrderId = poId, Status = 20, IsDeleted = false }
            });
        _stockInRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockIn, bool>>>())
            .Returns(new List<StockIn>
            {
                new() { Id = "si-1", StockInCode = "STI00001", SourceId = notifyId, Status = StockInHeaderStatusCode.Posted, IsDeleted = false }
            });
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());
        _vendorRepo.GetByIdAsync("v-new").Returns(new VendorInfo
        {
            Id = "v-new",
            OfficialName = "新供应商 Ltd"
        });

        var preview = await _service.PreviewAsync(poId, "v-new");

        Assert.False(preview.CanChange);
        Assert.Contains(preview.BlockingDocuments, d => d.Contains("STI00001"));
    }

    [Fact]
    public async Task ApplyAsync_ValidChange_UpdatesHeaderAndItems()
    {
        const string poId = "po-1";
        var order = new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00003",
            VendorId = "v-old",
            VendorName = "旧供应商",
            VendorContactId = "contact-old"
        };
        var line = new PurchaseOrderItem { Id = "line-1", PurchaseOrderId = poId, VendorId = "v-old", IsDeleted = false };

        _poRepo.GetByIdAsync(poId).Returns(order);
        _poItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<PurchaseOrderItem, bool>>>())
            .Returns(new List<PurchaseOrderItem> { line });
        _notifyRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StockInNotify, bool>>>())
            .Returns(new List<StockInNotify>());
        _paymentItemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<FinancePaymentItem, bool>>>())
            .Returns(new List<FinancePaymentItem>());
        _vendorRepo.GetByIdAsync("v-new").Returns(new VendorInfo
        {
            Id = "v-new",
            OfficialName = "新供应商 Ltd",
            Code = "VNEW"
        });
        _vendorContactRepo.GetByIdAsync("contact-old").Returns(new VendorContactInfo
        {
            Id = "contact-old",
            VendorId = "v-old"
        });

        var result = await _service.ApplyAsync(order, "v-new");

        Assert.True(result.Applied);
        Assert.Equal("v-new", order.VendorId);
        Assert.Equal("新供应商 Ltd", order.VendorName);
        Assert.Null(order.VendorContactId);
        Assert.Equal("v-new", line.VendorId);
        await _poItemRepo.Received(1).UpdateAsync(line);
    }
}
