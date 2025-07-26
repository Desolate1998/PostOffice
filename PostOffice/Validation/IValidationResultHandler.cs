using FluentValidation.Results;

namespace PostOffice.Validation;

/// <summary>
/// Interface for handling validation results in a custom way
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IValidationResultHandler<TResponse>
{
    /// <summary>
    /// Creates a response when validation fails
    /// </summary>
    /// <param name="failures">The validation failures</param>
    /// <returns>The error response</returns>
    TResponse CreateErrorResponse(IEnumerable<ValidationFailure> failures);
    
    /// <summary>
    /// Determines if this handler can create an error response for the given type
    /// </summary>
    bool CanHandle(Type responseType);
} 