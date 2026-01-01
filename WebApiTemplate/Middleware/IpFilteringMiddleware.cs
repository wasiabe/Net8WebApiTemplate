using System.Net;

public class IpFilteringMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task Invoke(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        // 取得 X-Forwarded-For (處理 Proxy)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0) remoteIp = IPAddress.Parse(ips[0].Trim());
        }

        // 這裡實作 IP 白名單邏輯...
        await next(context);
    }
}