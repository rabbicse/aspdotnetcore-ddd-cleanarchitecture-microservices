using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KYC.Application.Behaviors;

/// <summary>
/// Logs the start, end and duration of every MediatR request. Request contents are not logged, because
/// commands carry personal data.
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        LogHandling(logger, name);

        var started = Stopwatch.GetTimestamp();
        var response = await next(cancellationToken);

        LogHandled(logger, name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogHandled(ILogger logger, string requestName, double elapsedMilliseconds);
}
