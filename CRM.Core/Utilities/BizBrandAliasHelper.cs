namespace CRM.Core.Utilities;

/// <summary>品牌别名解析（<c>biz_brand.alias</c>，逗号/换行等分隔）。</summary>
public static class BizBrandAliasHelper
{
    private static readonly char[] TokenSeparators = [',', ';', '\n', '\r', '，', '、', '|'];

    /// <summary>别名中是否包含与 <paramref name="keyword"/> 完全一致的 token（忽略大小写）。</summary>
    public static bool ContainsExactToken(string? alias, string keyword)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(keyword))
            return false;

        var kw = keyword.Trim();
        foreach (var token in alias.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(token.Trim(), kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
