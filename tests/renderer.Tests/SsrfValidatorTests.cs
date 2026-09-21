using renderer.Services;

namespace renderer.Tests;

public sealed class SsrfValidatorTests
{
    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.5/internal")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://172.16.5.5/")]
    [InlineData("http://127.0.0.1:8080/admin")]
    [InlineData("http://localhost/admin")]
    public async Task ValidateAsync_RejectsPrivateAndMetadataAddresses(string url)
    {
        var validator = new SsrfValidator();

        var result = await validator.ValidateAsync(url);

        Assert.False(result.IsAllowed);
        Assert.NotNull(result.Reason);
    }

    [Theory]
    [InlineData("https://8.8.8.8/")]
    [InlineData("https://1.1.1.1/")]
    [InlineData("https://example.com/report.html")]
    public async Task ValidateAsync_AllowsPublicIpAddresses(string url)
    {
        var validator = new SsrfValidator();

        var result = await validator.ValidateAsync(url);

        Assert.True(result.IsAllowed, result.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/file.html")]
    public async Task ValidateAsync_RejectsMalformedOrUnsupportedUrls(string url)
    {
        var validator = new SsrfValidator();

        var result = await validator.ValidateAsync(url);

        Assert.False(result.IsAllowed);
    }
}