using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Core;
using PostOffice.Validation;

namespace PostOffice.Examples;

/// <summary>
/// Clean examples of how to use PostOffice with custom validation responses
/// </summary>
public static class CleanUsageExamples
{
    public static void RegisterServices(IServiceCollection services)
    {
        // 🔥 Clean way to add custom response validation
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidationResultHandler<TestErrorHandler, string>()
            .AddValidatorsFromAssemblyContaining<ExampleRequest>();
    }
}

// Example 1: Simple request that returns "Test" on validation errors
public class ExampleRequest : IMail<string>
{
    public string Name { get; set; } = string.Empty;
}

public class ExampleRequestValidator : AbstractValidator<ExampleRequest>
{
    public ExampleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}

public class ExampleRequestHandler : DeliveryAsync<ExampleRequest, string>
{
    public override Task<string> HandleAsync(ExampleRequest request)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}

// Example 2: Complex response with built-in error handling
public class UserRequest : IMail<UserResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserResponse : IValidationErrorResponse<UserResponse>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public UserResponse FromValidationFailures(IEnumerable<ValidationFailure> failures)
    {
        return new UserResponse
        {
            Success = false,
            Message = "Validation failed",
            Errors = failures.Select(f => f.ErrorMessage).ToList()
        };
    }
}

// Custom error handler that always returns "Test"
public class TestErrorHandler : IValidationResultHandler<string>
{
    public bool CanHandle(Type responseType) => responseType == typeof(string);

    public string CreateErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        return "Test"; // 🎯 Your exact requirement!
    }
} 