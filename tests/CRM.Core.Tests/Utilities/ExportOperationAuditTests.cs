using CRM.Core.Constants;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class ExportOperationAuditTests
{
    [Fact]
    public void BuildFilterSummary_Empty_ReturnsAll()
    {
        var summary = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.StockInList,
            new Dictionary<string, object?>());
        Assert.Equal("全部（无筛选）", summary);
    }

    [Fact]
    public void BuildExtraInfoJson_ContainsFilterSummaryAndCount()
    {
        var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
        {
            ["stockInCode"] = "SI001",
            ["page"] = 1,
            ["vendorName"] = "  "
        });
        var json = ExportOperationAudit.BuildExtraInfoJson(
            ExportAuditKinds.StockInList,
            12,
            filters,
            filtersMasked: true,
            truncated: false);
        Assert.Contains("\"exportedCount\":12", json);
        Assert.Contains("filterSummary", json);
        Assert.Contains("SI001", json);
        Assert.DoesNotContain("\"page\"", json);
    }

    [Fact]
    public void TryParseFilterSummary_ReadsExisting()
    {
        var json = ExportOperationAudit.BuildExtraInfoJson(
            ExportAuditKinds.StockOutList,
            3,
            new Dictionary<string, object?> { ["stockOutCode"] = "SO1" },
            false);
        var summary = ExportOperationAudit.TryParseFilterSummary(json);
        Assert.Contains("SO1", summary);
    }

    [Fact]
    public void BuildFilterSummary_WarehouseId_UsesDisplayName()
    {
        var warehouseId = "def6b268-e7e4-42df-9165-85a23e57dab1";
        var summary = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.StockInList,
            new Dictionary<string, object?> { ["warehouseId"] = warehouseId },
            new ExportFilterDisplayContext
            {
                WarehouseNamesById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [warehouseId] = "深圳仓"
                }
            });
        Assert.Equal("仓库=深圳仓", summary);
        Assert.DoesNotContain(warehouseId, summary);
    }

    [Fact]
    public void BuildFilterSummary_Enums_UseBusinessLabels()
    {
        var stockIn = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.StockInList,
            new Dictionary<string, object?> { ["stockInType"] = StockInTypeCode.Purchase });
        Assert.Equal("入库类型=采购入库", stockIn);

        var stockOut = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.StockOutList,
            new Dictionary<string, object?>
            {
                ["stockOutType"] = StockOutTypeCode.Sales,
                ["status"] = 1,
                ["shipmentMethod"] = LogisticsShipmentMethodCode.Express
            });
        Assert.Contains("出库类型=销售出库", stockOut);
        Assert.Contains("状态=待出库", stockOut);
        Assert.Contains("出货方式=快递", stockOut);

        var inventory = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.InventoryStockList,
            new Dictionary<string, object?> { ["stockType"] = 2 });
        Assert.Equal("库存类型=备货库存", inventory);

        var stockItems = ExportOperationAudit.BuildFilterSummary(
            ExportAuditKinds.InventoryStockItemList,
            new Dictionary<string, object?>
            {
                ["outboundStatus"] = 2,
                ["repertoryHasStock"] = true
            });
        Assert.Contains("出库状态=部分出库", stockItems);
        Assert.Contains("是否有库存=有库存", stockItems);
    }

    [Fact]
    public void BuildExtraInfoJson_KeepsRawFilterCodes()
    {
        var filters = new Dictionary<string, object?> { ["stockInType"] = 10 };
        var json = ExportOperationAudit.BuildExtraInfoJson(
            ExportAuditKinds.StockInList,
            1,
            filters,
            false);
        Assert.Contains("\"stockInType\":10", json);
        Assert.Equal("入库类型=采购入库", ExportOperationAudit.TryParseFilterSummary(json));
    }
}
