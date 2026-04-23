using System.Text.Json;
using NSubstitute;
using Roster.MCP.Api.Tools;
using Roster.MCP.RosterApi.Abstractions;
using Roster.MCP.RosterApi.Models;

namespace Roster.MCP.Api.Tests.Tools;

public class EventToolsTests
{
    private static readonly string OkEmpty = """{"result":"OK","error":{"message":""},"data":[]}""";

    private static IRosterApiClient FakeClient(string returnValue = "")
    {
        var client = Substitute.For<IRosterApiClient>();
        client.ListEventsAsync(default, default, default, default).ReturnsForAnyArgs(returnValue);
        client.CreateEventAsync(default!, default).ReturnsForAnyArgs(returnValue);
        client.UpdateEventAsync(default, default!, default).ReturnsForAnyArgs(returnValue);
        return client;
    }

    // ── list_events validation ────────────────────────────────────────────────

    [Theory]
    [InlineData("friday")]
    [InlineData("invalid")]
    public async Task ListEvents_InvalidCategory_ReturnsValidationError(string category)
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.ListEvents(category: category);

        AssertValidationError(result);
    }

    [Theory]
    [InlineData("2024/01/14", null)]
    [InlineData(null, "20240114")]
    public async Task ListEvents_InvalidDateFormat_ReturnsValidationError(string? from, string? to)
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.ListEvents(from: from, to: to);

        AssertValidationError(result);
    }

    [Fact]
    public async Task ListEvents_FromAfterTo_ReturnsValidationError()
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.ListEvents(from: "2024-02-01", to: "2024-01-01");

        AssertValidationError(result);
    }

    [Fact]
    public async Task ListEvents_ValidArgs_CallsClientAndReturnsResult()
    {
        var client = Substitute.For<IRosterApiClient>();
        client.ListEventsAsync("chinese", "2024-01-01", "2024-01-31", default)
              .Returns(OkEmpty);
        var tools = new EventTools(client);

        var result = await tools.ListEvents("Chinese", "2024-01-01", "2024-01-31");

        Assert.Equal(OkEmpty, result);
        await client.Received(1).ListEventsAsync("chinese", "2024-01-01", "2024-01-31", default);
    }

    [Fact]
    public async Task ListEvents_CategoryLowercasedBeforePassingToClient()
    {
        var client = Substitute.For<IRosterApiClient>();
        client.ListEventsAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
              .Returns(OkEmpty);
        var tools = new EventTools(client);

        await tools.ListEvents(category: "ENGLISH");

        await client.Received(1).ListEventsAsync(
            "english",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ── create_event validation ───────────────────────────────────────────────

    [Theory]
    [InlineData("", "chinese")]
    [InlineData("2024-01-21", "")]
    public async Task CreateEvent_MissingRequiredField_ReturnsValidationError(string date, string category)
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.CreateEvent(date, category);

        AssertValidationError(result);
    }

    [Fact]
    public async Task CreateEvent_InvalidDateFormat_ReturnsValidationError()
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.CreateEvent("21-01-2024", "chinese");

        AssertValidationError(result);
    }

    [Fact]
    public async Task CreateEvent_InvalidCategory_ReturnsValidationError()
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.CreateEvent("2024-01-21", "invalid");

        AssertValidationError(result);
    }

    [Fact]
    public async Task CreateEvent_ValidArgs_CallsClientWithLowercaseCategory()
    {
        var client = Substitute.For<IRosterApiClient>();
        client.CreateEventAsync(Arg.Any<EventWriteRequest>(), Arg.Any<CancellationToken>())
              .Returns("""{"result":"OK","error":{"message":""},"data":{"id":1}}""");
        var tools = new EventTools(client);

        await tools.CreateEvent("2024-01-21", "Chinese");

        await client.Received(1).CreateEventAsync(
            Arg.Is<EventWriteRequest>(r => r.Category == "chinese"),
            Arg.Any<CancellationToken>());
    }

    // ── update_event validation ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateEvent_IdZeroOrNegative_ReturnsValidationError()
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.UpdateEvent(0, "2024-01-21", "chinese");

        AssertValidationError(result);
    }

    [Fact]
    public async Task UpdateEvent_MissingDate_ReturnsValidationError()
    {
        var tools = new EventTools(FakeClient(OkEmpty));

        var result = await tools.UpdateEvent(1, "", "chinese");

        AssertValidationError(result);
    }

    [Fact]
    public async Task UpdateEvent_ValidArgs_CallsClientWithCorrectId()
    {
        var client = Substitute.For<IRosterApiClient>();
        client.UpdateEventAsync(Arg.Any<int>(), Arg.Any<EventWriteRequest>(), Arg.Any<CancellationToken>())
              .Returns("""{"result":"OK","error":{"message":""},"data":{}}""");
        var tools = new EventTools(client);

        await tools.UpdateEvent(42, "2024-01-21", "english");

        await client.Received(1).UpdateEventAsync(
            42,
            Arg.Is<EventWriteRequest>(r => r.Category == "english"),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AssertValidationError(string result)
    {
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("Error", doc.RootElement.GetProperty("result").GetString());
        Assert.Equal("invalid_input", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }
}
