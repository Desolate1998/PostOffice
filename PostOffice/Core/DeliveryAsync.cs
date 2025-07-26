namespace PostOffice.Core;

public abstract class DeliveryAsync<TPackage, TResponse>
{
    public abstract Task<TResponse> HandleAsync(TPackage request);
}
