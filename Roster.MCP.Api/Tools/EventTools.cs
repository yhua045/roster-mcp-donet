using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Roster.MCP.Api.Validation;
using Roster.MCP.RosterApi.Abstractions;
using Roster.MCP.RosterApi.Models;

namespace Roster.MCP.Api.Tools;

[McpServerToolType]
public sealed class EventTools(IRosterApiClient rosterApiClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [McpServerTool(Name = "list_events")]
    [Description("List roster events. Optionally filter by category (chinese / english / sundayschool) and date range (YYYY-MM-DD).")]
    public async Task<string> ListEvents(
        [Description("Service category: chinese, english, or sundayschool (case-insensitive).")] string? category = null,
        [Description("Start date in YYYY-MM-DD format (inclusive).")] string? from = null,
        [Description("End date in YYYY-MM-DD format (inclusive).")] string? to = null,
        CancellationToken cancellationToken = default)
    {
        var catError = InputValidator.ValidateCategory(category);
        if (catError is not null) return ValidationError(catError);

        var fromError = InputValidator.ValidateDate(from, "from");
        if (fromError is not null) return ValidationError(fromError);

        var toError = InputValidator.ValidateDate(to, "to");
        if (toError is not null) return ValidationError(toError);

        if (from is not null && to is not null)
        {
            var rangeError = InputValidator.ValidateDateRange(from, to);
            if (rangeError is not null) return ValidationError(rangeError);
        }

        return await rosterApiClient.ListEventsAsync(
            category?.ToLowerInvariant(),
            from,
            to,
            cancellationToken);
    }

    [McpServerTool(Name = "create_event")]
    [Description("Create a new roster event. Requires date and category; members and serviceInfo are optional.")]
    public async Task<string> CreateEvent(
        [Description("Event date in YYYY-MM-DD format.")] string date,
        [Description("Service category: chinese, english, or sundayschool.")] string category,
        [Description("Optional service metadata (footnote, skipService, skipReason).")] ServiceInfoWriteRequest? serviceInfo = null,
        [Description("List of members assigned to the event.")] List<MemberWriteRequest>? members = null,
        CancellationToken cancellationToken = default)
    {
        var fieldError = InputValidator.ValidateWriteRequest(date, category);
        if (fieldError is not null) return ValidationError(fieldError);

        var dateError = InputValidator.ValidateDate(date, "date");
        if (dateError is not null) return ValidationError(dateError);

        var catError = InputValidator.ValidateCategory(category);
        if (catError is not null) return ValidationError(catError);

        var request = new EventWriteRequest
        {
            Date = date,
            Category = category.ToLowerInvariant(),
            ServiceInfo = serviceInfo,
            Members = members ?? [],
        };

        return await rosterApiClient.CreateEventAsync(request, cancellationToken);
    }

    [McpServerTool(Name = "update_event")]
    [Description("Update an existing roster event by ID. Requires id, date, and category.")]
    public async Task<string> UpdateEvent(
        [Description("Numeric ID of the event to update.")] int id,
        [Description("Event date in YYYY-MM-DD format.")] string date,
        [Description("Service category: chinese, english, or sundayschool.")] string category,
        [Description("Optional service metadata (footnote, skipService, skipReason).")] ServiceInfoWriteRequest? serviceInfo = null,
        [Description("List of members assigned to the event.")] List<MemberWriteRequest>? members = null,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return ValidationError("Field 'id' must be a positive integer.");

        var fieldError = InputValidator.ValidateWriteRequest(date, category);
        if (fieldError is not null) return ValidationError(fieldError);

        var dateError = InputValidator.ValidateDate(date, "date");
        if (dateError is not null) return ValidationError(dateError);

        var catError = InputValidator.ValidateCategory(category);
        if (catError is not null) return ValidationError(catError);

        var request = new EventWriteRequest
        {
            Date = date,
            Category = category.ToLowerInvariant(),
            ServiceInfo = serviceInfo,
            Members = members ?? [],
        };

        return await rosterApiClient.UpdateEventAsync(id, request, cancellationToken);
    }

    private static string ValidationError(string message) =>
        $$"""{"result":"Error","error":{"code":"invalid_input","message":"{{JsonEncodedText.Encode(message)}}"},"data":null}""";
}
