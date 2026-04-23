using System.Text.Json.Serialization;

namespace Roster.MCP.RosterApi.Models;

public sealed class RosterApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}
