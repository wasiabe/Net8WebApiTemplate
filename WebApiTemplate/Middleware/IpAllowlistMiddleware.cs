using Microsoft.Extensions.Options;

public sealed class IpAllowlistMiddleware : IMiddleware
{
    private readonly IpAllowlistOptions _options;
    private readonly HashSet<string> _allow;

    public IpAllowlistMiddleware(IOptions<IpAllowlistOptions> options)
    {
        _options = options.Value;
        _allow = new HashSet<string>(_options.Allow, StringComparer.OrdinalIgnoreCase);
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var clientIp = ClientIpResolver.GetClientIp(context, _options);

        if (clientIp is null || !_allow.Contains(clientIp))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsync("Forbidden: IP is not allowed.");
        }

        return next(context);
    }
}
