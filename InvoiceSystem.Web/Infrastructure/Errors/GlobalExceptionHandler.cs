using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Infrastructure.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}", traceId, httpContext.Request.Path);

        // Sprawdzamy, czy żądanie oczekuje JSON/API (Route zaczyna się od /api, żądanie z KSeF lub nagłówek Accept zawiera application/json)
        var acceptsJson = httpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
        var isApiRoute = httpContext.Request.Path.StartsWithSegments("/api") || 
                         httpContext.Request.Path.Value?.Contains("ksef", StringComparison.OrdinalIgnoreCase) == true;

        if (acceptsJson || isApiRoute)
        {
            httpContext.Response.ContentType = "application/problem+json";
            
            var statusCode = exception switch
            {
                FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = exception switch
                {
                    FluentValidation.ValidationException => "Błąd walidacji wejściowej",
                    InvalidOperationException => "Błąd operacji biznesowej",
                    UnauthorizedAccessException => "Brak uprawnień",
                    KeyNotFoundException => "Zasób nie został znaleziony",
                    _ => "Wystąpił nieoczekiwany błąd serwera"
                },
                Detail = exception.Message,
                Instance = httpContext.Request.Path,
                Type = $"https://httpstatuses.io/{statusCode}"
            };

            problemDetails.Extensions["traceId"] = traceId;

            if (exception is FluentValidation.ValidationException valEx)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var group in valEx.Errors.GroupBy(e => e.PropertyName))
                {
                    errors[group.Key] = group.Select(e => e.ErrorMessage).ToArray();
                }
                problemDetails.Extensions["errors"] = errors;
            }

            await JsonSerializer.SerializeAsync(httpContext.Response.Body, problemDetails, cancellationToken: cancellationToken);
            return true; // Wyjątek został w pełni obsłużony i sformatowany jako JSON
        }

        // Zwracamy false dla standardowych żądań stron HTML (MVC),
        // co pozwoli ASP.NET Core przekierować żądanie do kontrolera błędów /error.
        return false;
    }
}
