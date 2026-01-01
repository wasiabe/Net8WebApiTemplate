
public static class ClientIpResolver
{
    public static string? GetClientIp(HttpContext ctx, IpAllowlistOptions options)
    {
        var remoteIp = ctx.Connection.RemoteIpAddress?.ToString();

        if (options.Mode == IpAllowlistMode.SourceIpOnly)
        {
            return remoteIp;
        }

        // Proxy + X-Forwarded-For mode:
        // Trust XFF only if remote IP is a trusted proxy.
        if (remoteIp is null)
        {
            return null;
        }

        if (!options.TrustedProxies.Contains(remoteIp, StringComparer.OrdinalIgnoreCase))
        {
            return remoteIp;
        }

        if (!ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var xff) || string.IsNullOrWhiteSpace(xff))
        {
            return remoteIp;
        }

        // XFF: "client, proxy1, proxy2" -> take first
        var first = xff.ToString().Split(',').Select(s => s.Trim()).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? remoteIp : first;
    }
}
