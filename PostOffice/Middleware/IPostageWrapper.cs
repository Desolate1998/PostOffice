namespace PostOffice.Middleware;

public interface IPostageWrapper
{
    Task<(bool handled, object? result)> Stamp(object mail, Func<object, Task<object>> next);
}
