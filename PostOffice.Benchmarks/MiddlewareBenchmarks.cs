using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Middleware;

namespace PostOffice.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class MiddlewareBenchmarks
{
    private Poster _posterNoMiddleware = null!;
    private Poster _poster1Middleware = null!;
    private Poster _poster3Middleware = null!;
    private Poster _poster5Middleware = null!;
    private Poster _poster10Middleware = null!;
    
    private MiddlewareRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _request = new MiddlewareRequest { Data = "test data" };

        // No middleware
        var services0 = new ServiceCollection();
        services0.AddPostOffice();
        _posterNoMiddleware = services0.BuildServiceProvider().GetRequiredService<Poster>();

        // 1 middleware
        var services1 = new ServiceCollection();
        services1.AddPostOffice()
            .AddMiddleware<PassThroughMiddleware1>();
        _poster1Middleware = services1.BuildServiceProvider().GetRequiredService<Poster>();

        // 3 middleware
        var services3 = new ServiceCollection();
        services3.AddPostOffice()
            .AddMiddleware<PassThroughMiddleware1>()
            .AddMiddleware<PassThroughMiddleware2>()
            .AddMiddleware<PassThroughMiddleware3>();
        _poster3Middleware = services3.BuildServiceProvider().GetRequiredService<Poster>();

        // 5 middleware
        var services5 = new ServiceCollection();
        services5.AddPostOffice()
            .AddMiddleware<PassThroughMiddleware1>()
            .AddMiddleware<PassThroughMiddleware2>()
            .AddMiddleware<PassThroughMiddleware3>()
            .AddMiddleware<PassThroughMiddleware4>()
            .AddMiddleware<PassThroughMiddleware5>();
        _poster5Middleware = services5.BuildServiceProvider().GetRequiredService<Poster>();

        // 10 middleware
        var services10 = new ServiceCollection();
        services10.AddPostOffice()
            .AddMiddleware<PassThroughMiddleware1>()
            .AddMiddleware<PassThroughMiddleware2>()
            .AddMiddleware<PassThroughMiddleware3>()
            .AddMiddleware<PassThroughMiddleware4>()
            .AddMiddleware<PassThroughMiddleware5>()
            .AddMiddleware<PassThroughMiddleware6>()
            .AddMiddleware<PassThroughMiddleware7>()
            .AddMiddleware<PassThroughMiddleware8>()
            .AddMiddleware<PassThroughMiddleware9>()
            .AddMiddleware<PassThroughMiddleware10>();
        _poster10Middleware = services10.BuildServiceProvider().GetRequiredService<Poster>();
    }

    [Benchmark(Baseline = true)]
    public async Task<MiddlewareResponse> NoMiddleware()
    {
        return await _posterNoMiddleware.Send(_request);
    }

    [Benchmark]
    public async Task<MiddlewareResponse> OneMiddleware()
    {
        return await _poster1Middleware.Send(_request);
    }

    [Benchmark]
    public async Task<MiddlewareResponse> ThreeMiddleware()
    {
        return await _poster3Middleware.Send(_request);
    }

    [Benchmark]
    public async Task<MiddlewareResponse> FiveMiddleware()
    {
        return await _poster5Middleware.Send(_request);
    }

    [Benchmark]
    public async Task<MiddlewareResponse> TenMiddleware()
    {
        return await _poster10Middleware.Send(_request);
    }
}

// Test entities
public class MiddlewareRequest : IMail<MiddlewareResponse>
{
    public string Data { get; set; } = string.Empty;
}

public class MiddlewareResponse
{
    public string Result { get; set; } = string.Empty;
    public int MiddlewareCount { get; set; }
}

public class MiddlewareRequestHandler : DeliveryAsync<MiddlewareRequest, MiddlewareResponse>
{
    public override Task<MiddlewareResponse> HandleAsync(MiddlewareRequest request)
    {
        return Task.FromResult(new MiddlewareResponse
        {
            Result = $"Processed: {request.Data}",
            MiddlewareCount = 0
        });
    }
}

// Pass-through middleware implementations
public class PassThroughMiddleware1 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        // Minimal processing overhead
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware2 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware3 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware4 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware5 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware6 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware7 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware8 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware9 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
}

public class PassThroughMiddleware10 : IPostageMiddleware<MiddlewareRequest, MiddlewareResponse>
{
    public Task<(bool handled, MiddlewareResponse? result)> StampAsync(MiddlewareRequest mail, Func<MiddlewareRequest, Task<MiddlewareResponse>> next)
    {
        return Task.FromResult((false, default(MiddlewareResponse)));
    }
} 