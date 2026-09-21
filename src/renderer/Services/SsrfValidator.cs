using System.Net;
using System.Net.Sockets;

namespace renderer.Services;

public sealed class SsrfValidator
{
    private static readonly IPNetwork[] BlockedRanges =
    [
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("224.0.0.0/4"),
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("fc00::/7"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("::ffff:0:0/96"),
    ];

    public async Task<SsrfCheckResult> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return SsrfCheckResult.Reject("Malformed URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return SsrfCheckResult.Reject($"Scheme '{uri.Scheme}' not allowed - only http/https.");
        }

        IPAddress[] resolvedAddresses;
        try
        {
            resolvedAddresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return SsrfCheckResult.Reject($"DNS resolution failed: {ex.Message}");
        }

        if (resolvedAddresses.Length == 0)
        {
            return SsrfCheckResult.Reject("Host did not resolve to any address.");
        }

        foreach (var address in resolvedAddresses)
        {
            var toCheck = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            if (BlockedRanges.Any(range => range.Contains(toCheck)))
            {
                return SsrfCheckResult.Reject(
                    $"Resolved address {toCheck} for host '{uri.Host}' is in a blocked range.");
            }
        }

        return SsrfCheckResult.Allow();
    }
}

public sealed record SsrfCheckResult(bool IsAllowed, string? Reason)
{
    public static SsrfCheckResult Allow() => new(true, null);
    public static SsrfCheckResult Reject(string reason) => new(false, reason);
}
