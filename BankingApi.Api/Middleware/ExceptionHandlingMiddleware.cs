using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception: {ErrorCode}", ex.ErrorCode);

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

                var problem = new
                {
                    type = $"https://banking-api.local/errors/{ex.ErrorCode.ToLower()}",
                    title = ex.Message,
                    status = 422,
                    detail = ex.Message,
                    instance = context.Request.Path,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier,
                    timestamp = DateTimeOffset.UtcNow.ToString("o")
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var problem = new
                {
                    type = "https://banking-api.local/errors/internal-server-error",
                    title = "An internal server error occurred",
                    status = 500,
                    detail = "An unexpected error occurred. Please try again later.",
                    instance = context.Request.Path,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier,
                    timestamp = DateTimeOffset.UtcNow.ToString("o")
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
