public class WebApiRequestLogRepository : IWebApiRequestLogRepository
{
    private readonly AppDbContext _db;

    public WebApiRequestLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(WebApiRequestLog log, CancellationToken ct)
    {
        _db.WebApiRequestLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 批次寫入
    /// </summary>
    /// <param name="logs"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task AddRangeAsync(IReadOnlyCollection<WebApiRequestLog> logs, CancellationToken ct)
    {
        _db.WebApiRequestLogs.AddRange(logs);
        await _db.SaveChangesAsync(ct);
    }

}
