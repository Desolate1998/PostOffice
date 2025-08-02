using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;
using PostOffice.Validation;

namespace PostOffice.Configuration;

public class PostOfficeBuilder
{
    internal readonly IServiceCollection _services;

    public PostOfficeBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds FluentValidation middleware (throws exceptions on validation failure)
    /// </summary>
    public PostOfficeBuilder AddValidation()
    {
        _services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ValidationMiddleware<,>));
        return this;
    }

    /// <summary>
    /// Adds a custom middleware to the pipeline
    /// </summary>
    public PostOfficeBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : class
    {
        _services.AddTransient(typeof(TMiddleware));
        return this;
    }

    /// <summary>
    /// Adds a custom middleware to the pipeline with factory
    /// </summary>
    public PostOfficeBuilder AddMiddleware<TMiddleware>(Func<IServiceProvider, TMiddleware> factory)
        where TMiddleware : class
    {
        _services.AddTransient(factory);
        return this;
    }

    /// <summary>
    /// Adds validators from the specified assembly
    /// </summary>
    public PostOfficeBuilder AddValidatorsFromAssembly(System.Reflection.Assembly assembly)
    {
        _services.AddValidatorsFromAssembly(assembly);
        return this;
    }

    /// <summary>
    /// Adds validators from the assembly containing the specified type
    /// </summary>
    public PostOfficeBuilder AddValidatorsFromAssemblyContaining<T>()
    {
        _services.AddValidatorsFromAssemblyContaining<T>();
        return this;
    }

    /// <summary>
    /// Adds validators from the assembly containing the specified type
    /// </summary>
    public PostOfficeBuilder AddValidatorsFromAssemblyContaining(Type type)
    {
        _services.AddValidatorsFromAssemblyContaining(type);
        return this;
    }
}