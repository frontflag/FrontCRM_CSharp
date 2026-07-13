using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

internal sealed class PeerIntelSnapshot
{
    public string ReportJson { get; init; } = "{}";
    public string SchemaVersion { get; init; } = "1.0";
    public string? InvocationLogId { get; init; }
}

internal static class IntelReportPeerCache
{
    public static async Task<PeerIntelSnapshot?> TryLoadFromCustomerTableAsync(
        ApplicationDbContext db,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var row = await db.CustomerIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return row == null
            ? null
            : new PeerIntelSnapshot
            {
                ReportJson = row.ReportJson,
                SchemaVersion = row.SchemaVersion,
                InvocationLogId = row.InvocationLogId
            };
    }

    public static async Task<PeerIntelSnapshot?> TryLoadFromVendorTableAsync(
        ApplicationDbContext db,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var row = await db.VendorIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return row == null
            ? null
            : new PeerIntelSnapshot
            {
                ReportJson = row.ReportJson,
                SchemaVersion = row.SchemaVersion,
                InvocationLogId = row.InvocationLogId
            };
    }
}
