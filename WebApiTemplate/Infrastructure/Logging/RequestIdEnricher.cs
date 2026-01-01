using Serilog.Core;
using Serilog.Events;

public sealed class RequestIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 新增公用無參數建構函式
    public RequestIdEnricher()
    {
        _httpContextAccessor = new HttpContextAccessor();
    }

    public RequestIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return;
        }

        string? requestId = null;

        // 嘗試從 X-Request-Id header 取得
        if (context.Request?.Headers != null &&
            context.Request.Headers.TryGetValue("X-Request-Id", out var headerValues))
        {
            var headerValue = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                requestId = headerValue;
            }
        }

        // 若 header 不存在或為空，使用 TraceIdentifier 作為 fallback
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = context.TraceIdentifier;
        }

        // 最終加入 LogEvent 屬性
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("RequestId", requestId));
    }
}
