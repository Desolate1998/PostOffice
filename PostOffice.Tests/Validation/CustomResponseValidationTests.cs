using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Validation;
using Xunit;

namespace PostOffice.Tests.Validation;

public class CustomResponseValidationTests
{
    [Fact]
    public async Task StringResponse_ValidationFails_ReturnsErrorMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidatorsFromAssemblyContaining<SimpleRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var invalidRequest = new SimpleRequest { Name = "" }; // Invalid

        // Act
        var result = await poster.Send(invalidRequest);

        // Assert
        Assert.Equal("Name is required", result);
    }

    [Fact]
    public async Task CustomErrorResponse_ValidationFails_ReturnsCustomObject()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidatorsFromAssemblyContaining<UserRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var invalidRequest = new UserRequest 
        { 
            Name = "", 
            Email = "invalid" 
        };

        // Act
        var result = await poster.Send(invalidRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Name is required", result.Errors);
        Assert.Contains("Invalid email", result.Errors);
    }

    [Fact]
    public async Task ValidInput_CallsHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddCustomResponseValidation()
            .AddValidatorsFromAssemblyContaining<SimpleRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var validRequest = new SimpleRequest { Name = "John" };

        // Act
        var result = await poster.Send(validRequest);

        // Assert
        Assert.Equal("Hello, John!", result);
    }
}

// Clean test data
public class SimpleRequest : IMail<string>
{
    public string Name { get; set; } = string.Empty;
}

public class UserRequest : IMail<UserResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserResponse : IValidationErrorResponse<UserResponse>
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();

    public UserResponse FromValidationFailures(IEnumerable<ValidationFailure> failures)
    {
        return new UserResponse
        {
            Success = false,
            Errors = failures.Select(f => f.ErrorMessage).ToList()
        };
    }
}

// Validators
public class SimpleRequestValidator : AbstractValidator<SimpleRequest>
{
    public SimpleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
    }
}

public class UserRequestValidator : AbstractValidator<UserRequest>
{
    public UserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Email).EmailAddress().WithMessage("Invalid email");
    }
}

// Handlers
public class SimpleRequestHandler : DeliveryAsync<SimpleRequest, string>
{
    public override Task<string> HandleAsync(SimpleRequest request)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}

public class UserRequestHandler : DeliveryAsync<UserRequest, UserResponse>
{
    public override Task<UserResponse> HandleAsync(UserRequest request)
    {
        return Task.FromResult(new UserResponse { Success = true });
    }
} 