using PostOffice.Core;

namespace PostOffice.Middleware;

public interface IMiddlewarePipeline<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    Task<TResponse> ExecuteAsync(TMail mail, Func<TMail, Task<TResponse>> finalHandler);
} 