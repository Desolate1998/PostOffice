using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Validation;

/// <summary>
/// Simple validation middleware that automatically validates requests using FluentValidation
/// Works exactly like MediatR + FluentValidation - just add validators and they get called automatically
/// </summary>
public class ValidationMiddleware<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationMiddleware(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        // Get all validators for this mail type
        var validators = _serviceProvider.GetServices<IValidator<TMail>>();
        
        if (validators.Any())
        {
            // Run all validators
            var validationTasks = validators.Select(v => v.ValidateAsync(mail));
            var validationResults = await Task.WhenAll(validationTasks);
            
            // Collect all validation failures
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count > 0)
            {
                // Validation failed - throw exception
                throw new ValidationException(failures);
            }
        }

        // Validation passed (or no validators found) - continue to next middleware
        return (false, default(TResponse));
    }
} 