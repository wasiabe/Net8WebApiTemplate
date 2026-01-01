global using Serilog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog 配置
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 資料庫字串 (從環境變數或 Secret 讀取，避免明碼)
// 範例：export ConnectionStrings__DefaultConnection="YourRealPassword"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 3. Rate Limiting 限流配置
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 註冊自定義錯誤處理器
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 使用準化錯誤回應格式 RFC 9457
builder.Services.AddProblemDetails();

//在 非 Controller 的地方 也能存取目前的 HTTP Request, 例如 Middleware, Service, Logging
builder.Services.AddHttpContextAccessor();  

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IP 卡控 Middleware (自定義實作)
app.UseMiddleware<IpFilteringMiddleware>();

app.UseRateLimiter();
app.UseExceptionHandler(); // 使用 IExceptionHandler
app.MapControllers();

app.Run();