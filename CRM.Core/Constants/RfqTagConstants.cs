using CRM.Core.Models.Tag;

namespace CRM.Core.Constants;

/// <summary>需求主表（RFQ）标签常量。</summary>
public static class RfqTagConstants
{
    public const string EntityType = "RFQ";

    /// <summary>用户自定义 RFQ 标签归属编码前缀：<c>RFQ_OWNER:{userGuid}</c></summary>
    public const string OwnerCodePrefix = "RFQ_OWNER:";

    public static readonly string[] SystemTagNames =
    {
        "加急",
        "重点跟进",
        "需二次寻源",
        "追单"
    };

    public static bool IsReservedSystemTagName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();
        return SystemTagNames.Any(n => string.Equals(n, trimmed, StringComparison.Ordinal));
    }

    public static string BuildOwnerCode(string userId) => OwnerCodePrefix + userId.Trim();

    public static bool IsOwnedByUser(TagDefinition tag, string userId)
    {
        if (tag.Type == 1) return true;
        var code = tag.Code?.Trim();
        if (string.IsNullOrEmpty(code)) return false;
        return string.Equals(code, BuildOwnerCode(userId), StringComparison.OrdinalIgnoreCase);
    }
}
