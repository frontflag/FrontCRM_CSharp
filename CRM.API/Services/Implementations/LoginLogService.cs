using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using IP2Region.Net.Abstractions;

namespace CRM.API.Services.Implementations;

public sealed class LoginLogService : ILoginLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ISearcher _searcher;
    private readonly ILogger<LoginLogService> _logger;

    public LoginLogService(ApplicationDbContext db, ISearcher searcher, ILogger<LoginLogService> logger)
    {
        _db = db;
        _searcher = searcher;
        _logger = logger;
    }

    public async Task RecordAsync(string userId, string userName, LoginMethod method, string? actorUserId, string clientIp, CancellationToken cancellationToken = default)
    {
        var ip = (clientIp ?? "").Trim();
        if (ip.Length > 45)
            ip = ip[..45];

        string? regionRaw = null;
        string geoSource = "none";
        LoginGeoParts? geo = null;

        if (ip.Length > 0)
        {
            try
            {
                regionRaw = _searcher.Search(ip);
                if (!string.IsNullOrWhiteSpace(regionRaw))
                {
                    geoSource = "ip2region";
                    geo = LoginGeoParser.FromRegionRaw(regionRaw);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ip2region lookup skipped for {Ip}", ip);
            }
        }

        var row = new LoginLog
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName,
            LoginAt = DateTime.UtcNow,
            ClientIp = ip.Length > 0 ? ip : "unknown",
            Country = geo?.Country,
            Province = geo?.Province,
            City = geo?.City,
            District = geo?.District,
            Street = geo?.Street,
            AddressLine = geo?.AddressLine,
            RegionRaw = regionRaw,
            LoginMethod = (short)method,
            ActorUserId = string.IsNullOrWhiteSpace(actorUserId) ? null : actorUserId.Trim(),
            GeoSource = geoSource
        };

        _db.LoginLogs.Add(row);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist login log for UserId={UserId}", userId);
        }
    }
}
