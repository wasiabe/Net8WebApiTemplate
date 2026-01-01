using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;

public class GlobalExceptionHandler(string? exceptionMessage = null) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        Log.Error(exception, "發生未預期錯誤: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server Error",
            Detail = exceptionMessage ?? exception.Message,
            Instance = httpContext.Request.Path
        };

        // 擴充要求的欄位
        // 單機追蹤:當 API 報錯時，客戶端把這個 ID 給後端人員，開發者可以直接在 Log 中找到該次報錯的上下文。
        // 從 Header 取得 X-Request-Id，若無此 Header 或空值再回傳 TraceIdentifier
        string requestId;
        if (httpContext.Request.Headers.TryGetValue("X-Request-Id", out var requestIdValues))
        {
            // 取第一個非空白的值，並去除前後 whitespace
            var headerValue = requestIdValues.FirstOrDefault()?.Trim();
            requestId = string.IsNullOrWhiteSpace(headerValue) ? httpContext.TraceIdentifier : headerValue!;
        }
        else
        {
            requestId = httpContext.TraceIdentifier;
        }
        problemDetails.Extensions.TryAdd("requestId",requestId);

        // 分散式追蹤:在微服務架構中，若 A 服務調用 B 服務，B 再調用 C，它們會共用同一個 Trace ID
        var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
        problemDetails.Extensions.TryAdd("traceId", activity?.Id ?? httpContext.TraceIdentifier); 

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}