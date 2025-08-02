using Microsoft.Extensions.Logging;

namespace PostOffice.Middleware;

/// <summary>
/// Performance logging middleware
/// </summary>
public class PerformanceLoggingMiddleware<TMail, TResponse>(ILogger<PerformanceLoggingMiddleware<TMail, TResponse>> logger) : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly ILogger<PerformanceLoggingMiddleware<TMail, TResponse>> _logger = logger;
    private readonly string _mailTypeName = typeof(TMail).Name;

  public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("🚀 Starting request: {MailType} at {StartTime}", _mailTypeName, startTime);

            var result = await next(mail);

            stopwatch.Stop();

            _logger.LogInformation("✅ Request completed: {MailType} in {ElapsedMs}ms at {EndTime}",
                _mailTypeName, stopwatch.ElapsedMilliseconds, DateTime.UtcNow);

            return (false, result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "❌ Request failed: {MailType} after {ElapsedMs}ms at {EndTime}",
                _mailTypeName, stopwatch.ElapsedMilliseconds, DateTime.UtcNow);

            throw;
        }
    }
}