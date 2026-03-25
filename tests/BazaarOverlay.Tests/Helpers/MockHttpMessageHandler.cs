using System.Net;

namespace BazaarOverlay.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _defaultResponse;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string defaultResponse = "", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _defaultResponse = defaultResponse;
        _statusCode = statusCode;
    }

    public MockHttpMessageHandler WithResponse(string urlContains, string response)
    {
        _responses[urlContains] = response;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";
        var content = _responses.FirstOrDefault(kvp => url.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)).Value
                      ?? _defaultResponse;

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(content)
        });
    }
}
