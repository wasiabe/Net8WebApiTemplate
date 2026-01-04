using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

public static class CustomizedProblemDetailsFactory
{
    public static ProblemDetails FromResult(HttpContext ctx, Result result)
    {
        var error = result.Error ?? new Error("unexpected_error", "An unexpected error occurred.");

        var (status, title) = error.Code switch
        {
            "validation_error" => (StatusCodes.Status400BadRequest, "Bad Request"),
            "unauthorized" => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            "forbidden" => (StatusCodes.Status403Forbidden, "Forbidden"),
            "not_found" => (StatusCodes.Status404NotFound, "Not Found"),
            "conflict" => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = error.Message,
            Type = $"urn:problem-type:{error.Code}",
            Instance = ctx.Request.Path
        };

        pd.Extensions.TryAdd("code", error.Code);

        // 使用 helper 取得 request id（先從 X-Request-Id，若無或為空則回退到 TraceIdentifier）
        string requestId = RequestHelper.GetRequestId(ctx);
        pd.Extensions.TryAdd("requestId", requestId);

        string traceId = RequestHelper.GetTraceId(ctx);
        pd.Extensions.TryAdd("traceId", traceId);

        return pd;
    }
}


