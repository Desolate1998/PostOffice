namespace PostOffice;

public class PostageWrapper<TMail, TResponse> : IPostageWrapper
    where TMail : IMail<TResponse>
{
    private readonly IPostageMiddleware<TMail, TResponse> _middleware;

    public PostageWrapper(IPostageMiddleware<TMail, TResponse> middleware)
    {
        _middleware = middleware;
    }

    public async Task<(bool handled, object? result)> Stamp(object mail, Func<object, Task<object>> next)
    {
        var (handled, result) = await _middleware.StampAsync(
            (TMail)mail,
            async (m) => (TResponse)(await next(m))!);

        return (handled, result);
    }
}
