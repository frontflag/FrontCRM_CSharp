using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderRefreshCompletedGateServiceTests
{
    private readonly IPurchaseQuoterPoolService _params = Substitute.For<IPurchaseQuoterPoolService>();
    private readonly MemoryRepository<PurchaseOrderItem> _poItems = new();
    private readonly MemoryRepository<StockInNotify> _notices = new();
    private readonly MemoryRepository<StockIn> _stockIns = new();
    private readonly MemoryRepository<StockInItemExtend> _stockInExtends = new();
    private readonly MemoryRepository<StockItem> _stockItems = new();
    private readonly MemoryRepository<PackingItem> _packingItems = new();
    private readonly MemoryRepository<Packing> _packings = new();
    private readonly MemoryRepository<CustomsDeclarationItem> _customsItems = new();
    private readonly MemoryRepository<CustomsDeclaration> _customs = new();
    private readonly MemoryRepository<StockOutItemExtend> _stockOutExtends = new();
    private readonly PurchaseOrderRefreshCompletedGateService _service;

    public PurchaseOrderRefreshCompletedGateServiceTests()
    {
        _params.GetRefreshCompletedFacetsAsync(Arg.Any<CancellationToken>())
            .Returns(new PurchaseRefreshCompletedFacets());
        _service = new PurchaseOrderRefreshCompletedGateService(
            _params,
            _poItems,
            _notices,
            _stockIns,
            _stockInExtends,
            _stockItems,
            _packingItems,
            _packings,
            _customsItems,
            _customs,
            _stockOutExtends);
    }

    [Fact]
    public async Task PreviewAsync_Status_AlwaysProceeds()
    {
        var preview = await _service.PreviewAsync("po-1", PurchaseOrderRefreshFacet.Status);
        Assert.True(preview.CanProceed);
        Assert.False(preview.HasCompleted);
    }

    [Fact]
    public async Task PreviewAsync_Pn_StockItem_ParamOff_Blocks()
    {
        _params.GetRefreshCompletedFacetsAsync(Arg.Any<CancellationToken>())
            .Returns(new PurchaseRefreshCompletedFacets { Pn = false });
        const string poId = "po-1";
        const string lineId = "line-1";
        await _poItems.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId });
        await _stockItems.AddAsync(new StockItem
        {
            Id = "si-1",
            PurchaseOrderItemId = lineId,
            StockInItemId = "sii-1",
            StockInId = "sin-1",
            StockAggregateId = "agg-1"
        });

        var preview = await _service.PreviewAsync(poId, PurchaseOrderRefreshFacet.Pn);

        Assert.False(preview.CanProceed);
        Assert.True(preview.HasCompleted);
        Assert.False(preview.AllowCompletedParam);
        Assert.Contains(preview.CompletedDocuments, d => d.Contains("库存明细"));
    }

    [Fact]
    public async Task EnsureAllowedAsync_Pn_ParamOn_WithoutConfirm_Throws()
    {
        const string poId = "po-1";
        const string lineId = "line-1";
        await _poItems.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId });
        await _stockItems.AddAsync(new StockItem
        {
            Id = "si-1",
            PurchaseOrderItemId = lineId,
            StockInItemId = "sii-1",
            StockInId = "sin-1",
            StockAggregateId = "agg-1"
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.EnsureAllowedAsync(poId, PurchaseOrderRefreshFacet.Pn, confirmCompleted: false));
        Assert.Contains("须确认后再刷新", ex.Message);
    }

    [Fact]
    public async Task EnsureAllowedAsync_Pn_ParamOn_WithConfirm_Succeeds()
    {
        const string poId = "po-1";
        const string lineId = "line-1";
        await _poItems.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId });
        await _stockItems.AddAsync(new StockItem
        {
            Id = "si-1",
            PurchaseOrderItemId = lineId,
            StockInItemId = "sii-1",
            StockInId = "sin-1",
            StockAggregateId = "agg-1"
        });

        await _service.EnsureAllowedAsync(poId, PurchaseOrderRefreshFacet.Pn, confirmCompleted: true);
    }

    [Fact]
    public async Task PreviewAsync_Qty_StockedInNotice_HasCompleted()
    {
        const string poId = "po-1";
        const string lineId = "line-1";
        await _poItems.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId });
        await _notices.AddAsync(new StockInNotify
        {
            Id = "n-1",
            NoticeCode = "AN001",
            PurchaseOrderItemId = lineId,
            Status = 100
        });

        var preview = await _service.PreviewAsync(poId, PurchaseOrderRefreshFacet.Qty);

        Assert.True(preview.CanProceed);
        Assert.True(preview.AllowCompletedParam);
        Assert.Contains(preview.CompletedDocuments, d => d.Contains("AN001"));
    }

    [Fact]
    public void PurchaseRefreshCompletedFacets_Defaults_VendorOff_OthersOn()
    {
        var facets = new PurchaseRefreshCompletedFacets();
        Assert.False(facets.Vendor);
        Assert.True(facets.Pn);
        Assert.True(facets.Brand);
        Assert.True(facets.Qty);
        Assert.True(facets.Price);
        Assert.False(facets.Allows(PurchaseOrderRefreshFacet.Vendor));
        Assert.True(facets.Allows(PurchaseOrderRefreshFacet.Pn));
        Assert.True(facets.Allows(PurchaseOrderRefreshFacet.Status));
    }
}
