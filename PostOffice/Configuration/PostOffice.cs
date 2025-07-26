using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Configuration;

public static class PostOffice
{
    public static PostOfficeBuilder AddPostOffice(this IServiceCollection services)
    {
        services.AddTransient<Poster>();

        // Register middleware pipeline
        services.AddTransient(typeof(IMiddlewarePipeline<,>), typeof(MiddlewarePipeline<,>));

        var handlerTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsAbstract &&
                type.BaseType != null &&
                type.BaseType.IsGenericType &&
                type.BaseType.GetGenericTypeDefinition() == typeof(DeliveryAsync<,>));

        foreach (var handler in handlerTypes)
        {
            var baseType = handler.BaseType!;
            services.AddTransient(baseType, handler);
        }

        return new PostOfficeBuilder(services);
    }

    /// <summary>
    /// Adds PostOffice with validation middleware
    /// </summary>
    public static PostOfficeBuilder AddPostOfficeWithValidation(this IServiceCollection services)
    {
        return services.AddPostOffice().AddValidation();
    }
}
