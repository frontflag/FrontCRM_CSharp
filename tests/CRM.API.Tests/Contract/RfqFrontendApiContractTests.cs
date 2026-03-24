namespace CRM.API.Tests.Contract;

/// <summary>
/// 为什么「单元测试 + 控制器测试」没拦住 /detail 404：
/// <list type="bullet">
/// <item>Core 内存仓储测的是 RFQService，不经过 HTTP，也不读前端 URL。</item>
/// <item>API 层测的是「new RFQsController(...).GetRFQ(id)」方法调用，不是 HttpClient 访问真实路径字符串。</item>
/// <item>前后端契约（Vue rfq.ts 里写的路径 vs ASP.NET 路由）此前没有自动化校验，因此多写了 /detail 也不会在测试中失败。</item>
/// </list>
/// 本文件从仓库读取 <c>CRM.Web/src/api/rfq.ts</c>，对关键方法做最薄的一层契约回归。
/// </summary>
public sealed class RfqFrontendApiContractTests
{
    private static string GetRfqTsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "CRM.Web", "src", "api", "rfq.ts");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        Assert.Fail("找不到 CRM.Web/src/api/rfq.ts（请从解决方案根目录运行 dotnet test）。");
        return "";
    }

    [Fact]
    public void GetRFQDetail_must_use_same_path_as_backend_GetRFQ_not_detail_suffix()
    {
        var lines = File.ReadAllLines(GetRfqTsPath());
        var start = Array.FindIndex(
            lines,
            l => l.Contains("getRFQDetail", StringComparison.Ordinal) && l.Contains("Promise<RFQ>", StringComparison.Ordinal));

        Assert.True(start >= 0, "应在 rfq.ts 中找到 getRFQDetail");

        var returnLine = lines.Skip(start).FirstOrDefault(l => l.Contains("apiClient.get<RFQ>(`", StringComparison.Ordinal));
        Assert.False(string.IsNullOrEmpty(returnLine), "getRFQDetail 内应有 return apiClient.get<RFQ>(`...`)");

        var m = System.Text.RegularExpressions.Regex.Match(returnLine, @"`([^`]+)`");
        Assert.True(m.Success, "应能从 return 行解析出模板字符串");
        var urlTemplate = m.Groups[1].Value;

        Assert.DoesNotContain("/detail", urlTemplate, StringComparison.Ordinal);
        Assert.Contains("${id}", urlTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void GetRFQItemsWithBestQuote_must_not_call_nonexistent_best_quote_route()
    {
        var lines = File.ReadAllLines(GetRfqTsPath());
        var start = Array.FindIndex(
            lines,
            l => l.Contains("getRFQItemsWithBestQuote", StringComparison.Ordinal)
                && l.Contains("Promise<RFQItem[]>", StringComparison.Ordinal));

        Assert.True(start >= 0, "应在 rfq.ts 中找到 getRFQItemsWithBestQuote");

        // 取方法体若干行（含模板字符串中的 ${rfqId}，避免用 [^}]* 类正则误截断）
        var chunk = string.Join("\n", lines.Skip(start).Take(8));

        Assert.DoesNotContain("best-quote", chunk, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("${BASE}/${rfqId}", chunk, StringComparison.Ordinal);
    }
}
