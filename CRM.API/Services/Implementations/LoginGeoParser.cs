namespace CRM.API.Services.Implementations;

public sealed class LoginGeoParts
{
    public string? Country { get; init; }
    public string? Province { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? Street { get; init; }
    public string? AddressLine { get; init; }
}

public static class LoginGeoParser
{
    /// <summary>解析 ip2region 默认竖线格式（国家|区域|省份|城市|ISP）。</summary>
    public static LoginGeoParts? FromRegionRaw(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var parts = raw.Split('|', StringSplitOptions.None);
        if (parts.Length == 0)
            return null;

        string? S(int i) => i < 0 || i >= parts.Length ? null : Norm(parts[i]);

        var country = S(0);
        var province = S(2);
        var city = S(3);
        // 段 1 多为区域/运营商无关的占位；段 4 一般为 ISP，不写入「登录地址」主串
        string? district = null;
        string? street = null;

        var lineParts = new List<string>();
        void Add(string? s)
        {
            if (!string.IsNullOrEmpty(s))
                lineParts.Add(s);
        }

        Add(country);
        Add(province);
        Add(city);

        var addressLine = lineParts.Count > 0 ? string.Concat(lineParts) : null;

        return new LoginGeoParts
        {
            Country = country,
            Province = province,
            City = city,
            District = district,
            Street = street,
            AddressLine = addressLine
        };
    }

    private static string? Norm(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        s = s.Trim();
        if (s.Length == 0 || s == "0")
            return null;
        return s;
    }
}
