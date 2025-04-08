namespace PostOffice;

public abstract class DeliveryAsync<TPackage, TResponse>
{
    public abstract Task<TResponse> HandleAsync(TPackage request);
}
