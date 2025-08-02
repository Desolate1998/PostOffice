using PostOffice.Core;

namespace PostOffice.Middleware;

/// <summary>
/// Default implementation of the middleware pipeline
/// </summary>
public class MiddlewarePipeline<TMail, TResponse>(IEnumerable<IPostageMiddleware<TMail, TResponse>> middleware) : IMiddlewarePipeline<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IEnumerable<IPostageMiddleware<TMail, TResponse>> _middleware = middleware;

  public async Task<TResponse> ExecuteAsync(TMail mail, Func<TMail, Task<TResponse>> finalHandler)
    {
        var middlewareArray = _middleware.ToArray();
        
        if (middlewareArray.Length == 0)
        {
            return await finalHandler(mail);
        }
        
        Func<TMail, Task<TResponse>> pipeline = finalHandler;
        
        for (int i = middlewareArray.Length - 1; i >= 0; i--)
        {
            var currentMiddleware = middlewareArray[i];
            var nextPipeline = pipeline;
            
            pipeline = async (mail) =>
            {
                var (handled, result) = await currentMiddleware.StampAsync(mail, nextPipeline);
                if (handled)
                {
                    return result!;
                }
                return await nextPipeline(mail);
            };
        }

        return await pipeline(mail);
    }
} 