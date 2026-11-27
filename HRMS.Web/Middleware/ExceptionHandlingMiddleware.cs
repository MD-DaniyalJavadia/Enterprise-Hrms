using System.Net;
using System.Text.Json;
using HRMS.Models.ResponseModels;

namespace HRMS.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            if (IsAjaxRequest(context.Request))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var message = _env.IsDevelopment()
                    ? $"An unexpected error occurred: {ex.Message}"
                    : "An unexpected server error occurred. Please contact support.";

                var payload = JsonSerializer.Serialize(ApiResponse<object>.Fail(message));
                await context.Response.WriteAsync(payload);
            }
            else
            {
                context.Response.Redirect("/Error");
            }
        }
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
               || request.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
    }
}
