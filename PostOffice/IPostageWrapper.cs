namespace PostOffice;

public interface IPostageWrapper
{
    Task<(bool handled, object? result)> Stamp(object mail, Func<object, Task<object>> next);
}
