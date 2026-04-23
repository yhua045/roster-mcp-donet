using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Roster.MCP.RosterApi;
using Roster.MCP.RosterApi.Models;

namespace Roster.MCP.Api.Tests.RosterApi;

public class RosterApiClientTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (RosterApiClient client, FakeHttpMessageHandler handler) CreateClient(
        HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://roster.efcsydney.org") };
        var client = new RosterApiClient(http, NullLogger<RosterApiClient>.Instance);
        return (client, handler);
    }

    // ── list_events query string ───────────────────────────────────────────────

    [Fact]
    public async Task ListEventsAsync_WithAllParams_BuildsCorrectQueryString()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"result":"OK","error":{"message":""},"data":[]}""");

        await client.ListEventsAsync("chinese", "2024-01-01", "2024-01-31");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        var query = handler.LastRequest.RequestUri!.Query;
        Assert.Contains("category=chinese", query);
        Assert.Contains("from=2024-01-01", query);
        Assert.Contains("to=2024-01-31", query);
    }

    [Fact]
    public async Task ListEventsAsync_WithNullParams_BuildsEmptyQueryString()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"result":"OK","error":{"message":""},"data":[]}""");

        await client.ListEventsAsync(null, null, null);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Empty(query);
    }

    [Fact]
    public async Task ListEventsAsync_ReturnsRawResponseBody()
    {
        const string expected = """{"result":"OK","error":{"message":""},"data":[]}""";
        var (client, _) = CreateClient(HttpStatusCode.OK, expected);

        var result = await client.ListEventsAsync(null, null, null);

        Assert.Equal(expected, result);
    }

    // ── create_event request body ─────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_SendsPostWithSerializedBody()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"result":"OK","error":{"message":""},"data":{"id":1}}""");
        var request = new EventWriteRequest
        {
            Date = "2024-01-21",
            Category = "chinese",
            ServiceInfo = new ServiceInfoWriteRequest { Footnote = "Test", SkipService = false },
            Members = [new MemberWriteRequest { Role = "證道", Name = "Pastor Chen", PersonId = 42 }],
        };

        await client.CreateEventAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        var sentBody = handler.LastRequestBody!;
        using var doc = JsonDocument.Parse(sentBody);
        Assert.Equal("2024-01-21", doc.RootElement.GetProperty("date").GetString());
        Assert.Equal("chinese", doc.RootElement.GetProperty("category").GetString());
    }

    [Fact]
    public async Task CreateEventAsync_TargetsEventsEndpoint()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"result":"OK","error":{"message":""},"data":{}}""");
        await client.CreateEventAsync(new EventWriteRequest { Date = "2024-01-21", Category = "english" });

        Assert.Equal("/api/events", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    // ── update_event request body ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateEventAsync_SendsPutWithIdInPath()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"result":"OK","error":{"message":""},"data":{}}""");
        await client.UpdateEventAsync(42, new EventWriteRequest { Date = "2024-01-21", Category = "english" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal("/api/events/42", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    // ── Error envelope passthrough ─────────────────────────────────────────────

    [Fact]
    public async Task ListEventsAsync_DownstreamReturns400_ReturnsRawErrorBody()
    {
        const string errorBody = """{"result":"Error","error":{"message":"Invalid category"},"data":null}""";
        var (client, _) = CreateClient(HttpStatusCode.BadRequest, errorBody);

        var result = await client.ListEventsAsync("bad", null, null);

        Assert.Equal(errorBody, result);
    }

    // ── Transport failure ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListEventsAsync_HttpRequestException_ReturnsAdapterError()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("connection refused"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://roster.efcsydney.org") };
        var client = new RosterApiClient(http, NullLogger<RosterApiClient>.Instance);

        var result = await client.ListEventsAsync(null, null, null);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal("Error", doc.RootElement.GetProperty("result").GetString());
        Assert.Equal("downstream_unavailable", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ListEventsAsync_Timeout_ReturnsAdapterTimeoutError()
    {
        var handler = new ThrowingHttpMessageHandler(new TaskCanceledException("timeout"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://roster.efcsydney.org") };
        var client = new RosterApiClient(http, NullLogger<RosterApiClient>.Instance);

        var result = await client.ListEventsAsync(null, null, null);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal("downstream_timeout", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }
}

// ── Fakes ─────────────────────────────────────────────────────────────────────

internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer body before the HttpRequestMessage is disposed by the caller.
        if (request.Content is not null)
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        LastRequest = request;
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
        return response;
    }
}

internal sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromException<HttpResponseMessage>(exception);
}
