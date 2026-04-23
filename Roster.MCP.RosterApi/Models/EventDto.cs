using System.Text.Json.Serialization;

namespace Roster.MCP.RosterApi.Models;

public sealed class EventDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("serviceInfo")]
    public ServiceInfoDto? ServiceInfo { get; set; }

    [JsonPropertyName("members")]
    public List<MemberDto> Members { get; set; } = [];
}

public sealed class ServiceInfoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("footnote")]
    public string? Footnote { get; set; }

    [JsonPropertyName("skipService")]
    public bool SkipService { get; set; }

    [JsonPropertyName("skipReason")]
    public string? SkipReason { get; set; }
}

public sealed class MemberDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("personId")]
    public int PersonId { get; set; }

    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
