using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Middleware;
using PostOffice.Validation;
using Xunit;

namespace PostOffice.Tests;

public class PostOfficeBuilderTests
{
    [Fact]
    public void AddPostOffice_RegistersPoster()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetService<Poster>();
        Assert.NotNull(poster);
    }

    [Fact]
    public void AddPostOffice_RegistersMiddlewarePipeline()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetService<IMiddlewarePipeline<TestMail, string>>();
        Assert.NotNull(pipeline);
        Assert.IsType<MiddlewarePipeline<TestMail, string>>(pipeline);
    }

    [Fact]
    public void AddPostOffice_AutoRegistersHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetService<DeliveryAsync<TestMail, string>>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddValidation_RegistersValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice()
            .AddValidation();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var behavior = serviceProvider.GetService<ValidationBehavior<TestMail, string>>();
        Assert.NotNull(behavior);
    }

    [Fact]
    public void AddPostOfficeWithValidation_RegistersBothPosterAndValidation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOfficeWithValidation();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        var poster = serviceProvider.GetService<Poster>();
        Assert.NotNull(poster);
        
        var behavior = serviceProvider.GetService<ValidationBehavior<TestMail, string>>();
        Assert.NotNull(behavior);
    }

    [Fact]
    public void AddValidatorsFromAssemblyContaining_RegistersValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<TestMailValidator>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidator<TestMail>>();
        Assert.NotNull(validator);
        Assert.IsType<TestMailValidator>(validator);
    }

    [Fact]
    public void AddFluentValidation_WithAutoDiscovery_RegistersValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice()
            .AddFluentValidation(config =>
            {
                config.AutoDiscoverValidators = true;
                config.AssembliesToScan = new[] { typeof(TestMailValidator).Assembly };
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidator<TestMail>>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void AddFluentValidation_WithoutAutoDiscovery_DoesNotRegisterValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice()
            .AddFluentValidation(config =>
            {
                config.AutoDiscoverValidators = false;
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidator<TestMail>>();
        Assert.Null(validator);
    }

    [Fact]
    public void AddMiddleware_RegistersCustomMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddPostOffice()
            .AddMiddleware<TestMiddleware>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var middleware = serviceProvider.GetService<TestMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddMiddleware_WithFactory_RegistersMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var factoryCalled = false;

        // Act
        var builder = services.AddPostOffice()
            .AddMiddleware<TestMiddleware>(sp =>
            {
                factoryCalled = true;
                return new TestMiddleware();
            });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var middleware = serviceProvider.GetService<TestMiddleware>();
        Assert.NotNull(middleware);
        Assert.True(factoryCalled);
    }
}

// Test validator
public class TestMailValidator : AbstractValidator<TestMail>
{
    public TestMailValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
    }
}

// Test middleware
public class TestMiddleware : IPostageMiddleware<TestMail, string>
{
    public Task<(bool handled, string? result)> StampAsync(TestMail mail, Func<TestMail, Task<string>> next)
    {
        return Task.FromResult((false, default(string)));
    }
} 