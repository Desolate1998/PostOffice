namespace PostOffice.Core;

/// <summary>
/// Base class for asynchronous mail handlers
/// </summary>
public abstract class DeliveryAsync<TPackage, TResponse>
{
    /// <summary>
    /// Handles the mail request asynchronously
    /// </summary>
    public abstract Task<TResponse> HandleAsync(TPackage request);
}
