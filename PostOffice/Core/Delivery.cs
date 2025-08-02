namespace PostOffice.Core;

/// <summary>
/// Base class for synchronous mail handlers
/// </summary>
public abstract class Delivery<TPackage, TResponse>
{
    /// <summary>
    /// Handles the mail request
    /// </summary>
    public abstract TResponse Handle(TPackage request);
}
