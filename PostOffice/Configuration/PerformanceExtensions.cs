using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;
using PostOffice.Validation;

namespace PostOffice.Configuration;

/// <summary>
/// Performance features for PostOffice
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>
    /// Adds performance logging that tracks request timing
    /// </summary>
    public static PostOfficeBuilder AddPerformanceLogging(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(PerformanceLoggingMiddleware<,>));
        return builder;
    }

    /// <summary>
    /// Adds validation performance logging
    /// </summary>
    public static PostOfficeBuilder AddValidationPerformanceLogging(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ValidationPerformanceMiddleware<,>));
        return builder;
    }
}