using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http.Resilience;
using Roster.MCP.Api.Tools;
using Roster.MCP.RosterApi;
using Roster.MCP.RosterApi.Abstractions;
using Roster.MCP.RosterApi.Options;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
var rosterApiOptions = builder.Configuration
    .GetSection(RosterApiOptions.SectionName)
    .Get<RosterApiOptions>() ?? new RosterApiOptions();

// ── Downstream Roster API client ─────────────────────────────────────────────
builder.Services
    .AddHttpClient<IRosterApiClient, RosterApiClient>(client =>
    {
        client.BaseAddress = new Uri(rosterApiOptions.BaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddStandardResilienceHandler(opts =>
    {
        opts.Retry.MaxRetryAttempts = rosterApiOptions.MaxRetries;
        opts.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(rosterApiOptions.WriteTimeoutSeconds);
        opts.AttemptTimeout.Timeout = TimeSpan.FromSeconds(rosterApiOptions.ReadTimeoutSeconds);
    });

// ── MCP server ───────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(EventTools).Assembly);

// ── Other services ───────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler("/error");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();

// ── MCP SSE endpoint ─────────────────────────────────────────────────────────
app.MapMcp("/mcp");

// ── Health / utility endpoints ───────────────────────────────────────────────
app.MapGet("/health", () => Results.Ok("Healthy"));

app.MapPost("/api/echo", async (HttpRequest req) =>
{
    req.EnableBuffering();
    using var sr = new StreamReader(req.Body, Encoding.UTF8, leaveOpen: true);
    var body = await sr.ReadToEndAsync();
    req.Body.Position = 0;
    return Results.Content(body, "application/json");
});

app.MapGet("/throw", (HttpContext _) => throw new Exception("fail"));

app.Map("/error", (HttpContext ctx) =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var detail = ex?.Message ?? "An error occurred";
    return Results.Problem(detail: detail, statusCode: 500);
});

app.Run();

public partial class Program { }
