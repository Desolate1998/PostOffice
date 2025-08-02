using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;

namespace PostOffice.Configuration;

/// <summary>
/// Configuration extensions for PostOffice
/// </summary>
public static class PostOffice
{
    /// <summary>
    /// Adds PostOffice services to the service collection
    /// </summary>
    public static PostOfficeBuilder AddPostOffice(this IServiceCollection services)
    {
        services.AddTransient<Poster>();
        services.AddTransient(typeof(IMiddlewarePipeline<,>), typeof(MiddlewarePipeline<,>));

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
