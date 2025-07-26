using FluentValidation.Results;

namespace PostOffice.Validation;

/// <summary>
/// Default handler for string responses
/// </summary>
public class StringValidationResultHandler : IValidationResultHandler<string>
{
    public bool CanHandle(Type responseType) => responseType == typeof(string);

    public string CreateErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        return string.Join("; ", failures.Select(f => f.ErrorMessage));
    }
}

/// <summary>
/// Generic handler for types that implement IValidationErrorResponse
/// </summary>
public class GenericValidationResultHandler<TResponse> : IValidationResultHandler<TResponse>
    where TResponse : IValidationErrorResponse<TResponse>, new()
{
    public bool CanHandle(Type responseType) => typeof(IValidationErrorResponse<TResponse>).IsAssignableFrom(responseType);

    public TResponse CreateErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        var response = new TResponse();
        return response.FromValidationFailures(failures);
    }
}

/// <summary>
/// Interface for responses that can be created from validation failures
/// </summary>
/// <typeparam name="T">The response type</typeparam>
public interface IValidationErrorResponse<T>
{
    /// <summary>
    /// Creates an instance from validation failures
    /// </summary>
    T FromValidationFailures(IEnumerable<ValidationFailure> failures);
} 