using PostOffice.Core;

namespace PostOffice.Middleware;

/// <summary>
/// Wrapper for typed middleware to handle untyped mail objects
/// </summary>
public class PostageWrapper<TMail, TResponse>(IPostageMiddleware<TMail, TResponse> middleware) : IPostageWrapper
    where TMail : IMail<TResponse>
{
    private readonly IPostageMiddleware<TMail, TResponse> _middleware = middleware;

  public async Task<(bool handled, object? result)> Stamp(object mail, Func<object, Task<object>> next)
    {
        var (handled, result) = await _middleware.StampAsync(
            (TMail)mail,
            async (m) => (TResponse)(await next(m))!);

        return (handled, result);
    }
}
