using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Roster.MCP.RosterApi.Abstractions;
using Roster.MCP.RosterApi.Models;

namespace Roster.MCP.RosterApi;

public sealed class RosterApiClient(HttpClient httpClient, ILogger<RosterApiClient> logger) : IRosterApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<string> ListEventsAsync(string? category, string? from, string? to, CancellationToken ct = default)
    {
        var query = BuildQuery([
            ("category", category),
            ("from", from),
            ("to", to),
        ]);
        var url = $"/api/events{query}";

        logger.LogInformation("Downstream GET {Url}", url);
        return await SendAsync(HttpMethod.Get, url, body: null, ct);
    }

    public async Task<string> CreateEventAsync(EventWriteRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Downstream POST /api/events");
        return await SendAsync(HttpMethod.Post, "/api/events", request, ct);
    }

    public async Task<string> UpdateEventAsync(int id, EventWriteRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Downstream PUT /api/events/{Id}", id);
        return await SendAsync(HttpMethod.Put, $"/api/events/{id}", request, ct);
    }

    private async Task<string> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        try
        {
            using var message = new HttpRequestMessage(method, url);
            if (body is not null)
            {
                message.Content = new StringContent(
                    JsonSerializer.Serialize(body, JsonOptions),
                    Encoding.UTF8,
                    "application/json");
            }

            using var response = await httpClient.SendAsync(message, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogInformation("Downstream {StatusCode} {Url} ResponseLength={Len}", (int)response.StatusCode, url, rawBody.Length);
            return rawBody;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Downstream request timed out for {Url}", url);
            return AdapterError("downstream_timeout", "The downstream Roster API did not respond in time.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Downstream request failed for {Url}", url);
            return AdapterError("downstream_unavailable", "The downstream Roster API is unavailable.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error calling downstream {Url}", url);
            return AdapterError("downstream_invalid_response", "An unexpected error occurred contacting the downstream Roster API.");
        }
    }

    private static string BuildQuery(IEnumerable<(string Key, string? Value)> parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null)
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");

        var joined = string.Join("&", parts);
        return joined.Length > 0 ? $"?{joined}" : string.Empty;
    }

    internal static string AdapterError(string code, string message) =>
        $$"""{"result":"Error","error":{"code":"{{code}}","message":"{{message}}"},"data":null}""";
}
