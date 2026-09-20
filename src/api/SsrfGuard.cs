using System.Net;
using System.Net.Sockets;

namespace api;

public sealed class SsrfBlockedException : Exception
{
    public SsrfBlockedException(string message) : base(message)
    {
    }
}

/// <summary>
/// Blocks navigation to internal/cloud-metadata network destinations when a caller supplies a
/// `url` instead of raw HTML. Checked once before a job is queued and again, per request, at
/// fetch time inside the browser context to close the gap redirects/DNS rebinding can open
/// between the two checks.
/// </summary>
public static class SsrfGuard
{
    private static readonly (IPAddress Network, int PrefixLength)[] BlockedIPv4Ranges =
    [
        (IPAddress.Parse("0.0.0.0"), 8),
        (IPAddress.Parse("10.0.0.0"), 8),
        (IPAddress.Parse("100.64.0.0"), 10),
        (IPAddress.Parse("127.0.0.0"), 8),
        (IPAddress.Parse("169.254.0.0"), 16),
        (IPAddress.Parse("172.16.0.0"), 12),
        (IPAddress.Parse("192.0.0.0"), 24),
        (IPAddress.Parse("192.168.0.0"), 16),
        (IPAddress.Parse("198.18.0.0"), 15),
        (IPAddress.Parse("224.0.0.0"), 4),
        (IPAddress.Parse("240.0.0.0"), 4),
    ];

    private static readonly (IPAddress Network, int PrefixLength)[] BlockedIPv6Ranges =
    [
        (IPAddress.Parse("::"), 128),
        (IPAddress.Parse("::1"), 128),
        (IPAddress.Parse("64:ff9b::"), 96),
        (IPAddress.Parse("fc00::"), 7),
        (IPAddress.Parse("fe80::"), 10),
        (IPAddress.Parse("ff00::"), 8),
    ];

    public static bool IsSchemeAllowed(Uri uri) => uri.Scheme is "http" or "https";

    public static async Task<bool> IsUrlAllowedAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (!IsSchemeAllowed(uri))
        {
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var ranges = address.AddressFamily == AddressFamily.InterNetwork
            ? BlockedIPv4Ranges
            : BlockedIPv6Ranges;

        return !ranges.Any(range => IsInRange(address, range.Network, range.PrefixLength));
    }

    private static bool IsInRange(IPAddress address, IPAddress network, int prefixLength)
    {
        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();

        if (addressBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}
