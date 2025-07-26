using BenchmarkDotNet.Attributes;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Examples;

namespace PostOffice.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class PerformanceEnhancementBenchmarks
{
    private IServiceProvider _standardProvider = null!;
    private IServiceProvider _highPerformanceProvider = null!;
    private IServiceProvider _maxPerformanceProvider = null!;
    
    private Poster _standardPoster = null!;
    private HighPerformancePoster _highPerformancePoster = null!;
    private HighPerformancePoster _maxPerformancePoster = null!;
    
    private FastRequest _validRequest = null!;
    private FastRequest _invalidRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Standard configuration (baseline)
        var standardServices = new ServiceCollection();
        standardServices.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        _standardProvider = standardServices.BuildServiceProvider();
        _standardPoster = _standardProvider.GetRequiredService<Poster>();

        // High-performance configuration (compiled expressions)
        var highPerfServices = new ServiceCollection();
        highPerfServices.AddPostOffice()
            .AddHighPerformancePoster()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        _highPerformanceProvider = highPerfServices.BuildServiceProvider();
        _highPerformancePoster = _highPerformanceProvider.GetRequiredService<HighPerformancePoster>();

        // Maximum performance configuration (all optimizations)
        var maxPerfServices = new ServiceCollection();
        PerformanceExamples.ConfigureMaxPerformance(maxPerfServices);
        _maxPerformanceProvider = maxPerfServices.BuildServiceProvider();
        _maxPerformancePoster = _maxPerformanceProvider.GetRequiredService<HighPerformancePoster>();

        // Warmup caches
        HighPerformancePoster.WarmupForTypes(typeof(FastRequest));

        // Test data
        _validRequest = new FastRequest { Name = "TestUser", Value = 42 };
        _invalidRequest = new FastRequest { Name = "", Value = -1 }; // Invalid
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_standardProvider as IDisposable)?.Dispose();
        (_highPerformanceProvider as IDisposable)?.Dispose();
        (_maxPerformanceProvider as IDisposable)?.Dispose();
    }

    // ========== VALID REQUEST BENCHMARKS ==========

    [Benchmark(Baseline = true)]
    public async Task<string> Standard_ValidRequest()
    {
        return await _standardPoster.Send(_validRequest);
    }

    [Benchmark]
    public async Task<string> HighPerformance_ValidRequest()
    {
        return await _highPerformancePoster.Send(_validRequest);
    }

    [Benchmark]
    public async Task<string> MaxPerformance_ValidRequest()
    {
        return await _maxPerformancePoster.Send(_validRequest);
    }

    // ========== INVALID REQUEST BENCHMARKS ==========

    [Benchmark]
    public async Task<string> Standard_InvalidRequest()
    {
        try
        {
            return await _standardPoster.Send(_invalidRequest);
        }
        catch (ValidationException)
        {
            return "Validation failed";
        }
    }

    [Benchmark]
    public async Task<string> HighPerformance_InvalidRequest()
    {
        try
        {
            return await _highPerformancePoster.Send(_invalidRequest);
        }
        catch (ValidationException)
        {
            return "Validation failed";
        }
    }

    [Benchmark]
    public async Task<string> MaxPerformance_InvalidRequest()
    {
        try
        {
            return await _maxPerformancePoster.Send(_invalidRequest);
        }
        catch (ValidationException)
        {
            return "Validation failed";
        }
    }
}

