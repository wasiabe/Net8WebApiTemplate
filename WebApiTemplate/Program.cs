global using Serilog;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog.Events;
using System.Diagnostics;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// Serilog 配置: Read configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration).CreateLogger();

//Redirect all log events through Serilog pipeline.
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.With<RequestIdEnricher>();

    var httpAccessor = services.GetRequiredService<IHttpContextAccessor>();
    cfg.Enrich.With(new TraceIdEnricher(httpAccessor));
});

// 設定 Options 
builder.Services.AddOptions<IpAllowlistOptions>()
    .Bind(builder.Configuration.GetSection(IpAllowlistOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // NOTE: In production, restrict KnownNetworks / KnownProxies!
});

// Rate Limiting 限流配置
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var rateCfg = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        var ipCfg = httpContext.RequestServices.GetRequiredService<IOptions<IpAllowlistOptions>>().Value;

        var clientIp = ClientIpResolver.GetClientIp(httpContext, ipCfg) ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateCfg.PermitLimit,
                Window = TimeSpan.FromSeconds(rateCfg.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = rateCfg.QueueLimit
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// 註冊自定義錯誤處理器
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 使用準化錯誤回應格式 RFC 9457
builder.Services.AddProblemDetails();

//在 非 Controller 的地方 也能存取目前的 HTTP Request, 例如 Middleware, Service, Logging
builder.Services.AddHttpContextAccessor();  

builder.Services.AddControllers();

//探索API
builder.Services.AddEndpointsApiExplorer();
//產生Swagger文件:/swagger/v1/swagger.json
builder.Services.AddSwaggerGen();

// 註冊 IP 卡控 Middleware (自定義實作) <- 移到 Build 之前
builder.Services.AddTransient<IpAllowlistMiddleware>();

// 注入資料庫服務
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注入Web API Log服務
builder.Services.Configure<WebApiRequestLogOptions>(
    builder.Configuration.GetSection("WebApiRequestLog"));
builder.Services.AddScoped<IWebApiRequestLogRepository, WebApiRequestLogRepository>();
builder.Services.AddScoped<WebApiRequestLogMiddleware>();

// 批次寫入LOG
builder.Services.AddSingleton(new WebApiRequestLogQueue(capacity: 5000));
builder.Services.AddHostedService<WebApiRequestLogWriterService>();

var app = builder.Build();

// 啟用 Forwarded Headers
// 必須放在其他 Middleware（如 Authentication, StaticFiles）之前
app.UseForwardedHeaders();

app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 400 ? LogEventLevel.Warning
        : LogEventLevel.Information;

    opts.EnrichDiagnosticContext = (diag, ctx) =>
    {
        // 優先使用 X-Request-Id header，若不存在或為空則使用 TraceIdentifier
        string requestId = RequestHelper.GetRequestId(ctx);
        diag.Set("RequestId", requestId);

        var activity = ctx.Features.Get<IHttpActivityFeature>()?.Activity ?? Activity.Current;
        diag.Set("TraceId", activity?.Id);

        var ipCfg = ctx.RequestServices.GetRequiredService<IOptions<IpAllowlistOptions>>().Value;
        diag.Set("ClientIp", ClientIpResolver.GetClientIp(ctx, ipCfg));

        diag.Set("Path", ctx.Request?.Path.Value);
        diag.Set("Method", ctx.Request?.Method);
        diag.Set("StatusCode", ctx.Response.StatusCode);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Web API Log Middleware: 一定要在全域例外處理之前
app.UseMiddleware<WebApiRequestLogMiddleware>();

// 全域例外處理
app.UseExceptionHandler();

// 使用已註冊的 IMiddleware 實例
app.UseMiddleware<IpAllowlistMiddleware>();

// 限流
app.UseRateLimiter();

app.MapControllers();

app.Run();