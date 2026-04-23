namespace Roster.MCP.RosterApi.Options;

public sealed class RosterApiOptions
{
    public const string SectionName = "RosterApi";

    public string BaseUrl { get; set; } = "https://roster.efcsydney.org";
    public int ReadTimeoutSeconds { get; set; } = 10;
    public int WriteTimeoutSeconds { get; set; } = 20;
    public int MaxRetries { get; set; } = 2;
}
