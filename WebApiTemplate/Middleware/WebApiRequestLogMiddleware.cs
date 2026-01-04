using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

public class WebApiRequestLogMiddleware : IMiddleware
{
    private readonly IWebApiRequestLogRepository _repo;
    private readonly WebApiRequestLogOptions _opt;
    private readonly WebApiRequestLogQueue _queue;

    public WebApiRequestLogMiddleware(
        IWebApiRequestLogRepository repo,
        IOptions<WebApiRequestLogOptions> options,
        WebApiRequestLogQueue queue)
    {
        _repo = repo;
        _opt = options.Value;
        _queue = queue;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!_opt.Enabled)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // (B) 路徑策略：Hard exclude > exclude > include
        if (StartsWithAny(path, _opt.ExcludedPathPrefixesHard))
        {
            await next(context);
            return;
        }

        if (StartsWithAny(path, _opt.ExcludedPathPrefixes))
        {
            await next(context);
            return;
        }

        if (_opt.IncludedPathPrefixes.Length > 0 && !StartsWithAny(path, _opt.IncludedPathPrefixes))
        {
            await next(context);
            return;
        }

        var requestId = RequestHelper.GetRequestId(context);
        var traceId = RequestHelper.GetTraceId(context);
        var requestTimeUtc = DateTime.UtcNow;

        // Request capture
        string? requestHeadersJson = null;
        string? requestBody = null;

        if (_opt.LogRequestHeaders)
        {
            requestHeadersJson = SerializeHeadersMasked(context.Request.Headers, _opt.SensitiveHeaders);
            requestHeadersJson = Truncate(requestHeadersJson, _opt.MaxBodyChars);
        }

        if (_opt.LogRequestBody && RequestHasBody(context.Request) && IsAllowedContentType(context.Request.ContentType, _opt.RequestBodyContentTypesAllowList))
        {
            context.Request.EnableBuffering();
            requestBody = await ReadRequestBodyAsync(context.Request);
            requestBody = MaskJsonIfPossible(requestBody, _opt.SensitiveJsonKeys);
            requestBody = Truncate(requestBody, _opt.MaxBodyChars);
        }

        var sw = Stopwatch.StartNew();

        // Response capture
        var originalBody = context.Response.Body;
        var originalBodyFeature = context.Features.Get<IHttpResponseBodyFeature>();

        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        int statusCode = 0;
        string? responseBody = null;

        try
        {
            await next(context); // ✅ 不處理例外；讓全域例外處理決定回應
        }
        finally
        {
            sw.Stop();

            // 先回放到 client
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            statusCode = context.Response.StatusCode;

            // Response body：只有 allowlist content-type 才讀
            if (_opt.LogResponseBody && IsAllowedContentType(context.Response.ContentType, _opt.ResponseBodyContentTypesAllowList))
            {
                responseBody = await ReadResponseBodyFromBufferAsync(buffer);
                responseBody = MaskJsonIfPossible(responseBody, _opt.SensitiveJsonKeys);
                responseBody = Truncate(responseBody, _opt.MaxBodyChars);
            }

            var log = new WebApiRequestLog
            {
                RequestId = requestId,
                TraceId = traceId,
                RequestDateTime = requestTimeUtc,

                RequestHeader = requestHeadersJson,
                RequestBody = requestBody,

                ResponseHttpStatusCode = statusCode,
                ResponseBody = responseBody,

                DurationMs = (int)sw.ElapsedMilliseconds
            };

            try
            {
                if (_opt.EnableBatchWrite)
                    _queue.TryEnqueue(log);
                else
                    await _repo.AddAsync(log, context.RequestAborted);
            }
            catch
            {
                // 不讓寫 log 影響主流程
            }
        }
    }

    private static bool StartsWithAny(string path, string[] prefixes)
        => prefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static bool RequestHasBody(HttpRequest req)
    {
        if (HttpMethods.IsGet(req.Method) || HttpMethods.IsHead(req.Method))
            return false;

        // ContentLength 可能為 null（chunked），所以不要用它當唯一判斷
        return true;
    }

    private static bool IsAllowedContentType(string? contentType, string[] allowList)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var ct = contentType.Split(';', 2)[0].Trim();
        return allowList.Any(a => ct.Equals(a, StringComparison.OrdinalIgnoreCase));
    }

    private static string SerializeHeadersMasked(IHeaderDictionary headers, string[] sensitiveHeaders)
    {
        var maskSet = new HashSet<string>(sensitiveHeaders, StringComparer.OrdinalIgnoreCase);

        var dict = headers.ToDictionary(
            h => h.Key,
            h => maskSet.Contains(h.Key) ? "***MASKED***" : (string?)h.Value.ToString());

        return JsonSerializer.Serialize(dict);
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static async Task<string?> ReadResponseBodyFromBufferAsync(MemoryStream buffer)
    {
        if (buffer.Length == 0) return null;

        buffer.Position = 0;
        using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        buffer.Position = 0;

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? Truncate(string? s, int maxChars)
        => string.IsNullOrEmpty(s) ? s : (s.Length <= maxChars ? s : s[..maxChars] + "...(truncated)");

    private static string? MaskJsonIfPossible(string? text, string[] sensitiveKeys)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var trimmed = text.TrimStart();
        if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
            return text;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var masked = MaskJsonElement(doc.RootElement, new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase));
            return JsonSerializer.Serialize(masked);
        }
        catch
        {
            return text;
        }
    }

    private static object? MaskJsonElement(JsonElement el, HashSet<string> sensitiveKeys)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(
                p => p.Name,
                p => sensitiveKeys.Contains(p.Name) ? "***MASKED***" : MaskJsonElement(p.Value, sensitiveKeys)
            ),
            JsonValueKind.Array => el.EnumerateArray().Select(x => MaskJsonElement(x, sensitiveKeys)).ToList(),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }
}
