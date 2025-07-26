using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Middleware;

namespace PostOffice.Validation;

public static class ValidationExtensions
{
    /// <summary>
    /// Adds custom response validation with automatic handler registration
    /// </summary>
    public static PostOfficeBuilder AddCustomResponseValidation(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(CustomResponseValidationBehavior<,>));
        
        // Register default handlers
        builder._services.AddTransient<IValidationResultHandler<string>, StringValidationResultHandler>();
        builder._services.AddTransient(typeof(IValidationResultHandler<>), typeof(GenericValidationResultHandler<>));
        
        return builder;
    }

    /// <summary>
    /// Adds a custom validation result handler
    /// </summary>
    public static PostOfficeBuilder AddValidationResultHandler<TResponse>(
        this PostOfficeBuilder builder, 
        IValidationResultHandler<TResponse> handler)
    {
        builder._services.AddSingleton(handler);
        return builder;
    }

    /// <summary>
    /// Adds a custom validation result handler with factory
    /// </summary>
    public static PostOfficeBuilder AddValidationResultHandler<THandler, TResponse>(
        this PostOfficeBuilder builder)
        where THandler : class, IValidationResultHandler<TResponse>
    {
        builder._services.AddTransient<IValidationResultHandler<TResponse>, THandler>();
        return builder;
    }
} 