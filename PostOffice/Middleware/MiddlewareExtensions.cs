using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using PostOffice.Configuration;
using PostOffice.Validation;

namespace PostOffice.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers validation middleware for specific mail types based on the ValidateAttribute
    /// </summary>
    public static PostOfficeBuilder AddValidationBehaviors(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ValidationBehavior<,>));
        return builder;
    }

    /// <summary>
    /// Adds FluentValidation with automatic discovery of validators
    /// </summary>
    public static PostOfficeBuilder AddFluentValidation(this PostOfficeBuilder builder, 
        Action<FluentValidationConfiguration>? configure = null)
    {
        var config = new FluentValidationConfiguration();
        configure?.Invoke(config);

        if (config.AutoDiscoverValidators)
        {
            var assemblies = config.AssembliesToScan ?? AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                builder.AddValidatorsFromAssembly(assembly);
            }
        }

        builder.AddValidationBehaviors();
        return builder;
    }
}

public class FluentValidationConfiguration
{
    public bool AutoDiscoverValidators { get; set; } = true;
    public System.Reflection.Assembly[]? AssembliesToScan { get; set; }
} 