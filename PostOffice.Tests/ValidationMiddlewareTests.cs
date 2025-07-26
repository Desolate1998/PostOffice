using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PostOffice.Core;
using PostOffice.Middleware;
using PostOffice.Validation;
using Xunit;

namespace PostOffice.Tests;

public class ValidationMiddlewareTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public ValidationMiddlewareTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    [Fact]
    public async Task StampAsync_WithNoValidator_ContinuesToNext()
    {
        // Arrange
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestMail>)))
            .Returns(null);

        var middleware = new ValidationMiddleware<TestMail, string>(_serviceProviderMock.Object);
        var testMail = new TestMail { Value = "test" };
        var nextCalled = false;

        Func<TestMail, Task<string>> next = (mail) =>
        {
            nextCalled = true;
            return Task.FromResult("next-result");
        };

        // Act
        var (handled, result) = await middleware.StampAsync(testMail, next);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
        Assert.False(nextCalled); // ValidationMiddleware doesn't call next, it just returns (false, default)
    }

    [Fact]
    public async Task StampAsync_WithValidInput_DoesNotThrow()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestMail>>();
        var validationResult = new ValidationResult(); // Empty = valid

        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestMail>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestMail>)))
            .Returns(validatorMock.Object);

        var middleware = new ValidationMiddleware<TestMail, string>(_serviceProviderMock.Object);
        var testMail = new TestMail { Value = "valid" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act & Assert (should not throw)
        var (handled, result) = await middleware.StampAsync(testMail, next);
        
        Assert.False(handled);
        Assert.Null(result);
    }

    [Fact]
    public async Task StampAsync_WithInvalidInput_ThrowsValidationException()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestMail>>();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Value", "Value is required")
        };
        var validationResult = new ValidationResult(validationFailures);

        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestMail>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestMail>)))
            .Returns(validatorMock.Object);

        var middleware = new ValidationMiddleware<TestMail, string>(_serviceProviderMock.Object);
        var testMail = new TestMail { Value = "" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            middleware.StampAsync(testMail, next));
        
        Assert.Single(exception.Errors);
        Assert.Equal("Value", exception.Errors.First().PropertyName);
        Assert.Equal("Value is required", exception.Errors.First().ErrorMessage);
    }
}

public class ValidationBehaviorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public ValidationBehaviorTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    [Fact]
    public async Task StampAsync_WithNoValidators_ContinuesToNext()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var behavior = new ValidationBehavior<TestMail, string>(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act
        var (handled, result) = await behavior.StampAsync(testMail, next);

        // Assert
        Assert.False(handled);
        Assert.Null(result);
    }

    [Fact]
    public async Task StampAsync_WithMultipleValidValidators_DoesNotThrow()
    {
        // Arrange
        var validator1Mock = new Mock<IValidator<TestMail>>();
        var validator2Mock = new Mock<IValidator<TestMail>>();

        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var services = new ServiceCollection();
        services.AddTransient<IValidator<TestMail>>(_ => validator1Mock.Object);
        services.AddTransient<IValidator<TestMail>>(_ => validator2Mock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var behavior = new ValidationBehavior<TestMail, string>(serviceProvider);
        var testMail = new TestMail { Value = "valid" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act & Assert (should not throw)
        var (handled, result) = await behavior.StampAsync(testMail, next);
        
        Assert.False(handled);
        Assert.Null(result);
    }

    [Fact]
    public async Task StampAsync_WithMultipleValidatorsOneInvalid_ThrowsValidationException()
    {
        // Arrange
        var validator1Mock = new Mock<IValidator<TestMail>>();
        var validator2Mock = new Mock<IValidator<TestMail>>();

        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Value", "Value must be longer")
        };
        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var services = new ServiceCollection();
        services.AddTransient<IValidator<TestMail>>(_ => validator1Mock.Object);
        services.AddTransient<IValidator<TestMail>>(_ => validator2Mock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var behavior = new ValidationBehavior<TestMail, string>(serviceProvider);
        var testMail = new TestMail { Value = "x" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            behavior.StampAsync(testMail, next));
        
        Assert.Single(exception.Errors);
        Assert.Equal("Value", exception.Errors.First().PropertyName);
    }

    [Fact]
    public async Task StampAsync_WithMultipleValidatorsMultipleFailures_AggregatesAllErrors()
    {
        // Arrange
        var validator1Mock = new Mock<IValidator<TestMail>>();
        var validator2Mock = new Mock<IValidator<TestMail>>();

        var failures1 = new List<ValidationFailure>
        {
            new ValidationFailure("Value", "Error from validator 1")
        };
        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures1));

        var failures2 = new List<ValidationFailure>
        {
            new ValidationFailure("OtherProperty", "Error from validator 2")
        };
        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<TestMail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures2));

        var services = new ServiceCollection();
        services.AddTransient<IValidator<TestMail>>(_ => validator1Mock.Object);
        services.AddTransient<IValidator<TestMail>>(_ => validator2Mock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var behavior = new ValidationBehavior<TestMail, string>(serviceProvider);
        var testMail = new TestMail { Value = "invalid" };

        Func<TestMail, Task<string>> next = (mail) => Task.FromResult("next-result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            behavior.StampAsync(testMail, next));
        
        Assert.Equal(2, exception.Errors.Count());
        Assert.Contains(exception.Errors, e => e.PropertyName == "Value");
        Assert.Contains(exception.Errors, e => e.PropertyName == "OtherProperty");
    }
} 