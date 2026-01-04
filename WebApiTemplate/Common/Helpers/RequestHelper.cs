using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

public static class RequestHelper
{
    /// <summary>
    /// 從 HttpContext 嘗試取得 request id：
    /// 1. 先讀取指定 header（預設 "X-Request-Id"），回傳第一個非空白值。
    /// 2. 若 header 不存在或值皆空白，回退到 TraceIdentifier。
    /// </summary>
    public static string GetRequestId(HttpContext? ctx, string headerName = "X-Request-Id")
    {
        if (ctx == null)
        {
            return string.Empty;
        }

        var headers = ctx.Request?.Headers;
        if (headers != null && headers.TryGetValue(headerName, out var headerValues))
        {
            // 逐項檢查 header 的各個值，取第一個非空白
            foreach (var value in headerValues)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value!;
                }
            }

            // 若多值合併後有內容也回傳
            var combined = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(combined))
            {
                return combined;
            }
        }

        // 回退到 TraceIdentifier（若為 null，回傳空字串）
        return ctx.TraceIdentifier ?? string.Empty;
    }

    /// <summary>
    /// 取得Trace Id
    /// 分散式追蹤:在微服務架構中，若 A 服務調用 B 服務，B 再調用 C，它們會共用同一個 Trace ID
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public static string GetTraceId (HttpContext? ctx)
    {
        var activity = ctx?.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;

        return activity?.Id ?? (ctx?.TraceIdentifier ?? string.Empty);
    }
}