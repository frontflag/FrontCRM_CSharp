namespace CRM.Core.Utilities;

/// <summary>
/// 供应商 Excel 导入查重：未删除记录按名称、统一社会信用代码建索引。
/// 名称优先于信用代码。同一键保留先写入的一条（调用方按创建时间升序装入库内记录）。
/// </summary>
public sealed class VendorImportLookup
{
    public sealed class Hit
    {
        public string Id { get; init; } = "";
        public string Code { get; init; } = "";

        /// <summary>本批已接受、尚未落库的名称。不作为「已有供应商编号」返回。</summary>
        public bool IsPending => string.IsNullOrEmpty(Code);
    }

    private readonly Dictionary<string, Hit> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Hit> _byCredit = new(StringComparer.Ordinal);

    public void Add(string? officialName, string? creditCode, string id, string code)
    {
        var name = VendorDuplicateKeys.NormalizeName(officialName);
        if (name != null && !_byName.ContainsKey(name))
            _byName[name] = new Hit { Id = id, Code = code };

        var credit = VendorDuplicateKeys.NormalizeCreditCode(creditCode);
        if (credit != null && !_byCredit.ContainsKey(credit))
            _byCredit[credit] = new Hit { Id = id, Code = code };
    }

    /// <summary>先比名称，再比统一社会信用代码。空值不参与。</summary>
    public Hit? Match(string? officialName, string? creditCode)
    {
        var name = VendorDuplicateKeys.NormalizeName(officialName);
        if (name != null && _byName.TryGetValue(name, out var byName))
            return byName;

        var credit = VendorDuplicateKeys.NormalizeCreditCode(creditCode);
        if (credit != null && _byCredit.TryGetValue(credit, out var byCredit))
            return byCredit;

        return null;
    }
}
