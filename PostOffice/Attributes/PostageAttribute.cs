namespace PostOffice.Attributes;

/// <summary>
/// Attribute to mark mail classes with middleware
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PostageAttribute(Type middlewareType, int order = 0) : Attribute
{
  /// <summary>
  /// The middleware type to apply
  /// </summary>
  public Type MiddlewareType { get; } = middlewareType;

  /// <summary>
  /// The order in which this middleware should run
  /// </summary>
  public int Order { get; } = order;
}
