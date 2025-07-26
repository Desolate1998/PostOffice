using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Tests;

public class MiddlewarePipelineTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public MiddlewarePipelineTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMiddleware_CallsFinalHandlerDirectly()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestMail, string>(Enumerable.Empty<IPostageMiddleware<TestMail, string>>());
        var testMail = new TestMail { Value = "test" };
        var expectedResult = "handled";
        
        var finalHandlerCalled = false;
        Func<TestMail, Task<string>> finalHandler = (mail) =>
        {
            finalHandlerCalled = true;
            Assert.Equal(testMail, mail);
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await pipeline.ExecuteAsync(testMail, finalHandler);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.True(finalHandlerCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleMiddleware_CallsMiddlewareThenHandler()
    {
        // Arrange
        var middlewareMock = new Mock<IPostageMiddleware<TestMail, string>>();
        middlewareMock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>((mail, next) => 
            {
                // Middleware doesn't handle, passes to next
                return Task.FromResult((false, default(string)));
            });

        var pipeline = new MiddlewarePipeline<TestMail, string>(new[] { middlewareMock.Object });
        var testMail = new TestMail { Value = "test" };
        var expectedResult = "handled";

        var finalHandlerCalled = false;
        Func<TestMail, Task<string>> finalHandler = (mail) =>
        {
            finalHandlerCalled = true;
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await pipeline.ExecuteAsync(testMail, finalHandler);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.True(finalHandlerCalled);
        middlewareMock.Verify(m => m.StampAsync(testMail, It.IsAny<Func<TestMail, Task<string>>>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMiddlewareThatHandles_DoesNotCallFinalHandler()
    {
        // Arrange
        var handledResult = "middleware-handled";
        var middlewareMock = new Mock<IPostageMiddleware<TestMail, string>>();
        middlewareMock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .ReturnsAsync((true, handledResult));

        var pipeline = new MiddlewarePipeline<TestMail, string>(new[] { middlewareMock.Object });
        var testMail = new TestMail { Value = "test" };

        var finalHandlerCalled = false;
        Func<TestMail, Task<string>> finalHandler = (mail) =>
        {
            finalHandlerCalled = true;
            return Task.FromResult("should-not-be-called");
        };

        // Act
        var result = await pipeline.ExecuteAsync(testMail, finalHandler);

        // Assert
        Assert.Equal(handledResult, result);
        Assert.False(finalHandlerCalled);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleMiddleware_CallsInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        
        var middleware1 = new Mock<IPostageMiddleware<TestMail, string>>();
        middleware1
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>(async (mail, next) =>
            {
                callOrder.Add("middleware1");
                await next(mail);
                return (false, default(string));
            });

        var middleware2 = new Mock<IPostageMiddleware<TestMail, string>>();
        middleware2
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .Returns<TestMail, Func<TestMail, Task<string>>>(async (mail, next) =>
            {
                callOrder.Add("middleware2");
                await next(mail);
                return (false, default(string));
            });

        var pipeline = new MiddlewarePipeline<TestMail, string>(new[] { middleware1.Object, middleware2.Object });
        var testMail = new TestMail { Value = "test" };

        Func<TestMail, Task<string>> finalHandler = (mail) =>
        {
            callOrder.Add("handler");
            return Task.FromResult("handled");
        };

        // Act
        await pipeline.ExecuteAsync(testMail, finalHandler);

        // Assert
        Assert.Equal(new[] { "middleware1", "middleware2", "handler" }, callOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WithMiddlewareException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Middleware error");
        var middlewareMock = new Mock<IPostageMiddleware<TestMail, string>>();
        middlewareMock
            .Setup(m => m.StampAsync(It.IsAny<TestMail>(), It.IsAny<Func<TestMail, Task<string>>>()))
            .ThrowsAsync(expectedException);

        var pipeline = new MiddlewarePipeline<TestMail, string>(new[] { middlewareMock.Object });
        var testMail = new TestMail { Value = "test" };

        Func<TestMail, Task<string>> finalHandler = (mail) => Task.FromResult("should-not-be-called");

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            pipeline.ExecuteAsync(testMail, finalHandler));
        
        Assert.Equal(expectedException, actualException);
    }
}

// Test data classes
public class TestMail : IMail<string>
{
    public string Value { get; set; } = string.Empty;
}

public class TestResponse
{
    public string Message { get; set; } = string.Empty;
    public int Id { get; set; }
} 