using System.ComponentModel.DataAnnotations;

public sealed class IpAllowlistOptions
{
    public const string SectionName = "IpAllowlist";

    public IpAllowlistMode Mode { get; init; } = IpAllowlistMode.SourceIpOnly;

    /// <summary>
    /// Only used when Mode = ProxyAndXForwardedFor.
    /// Trust X-Forwarded-For ONLY if RemoteIpAddress is one of these proxies.
    /// </summary>
    public string[] TrustedProxies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Allowed client IPs (string match). Extend to support CIDR if needed.
    /// </summary>
    [MinLength(1)]
    public string[] Allow { get; init; } = Array.Empty<string>();
}

public enum IpAllowlistMode
{
    SourceIpOnly = 0,
    ProxyAndXForwardedFor = 1
}
