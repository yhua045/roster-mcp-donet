using Roster.MCP.RosterApi.Models;

namespace Roster.MCP.RosterApi.Abstractions;

/// <summary>
/// Thin wrapper over the downstream Roster HTTP API.
/// All methods return the raw JSON response string preserving the downstream envelope.
/// On transport failures an adapter-owned error envelope is returned instead.
/// </summary>
public interface IRosterApiClient
{
    /// <summary>Maps to GET /api/events.</summary>
    Task<string> ListEventsAsync(string? category, string? from, string? to, CancellationToken ct = default);

    /// <summary>Maps to POST /api/events.</summary>
    Task<string> CreateEventAsync(EventWriteRequest request, CancellationToken ct = default);

    /// <summary>Maps to PUT /api/events/{id}.</summary>
    Task<string> UpdateEventAsync(int id, EventWriteRequest request, CancellationToken ct = default);
}
