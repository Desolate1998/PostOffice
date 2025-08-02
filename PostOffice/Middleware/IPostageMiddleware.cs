namespace PostOffice.Middleware;

/// <summary>
/// Middleware for processing mail requests
/// </summary>
public interface IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    /// <summary>
    /// Processes the mail request
    /// </summary>
    Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next);
}
