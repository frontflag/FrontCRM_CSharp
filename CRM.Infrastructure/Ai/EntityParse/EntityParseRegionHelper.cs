using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRM.Infrastructure.Ai.EntityParse;

internal sealed class RegionNode
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<RegionNode>? Children { get; set; }
}

/// <summary>与 CRM.Web/src/constants/region.ts + data/regions.ts 对齐的省市区补全。</summary>
internal static class EntityParseRegionHelper
{
    private static readonly Lazy<List<RegionNode>> RegionData = new(LoadRegionData);

    private static List<RegionNode> LoadRegionData()
    {
        var asm = typeof(EntityParseRegionHelper).Assembly;
        const string resourceName = "CRM.Infrastructure.Ai.EntityParse.china_regions.json";
        using var stream = asm.GetManifestResourceStream(resourceName)
                         ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}");
        var nodes = JsonSerializer.Deserialize<List<RegionNode>>(stream)
                    ?? new List<RegionNode>();
        return nodes;
    }

    private static string StripAdminSuffix(string name)
    {
        var s = name.Trim();
        if (string.IsNullOrEmpty(s)) return s;
        foreach (var suf in new[] { "特别行政区", "自治区", "省", "市", "区", "县" })
        {
            if (s.EndsWith(suf, StringComparison.Ordinal) && s.Length > suf.Length)
                return s[..^suf.Length];
        }

        return s;
    }

    private static bool RegionNamesEqual(string a, string b)
    {
        var ta = StripAdminSuffix(a);
        var tb = StripAdminSuffix(b);
        return ta.Length > 0 && ta == tb;
    }

    public static string LookupProvinceFromCityDistrict(string? city, string? district = null)
    {
        var cityRaw = (city ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(cityRaw)) return string.Empty;

        var districtRaw = (district ?? string.Empty).Trim();
        var matches = new List<(string Province, int Score)>();

        foreach (var province in RegionData.Value)
        {
            var pName = province.Value;
            if (RegionNamesEqual(cityRaw, pName))
                matches.Add((pName, districtRaw.Length > 0 ? 2 : 3));

            foreach (var cityNode in province.Children ?? [])
            {
                if (!RegionNamesEqual(cityRaw, cityNode.Value)) continue;
                var score = 3;
                if (districtRaw.Length > 0)
                {
                    var distMatch = (cityNode.Children ?? []).Any(d => RegionNamesEqual(districtRaw, d.Value));
                    score = distMatch ? 5 : 1;
                }

                matches.Add((pName, score));
            }
        }

        if (matches.Count == 0) return string.Empty;
        return matches.OrderByDescending(m => m.Score).First().Province;
    }

    public static (string Province, string City, string? District)? LookupCanonicalRegionLabels(
        string province,
        string city,
        string? district)
    {
        var pRaw = province.Trim();
        var cRaw = city.Trim();
        var dRaw = (district ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(pRaw) && string.IsNullOrEmpty(cRaw)) return null;

        foreach (var p in RegionData.Value)
        {
            if (pRaw.Length > 0 && !RegionNamesEqual(pRaw, p.Value)) continue;
            foreach (var c in p.Children ?? [])
            {
                if (!RegionNamesEqual(cRaw, c.Value)) continue;
                string? districtOut = dRaw.Length > 0 ? dRaw : null;
                if (dRaw.Length > 0)
                {
                    var found = (c.Children ?? []).FirstOrDefault(d => RegionNamesEqual(dRaw, d.Value));
                    if (found != null) districtOut = found.Value;
                }

                return (p.Value, c.Value, districtOut);
            }

            if (RegionNamesEqual(cRaw, p.Value))
            {
                string? districtOut = dRaw.Length > 0 ? dRaw : null;
                var cityNode = p.Children?.FirstOrDefault();
                if (dRaw.Length > 0 && cityNode?.Children != null)
                {
                    var found = cityNode.Children.FirstOrDefault(d => RegionNamesEqual(dRaw, d.Value));
                    if (found != null) districtOut = found.Value;
                }

                return (p.Value, p.Value, districtOut);
            }
        }

        if (string.IsNullOrEmpty(pRaw) && cRaw.Length > 0)
        {
            foreach (var p in RegionData.Value)
            {
                foreach (var c in p.Children ?? [])
                {
                    if (!RegionNamesEqual(cRaw, c.Value)) continue;
                    string? districtOut = dRaw.Length > 0 ? dRaw : null;
                    if (dRaw.Length > 0)
                    {
                        var found = (c.Children ?? []).FirstOrDefault(d => RegionNamesEqual(dRaw, d.Value));
                        if (found != null) districtOut = found.Value;
                    }

                    return (p.Value, c.Value, districtOut);
                }

                if (RegionNamesEqual(cRaw, p.Value))
                    return (p.Value, p.Value, dRaw.Length > 0 ? dRaw : null);
            }
        }

        return null;
    }

    public static RegionFields EnrichCustomerRegionFields(RegionFields fields)
    {
        var province = fields.Province.Trim();
        var city = fields.City.Trim();
        var district = fields.District.Trim();
        var country = fields.Country.Trim();

        if (string.IsNullOrEmpty(province) && city.Length > 0)
            province = LookupProvinceFromCityDistrict(city, district);

        var canonical = LookupCanonicalRegionLabels(province, city, district);
        if (canonical != null)
        {
            province = canonical.Value.Province;
            city = canonical.Value.City;
            if (!string.IsNullOrEmpty(canonical.Value.District))
                district = canonical.Value.District!;
        }

        if (string.IsNullOrEmpty(country) && province.Length > 0)
            country = "中国";

        return fields with
        {
            Province = province,
            City = city,
            District = district,
            Country = country
        };
    }

    internal readonly record struct RegionFields(
        string Province,
        string City,
        string District,
        string Country,
        string Address = "");
}
