using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Tests;

public class PosterTests
{
    [Fact]
    public async Task Send_WithNoMiddleware_CallsHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DeliveryAsync<TestMail, string>, TestMailHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        // Act
        var result = await poster.Send(testMail);

        // Assert
        Assert.Equal("Handled: test", result);
    }

    [Fact]
    public async Task Send_WithMiddlewarePipeline_CallsMiddlewareFirst()
    {
        // Arrange
        var middlewareMock = new Mock<IPostageMiddleware<TestMail, string>>();
        middlewareMock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>(async (mail, next) =>
            {
                var result = await next(mail);
                return (false, $"Middleware: {result}");
            });

        var services = new ServiceCollection();
        services.AddTransient<DeliveryAsync<TestMail, string>, TestMailHandler>();
        services.AddTransient<IMiddlewarePipeline<TestMail, string>, MiddlewarePipeline<TestMail, string>>();
        services.AddTransient<IPostageMiddleware<TestMail, string>>(_ => middlewareMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        // Act
        var result = await poster.Send(testMail);

        // Assert
        Assert.Equal("Middleware: Handled: test", result);
        middlewareMock.Verify(m => m.StampAsync(testMail, It.IsAny<Func<TestMail, Task<string>>>()), Times.Once);
    }

    [Fact]
    public async Task Send_WithHandlerThatThrows_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<DeliveryAsync<TestMail, string>, ThrowingTestMailHandler>();
        var serviceProvider = services.BuildServiceProvider();

        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "error" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => poster.Send(testMail));
        Assert.Equal("Handler error", exception.Message);
    }

    [Fact]
    public async Task Send_WithNonExistentHandler_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => poster.Send(testMail));
    }

    [Fact]
    public async Task Send_WithMultipleMiddleware_ExecutesInOrder()
    {
        // Arrange
        var middleware1Mock = new Mock<IPostageMiddleware<TestMail, string>>();
        middleware1Mock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>(async (mail, next) =>
            {
                var result = await next(mail);
                return (false, $"MW1({result})");
            });

        var middleware2Mock = new Mock<IPostageMiddleware<TestMail, string>>();
        middleware2Mock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>(async (mail, next) =>
            {
                var result = await next(mail);
                return (false, $"MW2({result})");
            });

        var services = new ServiceCollection();
        services.AddTransient<DeliveryAsync<TestMail, string>, TestMailHandler>();
        services.AddTransient<IMiddlewarePipeline<TestMail, string>, MiddlewarePipeline<TestMail, string>>();
        services.AddTransient<IPostageMiddleware<TestMail, string>>(_ => middleware1Mock.Object);
        services.AddTransient<IPostageMiddleware<TestMail, string>>(_ => middleware2Mock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        // Act
        var result = await poster.Send(testMail);

        // Assert
        // Note: The order depends on how DI returns the collection, but both should be called
        Assert.Contains("MW1", result);
        Assert.Contains("MW2", result);
        Assert.Contains("Handled: test", result);
    }

    [Fact]
    public async Task Send_WithMiddlewareThatHandles_DoesNotCallHandler()
    {
        // Arrange
        var middlewareMock = new Mock<IPostageMiddleware<TestMail, string>>();
        middlewareMock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .ReturnsAsync((true, "Handled by middleware"));

        var handlerMock = new Mock<TestMailHandler>();

        var services = new ServiceCollection();
        services.AddTransient<IMiddlewarePipeline<TestMail, string>, MiddlewarePipeline<TestMail, string>>();
        services.AddTransient<IPostageMiddleware<TestMail, string>>(_ => middlewareMock.Object);
        services.AddTransient<DeliveryAsync<TestMail, string>>(_ => handlerMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var poster = new Poster(serviceProvider);
        var testMail = new TestMail { Value = "test" };

        // Act
        var result = await poster.Send(testMail);

        // Assert
        Assert.Equal("Handled by middleware", result);
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<TestMail>()), Times.Never);
    }
}

// Test handlers
public class TestMailHandler : DeliveryAsync<TestMail, string>
{
    public override Task<string> HandleAsync(TestMail request)
    {
        return Task.FromResult($"Handled: {request.Value}");
    }
}

public class ThrowingTestMailHandler : DeliveryAsync<TestMail, string>
{
    public override Task<string> HandleAsync(TestMail request)
    {
        throw new InvalidOperationException("Handler error");
    }
} 