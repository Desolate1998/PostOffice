using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Validation;
using Xunit;

namespace PostOffice.Tests.Validation;

/// <summary>
/// Your exact example - validator returns "Test" when there are errors
/// </summary>
public class YourTestExample
{
    [Fact]
    public async Task ReturnsTest_WhenValidationFails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidationResultHandler<TestErrorHandler, string>()
            .AddValidatorsFromAssemblyContaining<MyRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var invalidRequest = new MyRequest { Name = "" }; // Invalid

        // Act
        var result = await poster.Send(invalidRequest);

        // Assert
        Assert.Equal("Test", result); // 🎯 Returns "Test" exactly as you wanted!
    }

    [Fact]
    public async Task CallsHandler_WhenValidationPasses()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidationResultHandler<TestErrorHandler, string>()
            .AddValidatorsFromAssemblyContaining<MyRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var validRequest = new MyRequest { Name = "John" };

        // Act
        var result = await poster.Send(validRequest);

        // Assert
        Assert.Equal("Success: John", result); // Calls handler when valid
    }
}

// Clean, simple implementation
public class MyRequest : IMail<string>
{
    public string Name { get; set; } = string.Empty;
}

public class MyRequestValidator : AbstractValidator<MyRequest>
{
    public MyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}

public class MyRequestHandler : DeliveryAsync<MyRequest, string>
{
    public override Task<string> HandleAsync(MyRequest request)
    {
        return Task.FromResult($"Success: {request.Name}");
    }
}

/// <summary>
/// Custom handler that returns "Test" for any validation errors
/// </summary>
public class TestErrorHandler : IValidationResultHandler<string>
{
    public bool CanHandle(Type responseType) => responseType == typeof(string);

    public string CreateErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        return "Test"; // 🎯 Always returns "Test" when there are errors!
    }
} 