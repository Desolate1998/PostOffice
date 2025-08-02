namespace PostOffice.Attributes;

/// <summary>
/// Attribute to mark mail classes that should be validated
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ValidateAttribute : Attribute
{
    /// <summary>
    /// The order in which this validation should run
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Whether validation should stop the pipeline on failure
    /// </summary>
    public bool StopOnFailure { get; set; } = true;
} 