using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Validation;

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
        // Try to get validator for the mail type
        var validatorType = typeof(IValidator<>).MakeGenericType(typeof(TMail));
        var validator = _serviceProvider.GetService(validatorType) as IValidator;

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(new ValidationContext<TMail>(mail));
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        // Continue to next middleware or handler
        return (false, default(TResponse));
    }
} 