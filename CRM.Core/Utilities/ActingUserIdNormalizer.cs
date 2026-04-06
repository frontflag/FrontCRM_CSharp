namespace CRM.Core.Utilities
{
    /// <summary>JWT NameIdentifier / user 主键 GUID 规范化（去空白，空则 null）。</summary>
    public static class ActingUserIdNormalizer
    {
        public static string? Normalize(string? actingUserId) =>
            string.IsNullOrWhiteSpace(actingUserId) ? null : actingUserId.Trim();
    }
}
