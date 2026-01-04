public class WebApiRequestLog
{
    public long Id { get; set; }

    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public DateTime RequestDateTime { get; set; }

    public string? RequestHeader { get; set; }
    public string? RequestBody { get; set; }

    public int ResponseHttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }

    public int DurationMs { get; set; }
}
