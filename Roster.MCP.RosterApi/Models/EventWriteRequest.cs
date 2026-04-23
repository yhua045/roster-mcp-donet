using System.Text.Json.Serialization;

namespace Roster.MCP.RosterApi.Models;

/// <summary>Request body for POST /api/events and PUT /api/events/{id}.</summary>
public sealed class EventWriteRequest
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("serviceInfo")]
    public ServiceInfoWriteRequest? ServiceInfo { get; set; }

    [JsonPropertyName("members")]
    public List<MemberWriteRequest> Members { get; set; } = [];
}

public sealed class ServiceInfoWriteRequest
{
    [JsonPropertyName("footnote")]
    public string? Footnote { get; set; }

    [JsonPropertyName("skipService")]
    public bool SkipService { get; set; }

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; set; }
}

public sealed class MemberWriteRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("personId")]
    public int PersonId { get; set; }
}
