using System.Net;
using System.Net.Http.Json;
using api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace api.Tests;

public class HtmlPdfApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HtmlPdfApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostConvert_ValidHtml_ReturnsPdfResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/convert", new
        {
            html = "<html><body><h1>Hello</h1><p>World</p></body></html>",
            options = new
            {
                pageSize = "A4",
                margins = new { top = 10, right = 10, bottom = 10, left = 10 }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task PostConvert_EmptyHtml_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/convert", new
        {
            html = "",
            options = new { }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostConvert_HtmlAndUrlBothProvided_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/convert", new
        {
            html = "<html></html>",
            url = "https://example.com",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud metadata endpoint
    [InlineData("http://127.0.0.1/")]                        // loopback
    [InlineData("http://10.0.0.5/")]                         // private network
    [InlineData("file:///etc/passwd")]                       // disallowed scheme
    public async Task PostConvert_UrlPointingAtDisallowedDestination_ReturnsBadRequest(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/convert", new { url });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
