using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Validation;

/// <summary>
/// Clean validation behavior that returns custom responses instead of throwing exceptions
/// </summary>
public class CustomResponseValidationBehavior<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public CustomResponseValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        var failures = await ValidateAsync(mail);
        
        if (failures.Any())
        {
            var errorResponse = CreateErrorResponse(mail, failures);
            return (true, errorResponse);
        }

        return (false, default(TResponse));
    }

    private async Task<IEnumerable<FluentValidation.Results.ValidationFailure>> ValidateAsync(TMail mail)
    {
        var validators = _serviceProvider.GetServices<IValidator<TMail>>();
        
        if (!validators.Any()) 
            return Enumerable.Empty<FluentValidation.Results.ValidationFailure>();

        var validationTasks = validators.Select(v => v.ValidateAsync(mail));
        var validationResults = await Task.WhenAll(validationTasks);
        
        return validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null);
    }

    private TResponse CreateErrorResponse(TMail mail, IEnumerable<FluentValidation.Results.ValidationFailure> failures)
    {
        // Try custom handler on the mail itself
        if (mail is IValidationErrorResponse<TResponse> customHandler)
        {
            return customHandler.FromValidationFailures(failures);
        }

        // Try registered handlers
        var handler = _serviceProvider.GetService<IValidationResultHandler<TResponse>>();
        if (handler?.CanHandle(typeof(TResponse)) == true)
        {
            return handler.CreateErrorResponse(failures);
        }

        // Fallback to exception for unsupported types
        throw new ValidationException(failures.ToList());
    }
} 