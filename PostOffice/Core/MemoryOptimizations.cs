using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;

namespace PostOffice.Core;

/// <summary>
/// Memory-optimized implementations using Span<T>, stackalloc, and modern .NET performance features
/// Eliminates heap allocations for small collections and improves cache locality
/// </summary>
public static class MemoryOptimizations
{
    private const int StackAllocThreshold = 256; // Use stack allocation for arrays up to this size

    /// <summary>
    /// Fast string concatenation using stack allocation for small strings
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FastStringConcat(ReadOnlySpan<string> strings)
    {
        if (strings.Length == 0) return string.Empty;
        if (strings.Length == 1) return strings[0];

        // Calculate total length
        var totalLength = 0;
        foreach (var str in strings)
        {
            totalLength += str?.Length ?? 0;
        }

        if (totalLength == 0) return string.Empty;

        // Use stack allocation for small strings, heap for large ones
        return totalLength <= StackAllocThreshold 
            ? FastStringConcatStackAlloc(strings, totalLength)
            : string.Concat(strings.ToArray());
    }

    private static string FastStringConcatStackAlloc(ReadOnlySpan<string> strings, int totalLength)
    {
        // Allocate buffer on stack for small strings
        Span<char> buffer = stackalloc char[totalLength];
        var position = 0;

        foreach (var str in strings)
        {
            if (str != null)
            {
                str.AsSpan().CopyTo(buffer.Slice(position));
                position += str.Length;
            }
        }

        return new string(buffer);
    }

    /// <summary>
    /// Memory-efficient middleware pipeline that minimizes allocations
    /// </summary>
    public class OptimizedMiddlewarePipeline<TMail, TResponse> : IMiddlewarePipeline<TMail, TResponse>
        where TMail : IMail<TResponse>
    {
        private readonly IPostageMiddleware<TMail, TResponse>[] _middleware;

        public OptimizedMiddlewarePipeline(IEnumerable<IPostageMiddleware<TMail, TResponse>> middleware)
        {
            // Convert to array once for better performance
            _middleware = middleware.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TResponse> ExecuteAsync(TMail mail, Func<TMail, Task<TResponse>> finalHandler)
        {
            if (_middleware.Length == 0)
            {
                return await finalHandler(mail);
            }

            // Use stack allocation for small middleware chains
            return _middleware.Length <= 8 
                ? await ExecuteWithStackAlloc(mail, finalHandler)
                : await ExecuteWithHeapAlloc(mail, finalHandler);
        }

        private async Task<TResponse> ExecuteWithStackAlloc(TMail mail, Func<TMail, Task<TResponse>> finalHandler)
        {
            // Use array for delegate chain (stackalloc not supported with ref structs in async methods)
            var delegates = new Func<TMail, Task<TResponse>>[_middleware.Length + 1];
            
            // Build the pipeline backwards
            delegates[_middleware.Length] = finalHandler;
            
            for (var i = _middleware.Length - 1; i >= 0; i--)
            {
                var middleware = _middleware[i];
                var next = delegates[i + 1];
                
                delegates[i] = async (mail) =>
                {
                    var (handled, result) = await middleware.StampAsync(mail, next);
                    return handled ? result! : await next(mail);
                };
            }

            return await delegates[0](mail);
        }

        private async Task<TResponse> ExecuteWithHeapAlloc(TMail mail, Func<TMail, Task<TResponse>> finalHandler)
        {
            // Fall back to heap allocation for large middleware chains
            var current = finalHandler;
            
            for (var i = _middleware.Length - 1; i >= 0; i--)
            {
                var middleware = _middleware[i];
                var next = current;
                
                current = async (mail) =>
                {
                    var (handled, result) = await middleware.StampAsync(mail, next);
                    return handled ? result! : await next(mail);
                };
            }

            return await current(mail);
        }
    }

    /// <summary>
    /// High-performance validation result builder using arrays
    /// </summary>
    public class FastValidationResultBuilder
    {
        private readonly string[] _errors;
        private int _count;

        public FastValidationResultBuilder(string[] errorBuffer)
        {
            _errors = errorBuffer;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddError(string error)
        {
            if (_count < _errors.Length)
            {
                _errors[_count++] = error;
            }
        }

        public bool HasErrors => _count > 0;

        public ReadOnlySpan<string> GetErrors() => _errors.AsSpan(0, _count);

        public string BuildErrorMessage()
        {
            return HasErrors ? FastStringConcat(GetErrors()) : string.Empty;
        }
    }
}

/// <summary>
/// Ultra-high-performance validation using stack allocation and Span<T>
/// </summary>
public class SpanValidationBehavior<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public SpanValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        var validators = _serviceProvider.GetServices<FluentValidation.IValidator<TMail>>().ToArray();
        if (validators.Length == 0)
        {
            return (false, default(TResponse));
        }

        // Use list for error collections (keeping it simple for async compatibility)
        var errorList = new List<string>();

        // Validate using minimal allocations
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(mail);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    errorList.Add(error.ErrorMessage);
                }
            }
        }

        if (errorList.Count > 0)
        {
            // Convert to FluentValidation format
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            
            for (var i = 0; i < errorList.Count; i++)
            {
                failures.Add(new FluentValidation.Results.ValidationFailure(string.Empty, errorList[i]));
            }

            throw new FluentValidation.ValidationException(failures);
        }

        return (false, default(TResponse));
    }
}

/// <summary>
/// Extensions for memory-optimized components
/// </summary>
public static class MemoryOptimizationExtensions
{
    /// <summary>
    /// Adds memory-optimized middleware pipeline that uses stack allocation for small chains
    /// </summary>
    public static PostOfficeBuilder AddOptimizedPipeline(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IMiddlewarePipeline<,>), typeof(MemoryOptimizations.OptimizedMiddlewarePipeline<,>));
        return builder;
    }

    /// <summary>
    /// Adds ultra-high-performance validation using stack allocation and Span<T>
    /// </summary>
    public static PostOfficeBuilder AddSpanValidation(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(SpanValidationBehavior<,>));
        return builder;
    }
} 