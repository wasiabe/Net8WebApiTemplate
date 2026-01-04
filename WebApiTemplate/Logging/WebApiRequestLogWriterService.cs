
public class WebApiRequestLogWriterService : BackgroundService
{
    private readonly WebApiRequestLogQueue _queue;
    private readonly IServiceProvider _sp;

    // 批次策略（你可調）
    private const int BatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    public WebApiRequestLogWriterService(WebApiRequestLogQueue queue, IServiceProvider sp)
    {
        _queue = queue;
        _sp = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<WebApiRequestLog>(BatchSize);
        var nextFlush = DateTime.UtcNow + FlushInterval;

        await foreach (var log in _queue.DequeueAllAsync(stoppingToken))
        {
            batch.Add(log);

            var timeToFlush = DateTime.UtcNow >= nextFlush;
            var sizeToFlush = batch.Count >= BatchSize;

            if (timeToFlush || sizeToFlush)
            {
                await FlushAsync(batch, stoppingToken);
                batch.Clear();
                nextFlush = DateTime.UtcNow + FlushInterval;
            }
        }

        // graceful shutdown: flush remaining
        if (batch.Count > 0)
        {
            await FlushAsync(batch, stoppingToken);
        }
    }

    private async Task FlushAsync(List<WebApiRequestLog> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;

        try
        {
            using var scope = _sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWebApiRequestLogRepository>();
            await repo.AddRangeAsync(batch, ct);
        }
        catch
        {
            // 企業版通常會：寫入失敗就丟棄 or 寫本機 fallback 檔
            // 這裡先不讓它把 BackgroundService 弄死
        }
    }
}
