using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ValidationException => HttpStatusCode.BadRequest,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                ConflictException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError,
            };

            context.Response.StatusCode = (int)statusCode;

            var errorResponse = new ErrorResponse(
                exception is ValidationException or NotFoundException or UnauthorizedException or ConflictException
                    ? exception.Message
                    : "An error occurred while processing your request.");

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
            }
            else
            {
                _logger.LogWarning("Application exception: {ExceptionType} - {Message}",
                    exception.GetType().Name, exception.Message);
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
        }
    }

    public class ErrorResponse
    {
        public string Message { get; }
        public DateTimeOffset Timestamp { get; }

        public ErrorResponse(string message)
        {
            Message = message;
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    public class ValidationErrorResponse : ErrorResponse
    {
        public System.Collections.Generic.List<string> ValidationErrors { get; }

        public ValidationErrorResponse(System.Collections.Generic.List<string> validationErrors) : base("Validation failed")
        {
            ValidationErrors = validationErrors;
        }
    }
}