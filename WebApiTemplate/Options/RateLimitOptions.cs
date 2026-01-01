using System.ComponentModel.DataAnnotations;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Range(1, 10_000)]
    public int PermitLimit { get; init; } = 60;

    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;

    [Range(0, 10_000)]
    public int QueueLimit { get; init; } = 0;
}
