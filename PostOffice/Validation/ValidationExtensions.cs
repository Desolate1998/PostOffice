using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Middleware;

namespace PostOffice.Validation;

/// <summary>
/// Validation extensions for PostOffice
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds validation middleware using FluentValidation
    /// </summary>
    public static PostOfficeBuilder AddValidation(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ValidationMiddleware<,>));
        return builder;
    }
} 