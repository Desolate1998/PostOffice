using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Validation;

/// <summary>
/// Performance logging middleware for validation
/// </summary>
public class ValidationPerformanceMiddleware<TMail, TResponse>(
    IServiceProvider serviceProvider,
    ILogger<ValidationPerformanceMiddleware<TMail, TResponse>> logger) : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<ValidationPerformanceMiddleware<TMail, TResponse>> _logger = logger;
    private readonly string _mailTypeName = typeof(TMail).Name;

  public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        var validators = _serviceProvider.GetServices<IValidator<TMail>>();
        
        if (!validators.Any())
        {
            _logger.LogDebug("⏭️ No validators found for {MailType}, skipping validation", _mailTypeName);
            return (false, default(TResponse));
        }

        var validationStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var validatorCount = validators.Count();

        _logger.LogInformation("🔍 Starting validation for {MailType} with {ValidatorCount} validators", 
            _mailTypeName, validatorCount);

        try
        {
            var validationTasks = validators.Select(v => v.ValidateAsync(mail));
            var validationResults = await Task.WhenAll(validationTasks);
            
            validationStopwatch.Stop();
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count > 0)
            {
                _logger.LogWarning("❌ Validation failed for {MailType} after {ElapsedMs}ms with {FailureCount} errors", 
                    _mailTypeName, validationStopwatch.ElapsedMilliseconds, failures.Count);
                
                foreach (var failure in failures)
                {
                    _logger.LogDebug("  - {PropertyName}: {ErrorMessage}", failure.PropertyName, failure.ErrorMessage);
                }
                
                throw new ValidationException(failures);
            }

            _logger.LogInformation("✅ Validation passed for {MailType} in {ElapsedMs}ms", 
                _mailTypeName, validationStopwatch.ElapsedMilliseconds);

            return (false, default(TResponse));
        }
        catch (ValidationException)
        {
            validationStopwatch.Stop();
            _logger.LogError("❌ Validation exception for {MailType} after {ElapsedMs}ms", 
                _mailTypeName, validationStopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            validationStopwatch.Stop();
            _logger.LogError(ex, "💥 Validation error for {MailType} after {ElapsedMs}ms", 
                _mailTypeName, validationStopwatch.ElapsedMilliseconds);
            throw;
        }
    }
} 