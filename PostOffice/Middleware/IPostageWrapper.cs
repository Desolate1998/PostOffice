namespace PostOffice.Middleware;

/// <summary>
/// Wrapper for middleware that handles untyped mail objects
/// </summary>
public interface IPostageWrapper
{
    /// <summary>
    /// Processes the mail request
    /// </summary>
    Task<(bool handled, object? result)> Stamp(object mail, Func<object, Task<object>> next);
}
