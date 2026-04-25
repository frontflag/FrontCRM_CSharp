using CRM.Core.Models.System;

namespace CRM.API.Services.Interfaces;

public interface ILoginLogService
{
    /// <summary>写入一条成功登录记录；地理解析失败时仍落库（GeoSource=none）。</summary>
    Task RecordAsync(string userId, string userName, LoginMethod method, string? actorUserId, string clientIp, CancellationToken cancellationToken = default);
}
