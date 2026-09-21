using System.Net;
using System.Net.Sockets;

namespace renderer.Services;

public sealed class SsrfValidator
{
    public ValueTask ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL is required.", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("URL must be an absolute URI.", nameof(url));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Only http and https URLs are allowed.");
        }

        return ValidateHostAsync(uri.Host, cancellationToken);
    }

    private static async ValueTask ValidateHostAsync(string host, CancellationToken cancellationToken)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Localhost targets are not allowed.");
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

        foreach (var address in addresses)
        {
            if (IsBlockedAddress(address))
            {
                throw new InvalidOperationException($"Blocked private or metadata address: {address}");
            }
        }
    }

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;
        if (address.Equals(IPAddress.Parse("169.254.169.254"))) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            var octet1 = bytes[0];
            var octet2 = bytes[1];

            return octet1 == 10 ||
                   octet1 == 127 ||
                   octet1 == 169 && octet2 == 254 ||
                   octet1 == 172 && octet2 >= 16 && octet2 <= 31 ||
                   octet1 == 192 && octet2 == 168 ||
                   octet1 == 100 && (octet2 >= 64 && octet2 <= 127) ||
                   octet1 == 0 ||
                   octet1 == 255;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IPAddress.IsLoopback(address) ||
                   address.Equals(IPAddress.IPv6None) ||
                   address.IsIPv6LinkLocal ||
                   address.IsIPv6Multicast ||
                   address.IsIPv6SiteLocal;
        }

        return false;
    }
}
