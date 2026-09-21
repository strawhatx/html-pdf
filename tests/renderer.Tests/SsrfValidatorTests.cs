using renderer.Services;

namespace renderer.Tests;

public sealed class SsrfValidatorTests
{
    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.5/internal")]
    [InlineData("http://192.168.1.1/")]
    public async Task ValidateAsync_RejectsPrivateAndMetadataAddresses(string url)
    {
        var validator = new SsrfValidator();

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => validator.ValidateAsync(url).AsTask());
    }

    [Theory]
    [InlineData("https://8.8.8.8/")]
    [InlineData("https://1.1.1.1/")]
    public async Task ValidateAsync_AllowsPublicIpAddresses(string url)
    {
        var validator = new SsrfValidator();

        await validator.ValidateAsync(url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("file:///etc/passwd")]
    public async Task ValidateAsync_RejectsMalformedOrUnsupportedUrls(string url)
    {
        var validator = new SsrfValidator();

        await Assert.ThrowsAnyAsync<Exception>(() => validator.ValidateAsync(url).AsTask());
    }
}