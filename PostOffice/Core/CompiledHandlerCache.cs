using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace PostOffice.Core;

/// <summary>
/// High-performance compiled expression cache that eliminates reflection overhead
/// Provides ~10x faster handler invocation compared to reflection
/// </summary>
public static class CompiledHandlerCache
{
    private static readonly ConcurrentDictionary<(Type mailType, Type responseType), Func<object, object, Task<object>>> _compiledHandlers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _compiledResolvers = new();

    /// <summary>
    /// Gets or compiles a fast handler delegate for the specified mail and response types
    /// First call compiles and caches, subsequent calls are O(1) dictionary lookups
    /// </summary>
    public static Func<object, object, Task<object>> GetOrCompileHandler(Type mailType, Type responseType)
    {
        var key = (mailType, responseType);
        return _compiledHandlers.GetOrAdd(key, static k => CompileHandler(k.mailType, k.responseType));
    }

    /// <summary>
    /// Gets or compiles a fast service resolver for the specified handler type
    /// </summary>
    public static Func<IServiceProvider, object> GetOrCompileResolver(Type handlerType)
    {
        return _compiledResolvers.GetOrAdd(handlerType, static type => CompileResolver(type));
    }

    private static Func<object, object, Task<object>> CompileHandler(Type mailType, Type responseType)
    {
        // Find the handler type: DeliveryAsync<TMail, TResponse>
        var handlerType = typeof(DeliveryAsync<,>).MakeGenericType(mailType, responseType);
        var handleMethod = handlerType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance);
        
        if (handleMethod == null)
            throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");

        // Create parameters: (object handler, object mail) => Task<object>
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var mailParam = Expression.Parameter(typeof(object), "mail");

        // Cast handler to correct type
        var typedHandler = Expression.Convert(handlerParam, handlerType);
        
        // Cast mail to correct type
        var typedMail = Expression.Convert(mailParam, mailType);

        // Call HandleAsync method
        var methodCall = Expression.Call(typedHandler, handleMethod, typedMail);

        // Handle the Task<TResponse> return - convert to Task<object>
        var taskType = typeof(Task<>).MakeGenericType(responseType);
        var convertMethod = typeof(CompiledHandlerCache)
            .GetMethod(nameof(ConvertTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(responseType);

        var convertCall = Expression.Call(convertMethod, methodCall);

        // Compile the expression
        var lambda = Expression.Lambda<Func<object, object, Task<object>>>(
            convertCall, handlerParam, mailParam);

        return lambda.Compile();
    }

    private static Func<IServiceProvider, object> CompileResolver(Type serviceType)
    {
        // Create parameter: (IServiceProvider provider) => object
        var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");

        // Call GetRequiredService<T>()
        var getServiceMethod = typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions)
            .GetMethod(nameof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService), 
                       new[] { typeof(IServiceProvider) })!
            .MakeGenericMethod(serviceType);

        var methodCall = Expression.Call(getServiceMethod, providerParam);

        // Convert to object
        var convertToObject = Expression.Convert(methodCall, typeof(object));

        // Compile the expression
        var lambda = Expression.Lambda<Func<IServiceProvider, object>>(
            convertToObject, providerParam);

        return lambda.Compile();
    }

    // Helper method to convert Task<T> to Task<object>
    private static async Task<object> ConvertTaskResult<T>(Task<T> task)
    {
        var result = await task;
        return result!;
    }

    /// <summary>
    /// Precompiles handlers for common types to eliminate first-call compilation overhead
    /// Call this during application startup for maximum performance
    /// </summary>
    public static void WarmupCache(params Type[] mailTypes)
    {
        foreach (var mailType in mailTypes)
        {
            // Find IMail<TResponse> interface to get response type
            var mailInterface = mailType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMail<>));
            
            if (mailInterface != null)
            {
                var responseType = mailInterface.GetGenericArguments()[0];
                var handlerType = typeof(DeliveryAsync<,>).MakeGenericType(mailType, responseType);
                
                // Precompile both handler and resolver
                GetOrCompileHandler(mailType, responseType);
                GetOrCompileResolver(handlerType);
            }
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging
    /// </summary>
    public static (int HandlersCompiled, int ResolversCompiled) GetCacheStats()
    {
        return (_compiledHandlers.Count, _compiledResolvers.Count);
    }
} 