using System.Net;
using api;

namespace api.Tests;

public class SsrfGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]        // loopback
    [InlineData("169.254.169.254")]  // cloud metadata endpoint
    [InlineData("10.0.0.5")]         // private
    [InlineData("172.16.4.4")]       // private
    [InlineData("192.168.1.1")]      // private
    [InlineData("100.64.0.1")]       // carrier-grade NAT
    [InlineData("0.0.0.0")]
    public void IsPublicAddress_BlocksInternalIPv4(string ip)
    {
        Assert.False(SsrfGuard.IsPublicAddress(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("::1")]      // loopback
    [InlineData("fe80::1")]  // link-local
    [InlineData("fc00::1")]  // unique local
    public void IsPublicAddress_BlocksInternalIPv6(string ip)
    {
        Assert.False(SsrfGuard.IsPublicAddress(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    public void IsPublicAddress_AllowsPublicIPv4(string ip)
    {
        Assert.True(SsrfGuard.IsPublicAddress(IPAddress.Parse(ip)));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    public void IsSchemeAllowed_AllowsHttpAndHttps(string url)
    {
        Assert.True(SsrfGuard.IsSchemeAllowed(new Uri(url)));
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com")]
    public void IsSchemeAllowed_BlocksOtherSchemes(string url)
    {
        Assert.False(SsrfGuard.IsSchemeAllowed(new Uri(url)));
    }

    [Fact]
    public async Task IsUrlAllowedAsync_BlocksCloudMetadataIpLiteral()
    {
        var allowed = await SsrfGuard.IsUrlAllowedAsync(new Uri("http://169.254.169.254/latest/meta-data/"));
        Assert.False(allowed);
    }

    [Fact]
    public async Task IsUrlAllowedAsync_AllowsPublicIpLiteral()
    {
        var allowed = await SsrfGuard.IsUrlAllowedAsync(new Uri("http://8.8.8.8/"));
        Assert.True(allowed);
    }
}
