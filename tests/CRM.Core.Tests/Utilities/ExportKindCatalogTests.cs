using CRM.Core.Constants;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class ExportKindCatalogTests
{
    [Fact]
    public void SanitizePageUrl_RejectsProtocolAndQuery()
    {
        Assert.Null(ExportKindCatalog.SanitizePageUrl("https://evil.example/x"));
        Assert.Null(ExportKindCatalog.SanitizePageUrl("//cdn.example/x"));
        Assert.Equal("/sales-orders/abc", ExportKindCatalog.SanitizePageUrl("/sales-orders/abc?x=1#y"));
    }

    [Fact]
    public void Hydrate_FallsBackToCatalog_WhenExtraMissingPage()
    {
        var json = ExportOperationAudit.BuildExtraInfoJson(
            ExportAuditKinds.SalesOrderItemList,
            3,
            new Dictionary<string, object?>(),
            filtersMasked: false);
        var d = ExportKindCatalog.Hydrate(json);
        Assert.Equal("销售订单明细列表", d.BusinessTypeName);
        Assert.Equal("销售订单明细", d.PageTitle);
        Assert.Equal("/sales-order-items", d.PageUrl);
        Assert.Equal(3, d.ExportedCount);
        Assert.Equal("全部（无筛选）", d.FilterSummary);
        Assert.Null(d.SysRemark);
    }

    [Fact]
    public void BuildSysRemark_TruncatedAndMasked()
    {
        Assert.Equal("", ExportKindCatalog.BuildSysRemark(false, false, 50000));
        Assert.Equal("已截断（上限 50000）", ExportKindCatalog.BuildSysRemark(true, false, 50000));
        Assert.Equal("已截断（上限 50000）；条件已按权限脱敏", ExportKindCatalog.BuildSysRemark(true, true, 50000));
    }
}
