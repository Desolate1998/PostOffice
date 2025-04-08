namespace PostOffice;

public interface IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
{
    Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next);
}
