using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Middleware;
using Xunit;

namespace PostOffice.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task EndToEnd_WithValidationMiddleware_ValidatesAndExecutes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var validRequest = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "John Doe",
            Age = 25
        };

        // Act
        var result = await poster.Send(validRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("John Doe", result.Message);
        Assert.True(result.UserId > 0);
    }

    [Fact]
    public async Task EndToEnd_WithValidationMiddleware_ThrowsOnInvalidInput()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var invalidRequest = new CreateUserRequest
        {
            Email = "invalid-email",
            Name = "", // Invalid
            Age = -5   // Invalid
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => poster.Send(invalidRequest));
        Assert.Contains(exception.Errors, e => e.PropertyName == "Email");
        Assert.Contains(exception.Errors, e => e.PropertyName == "Name");
        Assert.Contains(exception.Errors, e => e.PropertyName == "Age");
    }

    [Fact]
    public async Task EndToEnd_WithMultipleMiddleware_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddMiddleware<LoggingMiddleware>()
            .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var validRequest = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "John Doe",
            Age = 25
        };

        // Act
        var result = await poster.Send(validRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("John Doe", result.Message);

        // Verify logging middleware was called
        var loggingMiddleware = serviceProvider.GetRequiredService<LoggingMiddleware>();
        Assert.True(loggingMiddleware.WasCalled);
    }

    [Fact]
    public async Task EndToEnd_WithFluentValidationAutoDiscovery_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddFluentValidation(config =>
            {
                config.AutoDiscoverValidators = true;
                config.AssembliesToScan = new[] { typeof(CreateUserRequestValidator).Assembly };
            });

        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var validRequest = new CreateUserRequest
        {
            Email = "test@example.com",
            Name = "John Doe",
            Age = 25
        };

        // Act
        var result = await poster.Send(validRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("John Doe", result.Message);
    }

    [Fact]
    public async Task EndToEnd_WithoutValidation_ExecutesDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice(); // No validation

        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var request = new CreateUserRequest
        {
            Email = "invalid", // Would fail validation, but no validation middleware
            Name = "",
            Age = -5
        };

        // Act
        var result = await poster.Send(request);

        // Assert - Should execute without validation
        Assert.NotNull(result);
        Assert.True(result.UserId > 0);
    }
}

// Test entities for integration tests
public class CreateUserRequest : IMail<CreateUserResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class CreateUserResponse
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2);

        RuleFor(x => x.Age)
            .GreaterThan(0)
            .LessThan(120);
    }
}

public class CreateUserHandler : DeliveryAsync<CreateUserRequest, CreateUserResponse>
{
    public override async Task<CreateUserResponse> HandleAsync(CreateUserRequest request)
    {
        // Simulate async work
        await Task.Delay(1);

        return new CreateUserResponse
        {
            UserId = Random.Shared.Next(1, 1000),
            Message = $"User {request.Name} created successfully"
        };
    }
}

public class LoggingMiddleware : IPostageMiddleware<CreateUserRequest, CreateUserResponse>
{
    public bool WasCalled { get; private set; }

    public async Task<(bool handled, CreateUserResponse? result)> StampAsync(CreateUserRequest mail, Func<CreateUserRequest, Task<CreateUserResponse>> next)
    {
        WasCalled = true;
        // Just pass through to next middleware
        return (false, default(CreateUserResponse));
    }
} 