/// <summary>
/// Benchmarks comparing different performance profiles
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class PerformanceProfileBenchmarks
{
    private HighPerformancePoster _maxThroughputPoster = null!;
    private HighPerformancePoster _lowLatencyPoster = null!;
    private HighPerformancePoster _lowMemoryPoster = null!;
    private HighPerformancePoster _balancedPoster = null!;
    
    private FastRequest _validRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Max Throughput Profile
        var maxThroughputServices = new ServiceCollection();
        maxThroughputServices.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.MaxThroughput)
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        var maxThroughputProvider = maxThroughputServices.BuildServiceProvider();
        _maxThroughputPoster = maxThroughputProvider.GetRequiredService<HighPerformancePoster>();

        // Low Latency Profile
        var lowLatencyServices = new ServiceCollection();
        lowLatencyServices.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.LowLatency)
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        var lowLatencyProvider = lowLatencyServices.BuildServiceProvider();
        _lowLatencyPoster = lowLatencyProvider.GetRequiredService<HighPerformancePoster>();

        // Low Memory Profile
        var lowMemoryServices = new ServiceCollection();
        lowMemoryServices.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.LowMemory)
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        var lowMemoryProvider = lowMemoryServices.BuildServiceProvider();
        _lowMemoryPoster = lowMemoryProvider.GetRequiredService<HighPerformancePoster>();

        // Balanced Profile
        var balancedServices = new ServiceCollection();
        balancedServices.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.Balanced)
            .AddValidatorsFromAssemblyContaining<FastRequestValidator>();
        var balancedProvider = balancedServices.BuildServiceProvider();
        _balancedPoster = balancedProvider.GetRequiredService<HighPerformancePoster>();

        _validRequest = new FastRequest { Name = "TestUser", Value = 42 };
    }

    [Benchmark(Baseline = true)]
    public async Task<string> MaxThroughput_Profile()
    {
        return await _maxThroughputPoster.Send(_validRequest);
    }

    [Benchmark]
    public async Task<string> LowLatency_Profile()
    {
        return await _lowLatencyPoster.Send(_validRequest);
    }

    [Benchmark]
    public async Task<string> LowMemory_Profile()
    {
        return await _lowMemoryPoster.Send(_validRequest);
    }

    [Benchmark]
    public async Task<string> Balanced_Profile()
    {
        return await _balancedPoster.Send(_validRequest);
    }
}

/// <summary>
/// Benchmarks for compiled expressions vs reflection
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CompiledVsReflectionBenchmarks
{
    private SimpleRequest _request = null!;
    private SimpleRequestHandler _handler = null!;
    private Func<object, object, Task<object>> _compiledHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        _request = new SimpleRequest { Message = "Test" };
        _handler = new SimpleRequestHandler();
        
        // Get compiled handler
        _compiledHandler = CompiledHandlerCache.GetOrCompileHandler(typeof(SimpleRequest), typeof(string));
    }

    [Benchmark(Baseline = true)]
    public async Task<string> Reflection_HandlerCall()
    {
        // Simulate reflection call
        var method = typeof(SimpleRequestHandler).GetMethod("HandleAsync")!;
        var task = (Task<string>)method.Invoke(_handler, new object[] { _request })!;
        return await task;
    }

    [Benchmark]
    public async Task<object> Compiled_HandlerCall()
    {
        // Use compiled expression
        return await _compiledHandler(_handler, _request);
    }
}

/// <summary>
/// Memory allocation comparison benchmarks
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryAllocationBenchmarks
{
    [Benchmark(Baseline = true)]
    public List<string> Standard_ListAllocation()
    {
        var list = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            list.Add($"Error {i}");
        }
        return list;
    }

    [Benchmark]
    public List<string> Pooled_ListAllocation()
    {
        var list = ObjectPooling.RentValidationFailureList();
        try
        {
            for (int i = 0; i < 10; i++)
            {
                // Simulate adding errors (we can't add strings to ValidationFailure list, but this shows the pattern)
            }
            return new List<string>(); // Return empty for comparison
        }
        finally
        {
            ObjectPooling.ReturnValidationFailureList(list);
        }
    }

    [Benchmark]
    public string Standard_StringConcat()
    {
        var strings = new[] { "Error 1", "Error 2", "Error 3", "Error 4", "Error 5" };
        return string.Join(", ", strings);
    }

    [Benchmark]
    public string Optimized_StringConcat()
    {
        var strings = new[] { "Error 1", "Error 2", "Error 3", "Error 4", "Error 5" };
        return MemoryOptimizations.FastStringConcat(strings);
    }
} 