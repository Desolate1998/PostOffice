using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Validation;

/// <summary>
/// Alternative validation approach that directly validates requests
/// </summary>
public class ValidationBehavior<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        await ValidateAsync(mail);
        
        // Validation passed, continue to next middleware
        return (false, default(TResponse));
    }

    private async Task ValidateAsync(TMail mail)
    {
        var validators = _serviceProvider.GetServices<IValidator<TMail>>();
        
        if (validators.Any())
        {
            var validationTasks = validators.Select(v => v.ValidateAsync(mail));
            var validationResults = await Task.WhenAll(validationTasks);
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }
    }
} 