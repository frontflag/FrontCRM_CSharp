using System.Net;

namespace CRM.API.Utilities;

public static class ClientIpResolver
{
    public static string Resolve(HttpContext httpContext)
    {
        var req = httpContext.Request;

        var xff = req.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xff))
        {
            var first = xff.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(first) && first != "unknown")
                return NormalizeForLog(first);
        }

        var xri = req.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xri) && xri != "unknown")
            return NormalizeForLog(xri.Trim());

        var remote = httpContext.Connection.RemoteIpAddress;
        if (remote is null)
            return "";

        if (remote.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (remote.IsIPv4MappedToIPv6)
                remote = remote.MapToIPv4();
        }

        return NormalizeForLog(remote.ToString());
    }

    private static string NormalizeForLog(string ip)
    {
        ip = ip.Trim();
        if (ip.Length == 0)
            return "";
        if (IPAddress.TryParse(ip, out var addr) && addr.IsIPv4MappedToIPv6)
            return addr.MapToIPv4().ToString();
        return ip;
    }
}
