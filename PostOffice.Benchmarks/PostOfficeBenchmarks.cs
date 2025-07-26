using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Benchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class PostOfficeBenchmarks
{
    private IServiceProvider _serviceProviderNoMiddleware = null!;
    private IServiceProvider _serviceProviderWithValidation = null!;
    private IServiceProvider _serviceProviderWithMultipleMiddleware = null!;
    private Poster _posterNoMiddleware = null!;
    private Poster _posterWithValidation = null!;
    private Poster _posterWithMultipleMiddleware = null!;
    private BenchmarkRequest _validRequest = null!;
    private BenchmarkRequest _invalidRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup: No middleware
        var servicesNoMiddleware = new ServiceCollection();
        servicesNoMiddleware.AddPostOffice();
        _serviceProviderNoMiddleware = servicesNoMiddleware.BuildServiceProvider();
        _posterNoMiddleware = _serviceProviderNoMiddleware.GetRequiredService<Poster>();

        // Setup: With validation
        var servicesWithValidation = new ServiceCollection();
        servicesWithValidation.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<BenchmarkRequestValidator>();
        _serviceProviderWithValidation = servicesWithValidation.BuildServiceProvider();
        _posterWithValidation = _serviceProviderWithValidation.GetRequiredService<Poster>();

        // Setup: With multiple middleware
        var servicesWithMultiple = new ServiceCollection();
        servicesWithMultiple.AddPostOffice()
            .AddValidation()
            .AddMiddleware<LoggingBenchmarkMiddleware>()
            .AddMiddleware<TimingBenchmarkMiddleware>()
            .AddValidatorsFromAssemblyContaining<BenchmarkRequestValidator>();
        _serviceProviderWithMultipleMiddleware = servicesWithMultiple.BuildServiceProvider();
        _posterWithMultipleMiddleware = _serviceProviderWithMultipleMiddleware.GetRequiredService<Poster>();

        _validRequest = new BenchmarkRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        _invalidRequest = new BenchmarkRequest
        {
            Name = "",
            Email = "invalid-email",
            Age = -5
        };
    }

    [Benchmark(Baseline = true)]
    public async Task<BenchmarkResponse> DirectHandlerCall()
    {
        var handler = new BenchmarkRequestHandler();
        return await handler.HandleAsync(_validRequest);
    }

    [Benchmark]
    public async Task<BenchmarkResponse> NoMiddleware()
    {
        return await _posterNoMiddleware.Send(_validRequest);
    }

    [Benchmark]
    public async Task<BenchmarkResponse> WithValidationMiddleware_ValidInput()
    {
        return await _posterWithValidation.Send(_validRequest);
    }

    [Benchmark]
    public async Task<BenchmarkResponse> WithMultipleMiddleware()
    {
        return await _posterWithMultipleMiddleware.Send(_validRequest);
    }

    [Benchmark]
    public async Task WithValidationMiddleware_InvalidInput()
    {
        try
        {
            await _posterWithValidation.Send(_invalidRequest);
        }
        catch (ValidationException)
        {
            // Expected
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_serviceProviderNoMiddleware as IDisposable)?.Dispose();
        (_serviceProviderWithValidation as IDisposable)?.Dispose();
        (_serviceProviderWithMultipleMiddleware as IDisposable)?.Dispose();
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(10));
        }
    }
}

// Benchmark entities
public class BenchmarkRequest : IMail<BenchmarkResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class BenchmarkResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ProcessingTimeMs { get; set; }
}

public class BenchmarkRequestValidator : AbstractValidator<BenchmarkRequest>
{
    public BenchmarkRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(120);
    }
}

public class BenchmarkRequestHandler : DeliveryAsync<BenchmarkRequest, BenchmarkResponse>
{
    public override Task<BenchmarkResponse> HandleAsync(BenchmarkRequest request)
    {
        return Task.FromResult(new BenchmarkResponse
        {
            Message = $"Processed {request.Name}",
            Timestamp = DateTime.UtcNow,
            ProcessingTimeMs = 1
        });
    }
}

public class LoggingBenchmarkMiddleware : IPostageMiddleware<BenchmarkRequest, BenchmarkResponse>
{
    public async Task<(bool handled, BenchmarkResponse? result)> StampAsync(BenchmarkRequest mail, Func<BenchmarkRequest, Task<BenchmarkResponse>> next)
    {
        // Simulate minimal logging overhead
        var requestId = Guid.NewGuid().ToString("N")[..8];
        return (false, default(BenchmarkResponse));
    }
}

public class TimingBenchmarkMiddleware : IPostageMiddleware<BenchmarkRequest, BenchmarkResponse>
{
    public async Task<(bool handled, BenchmarkResponse? result)> StampAsync(BenchmarkRequest mail, Func<BenchmarkRequest, Task<BenchmarkResponse>> next)
    {
        // Simulate timing overhead
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;
        return (false, default(BenchmarkResponse));
    }
} 