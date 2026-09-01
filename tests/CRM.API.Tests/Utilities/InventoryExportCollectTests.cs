using CRM.API.Utilities;
using CRM.Core.Interfaces;

namespace CRM.API.Tests.Utilities;

public class InventoryExportCollectTests
{
    [Fact]
    public async Task CollectForExportAsync_keeps_paging_when_query_caps_page_size()
    {
        const int total = 250;
        const int queryCap = 100;
        var all = Enumerable.Range(1, total).ToList();
        var pages = 0;

        var (items, truncated, totalCount) = await InventoryExportHttp.CollectForExportAsync(
            (page, take, _) =>
            {
                pages++;
                var size = take < 1 ? 20 : Math.Min(take, queryCap);
                var skip = (page - 1) * size;
                return Task.FromResult(new PagedResult<int>
                {
                    Items = all.Skip(skip).Take(size).ToList(),
                    TotalCount = total,
                    PageIndex = page,
                    PageSize = size
                });
            });

        Assert.Equal(total, totalCount);
        Assert.False(truncated);
        Assert.Equal(total, items.Count);
        Assert.Equal(all, items);
        Assert.Equal(3, pages);
    }

    [Fact]
    public async Task CollectForExportAsync_stops_at_max_rows()
    {
        const int total = 400;
        const int cap = 100;
        var all = Enumerable.Range(1, total).ToList();

        var (items, truncated, totalCount) = await InventoryExportHttp.CollectForExportAsync(
            (page, take, _) =>
            {
                var size = Math.Min(take, cap);
                var skip = (page - 1) * size;
                return Task.FromResult(new PagedResult<int>
                {
                    Items = all.Skip(skip).Take(size).ToList(),
                    TotalCount = total,
                    PageIndex = page,
                    PageSize = size
                });
            },
            maxRows: 150);

        Assert.Equal(total, totalCount);
        Assert.True(truncated);
        Assert.Equal(150, items.Count);
        Assert.Equal(all.Take(150), items);
    }
}
