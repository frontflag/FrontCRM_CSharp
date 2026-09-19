namespace CRM.Core.Constants;

/// <summary>
/// 上传文档业务分类（表 <c>upload_document.DocCategory</c>）。
/// 出库单附件使用三类：出货照片 / 出货签收单 / 其他；其它业务未传时归 <see cref="Other"/>。
/// </summary>
public static class UploadDocumentCategory
{
    public const string ShipPhoto = "SHIP_PHOTO";
    public const string Pod = "POD";
    public const string Other = "OTHER";

    public static readonly string[] All = [ShipPhoto, Pod, Other];

    /// <summary>空值或未知码一律归「其他」，不抛错。</summary>
    public static string Normalize(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length == 0)
            return Other;
        if (v.Equals(ShipPhoto, StringComparison.OrdinalIgnoreCase))
            return ShipPhoto;
        if (v.Equals(Pod, StringComparison.OrdinalIgnoreCase))
            return Pod;
        if (v.Equals(Other, StringComparison.OrdinalIgnoreCase))
            return Other;
        return Other;
    }
}
