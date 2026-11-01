using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventEase.Tests;

// Runs the real app in memory and requests pages over HTTP.
public class RoutingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/events")]
    [InlineData("/events/1")]
    [InlineData("/events/1/register")]
    public async Task ValidPages_ReturnOk(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/events/9999")]           // event does not exist
    [InlineData("/events/0")]
    [InlineData("/events/-5")]
    [InlineData("/events/9999/register")]
    [InlineData("/events/abc")]            // not a number
    [InlineData("/events/1/unknown")]      // unknown sub-page
    [InlineData("/no-such-page")]
    public async Task InvalidPaths_ReturnNotFoundPage(string path)
    {
        var response = await _client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", html);
        Assert.Contains("href=\"events\"", html);
    }
}
