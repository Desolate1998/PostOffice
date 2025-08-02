using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Middleware;

namespace PostOffice.Validation;

public static class ValidationExtensions
{
    /// <summary>
    /// Adds validation middleware that automatically validates requests using FluentValidation
    /// Just add validators to your DI container and they get called automatically
    /// </summary>
    public static PostOfficeBuilder AddValidation(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ValidationMiddleware<,>));
        return builder;
    }
} 