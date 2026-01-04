public interface IWebApiRequestLogRepository
{
    Task AddAsync(WebApiRequestLog log, CancellationToken ct);

    //批次寫入
    Task AddRangeAsync(IReadOnlyCollection<WebApiRequestLog> logs, CancellationToken ct);

}
