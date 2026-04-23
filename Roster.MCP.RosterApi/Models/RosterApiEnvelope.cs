using System.Text.Json.Serialization;

namespace Roster.MCP.RosterApi.Models;

public sealed class RosterApiEnvelope<T>
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public RosterApiError? Error { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
