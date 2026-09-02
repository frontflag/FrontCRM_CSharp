using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SalesOrderRefreshCompletedGateServiceTests
{
    private readonly ISalesParamsService _params = Substitute.For<ISalesParamsService>();
    private readonly MemoryRepository<SellOrderItem> _soItems = new();
    private readonly MemoryRepository<StockOutRequest> _notices = new();
    private readonly MemoryRepository<PackingItem> _packingItems = new();
    private readonly MemoryRepository<Packing> _packings = new();
    private readonly MemoryRepository<StockOut> _stockOuts = new();
    private readonly MemoryRepository<StockItem> _stockItems = new();
    private readonly MemoryRepository<FinanceReceivable> _receivables = new();
    private readonly SalesOrderRefreshCompletedGateService _service;

    public SalesOrderRefreshCompletedGateServiceTests()
    {
        _params.GetRefreshCompletedFacetsAsync(Arg.Any<CancellationToken>())
            .Returns(new SalesRefreshCompletedFacets());
        _service = new SalesOrderRefreshCompletedGateService(
            _params,
            _soItems,
            _notices,
            _packingItems,
            _packings,
            _stockOuts,
            _stockItems,
            _receivables);
    }

    [Fact]
    public async Task PreviewAsync_Status_AlwaysProceeds()
    {
        var preview = await _service.PreviewAsync("so-1", SalesOrderRefreshFacet.Status);
        Assert.True(preview.CanProceed);
        Assert.False(preview.HasCompleted);
    }

    [Fact]
    public async Task PreviewAsync_Pn_StockedOutNotice_ParamOff_Blocks()
    {
        _params.GetRefreshCompletedFacetsAsync(Arg.Any<CancellationToken>())
            .Returns(new SalesRefreshCompletedFacets { Pn = false });
        const string soId = "so-1";
        const string lineId = "line-1";
        await _soItems.AddAsync(new SellOrderItem { Id = lineId, SellOrderId = soId });
        await _notices.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            Status = StockOutRequestStatusCode.StockedOut
        });

        var preview = await _service.PreviewAsync(soId, SalesOrderRefreshFacet.Pn);

        Assert.False(preview.CanProceed);
        Assert.True(preview.HasCompleted);
        Assert.False(preview.AllowCompletedParam);
        Assert.Contains(preview.CompletedDocuments, d => d.Contains("STOR001"));
    }

    [Fact]
    public async Task EnsureAllowedAsync_Pn_ParamOn_WithoutConfirm_Throws()
    {
        const string soId = "so-1";
        const string lineId = "line-1";
        await _soItems.AddAsync(new SellOrderItem { Id = lineId, SellOrderId = soId });
        await _notices.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            Status = StockOutRequestStatusCode.StockedOut
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.EnsureAllowedAsync(soId, SalesOrderRefreshFacet.Pn, confirmCompleted: false));
        Assert.Contains("须确认后再刷新", ex.Message);
    }

    [Fact]
    public async Task EnsureAllowedAsync_Pn_ParamOn_WithConfirm_Succeeds()
    {
        const string soId = "so-1";
        const string lineId = "line-1";
        await _soItems.AddAsync(new SellOrderItem { Id = lineId, SellOrderId = soId });
        await _notices.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            Status = StockOutRequestStatusCode.StockedOut
        });

        await _service.EnsureAllowedAsync(soId, SalesOrderRefreshFacet.Pn, confirmCompleted: true);
    }

    [Fact]
    public async Task PreviewAsync_Qty_StockedOutNotice_HasCompleted()
    {
        const string soId = "so-1";
        const string lineId = "line-1";
        await _soItems.AddAsync(new SellOrderItem { Id = lineId, SellOrderId = soId });
        await _notices.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            Status = StockOutRequestStatusCode.StockedOut
        });

        var preview = await _service.PreviewAsync(soId, SalesOrderRefreshFacet.Qty);

        Assert.True(preview.CanProceed);
        Assert.True(preview.AllowCompletedParam);
        Assert.Contains(preview.CompletedDocuments, d => d.Contains("STOR001"));
    }

    [Fact]
    public async Task PreviewAsync_Price_VerifiedReceivable_HasCompleted()
    {
        const string soId = "so-1";
        const string lineId = "line-1";
        await _soItems.AddAsync(new SellOrderItem { Id = lineId, SellOrderId = soId });
        await _receivables.AddAsync(new FinanceReceivable
        {
            Id = "ar-1",
            ReceivableCode = "AR001",
            SellOrderItemId = lineId,
            VerifiedDone = 10m
        });

        var preview = await _service.PreviewAsync(soId, SalesOrderRefreshFacet.Price);

        Assert.True(preview.CanProceed);
        Assert.Contains(preview.CompletedDocuments, d => d.Contains("AR001"));
    }

    [Fact]
    public void SalesRefreshCompletedFacets_Defaults_CustomerOff_OthersOn()
    {
        var facets = new SalesRefreshCompletedFacets();
        Assert.False(facets.Customer);
        Assert.True(facets.Pn);
        Assert.True(facets.Brand);
        Assert.True(facets.Qty);
        Assert.True(facets.Price);
        Assert.False(facets.Allows(SalesOrderRefreshFacet.Customer));
        Assert.True(facets.Allows(SalesOrderRefreshFacet.Pn));
        Assert.True(facets.Allows(SalesOrderRefreshFacet.Status));
    }

    [Theory]
    [InlineData("status", SalesOrderRefreshFacet.Status)]
    [InlineData("customer", SalesOrderRefreshFacet.Customer)]
    [InlineData("pn", SalesOrderRefreshFacet.Pn)]
    [InlineData("brand", SalesOrderRefreshFacet.Brand)]
    [InlineData("qty", SalesOrderRefreshFacet.Qty)]
    [InlineData("price", SalesOrderRefreshFacet.Price)]
    public void Parser_AcceptsApiValues(string raw, SalesOrderRefreshFacet expected)
    {
        Assert.True(SalesOrderRefreshFacetParser.TryParse(raw, out var facet));
        Assert.Equal(expected, facet);
        Assert.Equal(raw, facet.ToApiValue());
    }
}
