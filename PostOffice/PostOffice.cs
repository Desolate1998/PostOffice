using Microsoft.Extensions.DependencyInjection;

namespace PostOffice;

public static class PostOffice
{
    public static IServiceCollection AddPostOffice(this IServiceCollection services)
    {
        services.AddTransient<Poster>();

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

        return services;
    }
}
