namespace PostOffice.Core;

public abstract class Delivery<TPackage, TResponse>
{
    public abstract TResponse Handle(TPackage request);
}
