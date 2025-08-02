using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;

namespace PostOffice.Configuration;

public static class PostOffice
{
    public static PostOfficeBuilder AddPostOffice(this IServiceCollection services)
    {
        // Register the standard Poster
        services.AddTransient<Poster>();

        // Register middleware pipeline
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
