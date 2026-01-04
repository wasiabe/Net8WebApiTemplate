using System.Threading.Channels;

public class WebApiRequestLogQueue
{
    private readonly Channel<WebApiRequestLog> _channel;

    public WebApiRequestLogQueue(int capacity = 5000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // 滿了就丟最舊的，避免 API 卡住
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<WebApiRequestLog>(options);
    }

    public bool TryEnqueue(WebApiRequestLog log) => _channel.Writer.TryWrite(log);

    public IAsyncEnumerable<WebApiRequestLog> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
