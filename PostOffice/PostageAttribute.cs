namespace PostOffice;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PostageAttribute : Attribute
{
    public Type MiddlewareType { get; }
    public int Order { get; }

    public PostageAttribute(Type middlewareType, int order = 0)
    {
        MiddlewareType = middlewareType;
        Order = order;
    }
}
