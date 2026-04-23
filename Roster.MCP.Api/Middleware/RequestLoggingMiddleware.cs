using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            string body = string.Empty;
            if (context.Request.ContentLength > 0)
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            _logger.LogInformation("HTTP {Method} {Path} Body: {Body}", context.Request.Method, context.Request.Path, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request body for logging");
        }

        await _next(context);
    }
}
