using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KYC.API.Http;

/// <summary>
/// Turns unhandled exceptions into RFC 9457 problem-details responses:
/// FluentValidation failures become HTTP 400 with per-field errors, everything else becomes HTTP 500
/// without leaking exception details to the caller.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client went away; there is nobody to answer and nothing worth logging as an error.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;
            return true;
        }

        ProblemDetails problem;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

            problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
        }

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem, problem.GetType(), options: null, contentType: "application/problem+json", cancellationToken);

        return true;
    }
}
