using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Serilog.Core;
using Serilog.Events;

public sealed class TraceIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 新增公用無參數建構函式
    public TraceIdEnricher()
    {
        _httpContextAccessor = new HttpContextAccessor();
    }

    // 若需要注入 IHttpContextAccessor，請保留此建構函式
    public TraceIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return;

        var activity = ctx.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
        if (activity?.Id is null) return;

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("TraceId", activity.Id));
    }
}


