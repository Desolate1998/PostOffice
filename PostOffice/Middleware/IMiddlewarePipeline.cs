using PostOffice.Core;

namespace PostOffice.Middleware;

/// <summary>
/// Middleware pipeline for processing mail requests
/// </summary>
public interface IMiddlewarePipeline<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    /// <summary>
    /// Executes the middleware pipeline
    /// </summary>
    Task<TResponse> ExecuteAsync(TMail mail, Func<TMail, Task<TResponse>> finalHandler);
} 