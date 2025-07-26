using PostOffice.Core;

namespace PostOffice.Middleware;

public class MiddlewarePipeline<TMail, TResponse> : IMiddlewarePipeline<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    private readonly IEnumerable<IPostageMiddleware<TMail, TResponse>> _middleware;

    public MiddlewarePipeline(IEnumerable<IPostageMiddleware<TMail, TResponse>> middleware)
    {
        _middleware = middleware;
    }

    public async Task<TResponse> ExecuteAsync(TMail mail, Func<TMail, Task<TResponse>> finalHandler)
    {
        var middlewareArray = _middleware.ToArray();
        
        if (middlewareArray.Length == 0)
        {
            return await finalHandler(mail);
        }
        
        // Build the pipeline from right to left (last middleware first)
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
                // If not handled, continue to next in pipeline
                return await nextPipeline(mail);
            };
        }

        return await pipeline(mail);
    }
} 