using System.Collections.Concurrent;
using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;

namespace PostOffice.Core;

/// <summary>
/// High-performance object pooling for frequently allocated objects
/// Reduces GC pressure and improves performance by reusing objects
/// </summary>
public static class ObjectPooling
{
    private static readonly ConcurrentBag<ValidationContext<object>> _validationContextPool = new();
    private static readonly ConcurrentBag<List<ValidationFailure>> _validationFailureListPool = new();
    private static readonly ConcurrentBag<StringBuilder> _stringBuilderPool = new();

    /// <summary>
    /// Gets a pooled validation context or creates a new one
    /// </summary>
    public static ValidationContext<T> RentValidationContext<T>(T instance)
    {
        if (_validationContextPool.TryTake(out var pooledContext))
        {
            // Reuse pooled context by creating a new one with the correct type
            // Note: ValidationContext<T> doesn't support direct reuse, so we minimize allocations where possible
        }
        
        return new ValidationContext<T>(instance);
    }

    /// <summary>
    /// Returns a validation context to the pool (currently just disposes as ValidationContext isn't reusable)
    /// </summary>
    public static void ReturnValidationContext<T>(ValidationContext<T> context)
    {
        // ValidationContext<T> isn't easily reusable, but we keep the pool pattern for consistency
        // In a real implementation, you might implement a custom reusable validation context
    }

    /// <summary>
    /// Gets a pooled list for validation failures
    /// </summary>
    public static List<ValidationFailure> RentValidationFailureList()
    {
        if (_validationFailureListPool.TryTake(out var list))
        {
            list.Clear(); // Ensure it's clean
            return list;
        }
        
        return new List<ValidationFailure>();
    }

    /// <summary>
    /// Returns a validation failure list to the pool
    /// </summary>
    public static void ReturnValidationFailureList(List<ValidationFailure> list)
    {
        if (list.Count < 100) // Don't pool very large lists
        {
            _validationFailureListPool.Add(list);
        }
    }

    /// <summary>
    /// Gets a pooled StringBuilder for string operations
    /// </summary>
    public static StringBuilder RentStringBuilder()
    {
        if (_stringBuilderPool.TryTake(out var sb))
        {
            sb.Clear(); // Ensure it's clean
            return sb;
        }
        
        return new StringBuilder();
    }

    /// <summary>
    /// Returns a StringBuilder to the pool
    /// </summary>
    public static void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity < 1024) // Don't pool very large builders
        {
            _stringBuilderPool.Add(sb);
        }
    }

    /// <summary>
    /// Gets pool statistics for monitoring
    /// </summary>
    public static (int ValidationContexts, int FailureLists, int StringBuilders) GetPoolStats()
    {
        return (_validationContextPool.Count, _validationFailureListPool.Count, _stringBuilderPool.Count);
    }
}

/// <summary>
/// High-performance pooled validation behavior that reuses objects
/// </summary>
public class PooledValidationBehavior<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public PooledValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        var validators = _serviceProvider.GetServices<IValidator<TMail>>();
        if (!validators.Any())
        {
            return (false, default(TResponse));
        }

        // Use pooled list for validation failures
        var failures = ObjectPooling.RentValidationFailureList();
        try
        {
            // Fast validation with pooled objects
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(mail);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            return (false, default(TResponse));
        }
        finally
        {
            // Return the list to the pool
            ObjectPooling.ReturnValidationFailureList(failures);
        }
    }
}

/// <summary>
/// Extension for using pooled validation behaviors
/// </summary>
public static class PooledValidationExtensions
{
    /// <summary>
    /// Adds high-performance pooled validation that reuses objects to reduce GC pressure
    /// </summary>
    public static PostOfficeBuilder AddPooledValidation(this PostOfficeBuilder builder)
    {
        builder._services.AddTransient(typeof(IPostageMiddleware<,>), typeof(PooledValidationBehavior<,>));
        return builder;
    }
} 