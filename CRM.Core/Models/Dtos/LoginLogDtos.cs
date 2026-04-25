namespace CRM.Core.Models.Dtos;

public sealed class LoginLogQuery
{
    public DateTime? LoginAtFrom { get; set; }
    public DateTime? LoginAtTo { get; set; }
    public string? UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class LoginLogListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime LoginAt { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public short LoginMethod { get; set; }
}

public sealed class LoginLogPagedResult
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<LoginLogListItemDto> Items { get; set; } = Array.Empty<LoginLogListItemDto>();
}
