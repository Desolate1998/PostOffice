using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using PostOffice.Validation;

namespace PostOffice.Examples;

/// <summary>
/// Examples demonstrating all the cool performance enhancements
/// </summary>
public static class PerformanceExamples
{
    /// <summary>
    /// 🚀 MAXIMUM PERFORMANCE: All optimizations enabled
    /// Expected: ~50-100ns for simple operations (vs 2,400ns standard)
    /// </summary>
    public static void ConfigureMaxPerformance(IServiceCollection services)
    {
        services.AddPostOffice()
            .AddMaxPerformance()  // 🔥 ALL optimizations enabled!
            .WarmupForTypes(      // 🏃‍♂️ Precompile common handlers
                typeof(FastRequest),
                typeof(SimpleRequest),
                typeof(ComplexRequest)
            );

        // Add your validators
        services.AddValidatorsFromAssemblyContaining<FastRequestValidator>();
    }

    /// <summary>
    /// ⚡ HIGH THROUGHPUT: Optimized for maximum requests/second
    /// Best for APIs handling thousands of requests
    /// </summary>
    public static void ConfigureHighThroughput(IServiceCollection services)
    {
        services.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.MaxThroughput)
            .WarmupForTypes(typeof(FastRequest));
    }

    /// <summary>
    /// 🏃‍♂️ LOW LATENCY: Optimized for minimum response time  
    /// Best for real-time applications
    /// </summary>
    public static void ConfigureLowLatency(IServiceCollection services)
    {
        services.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.LowLatency)
            .WarmupForTypes(typeof(FastRequest));
    }

    /// <summary>
    /// 💾 LOW MEMORY: Optimized for minimal allocations
    /// Best for high-concurrency scenarios
    /// </summary>
    public static void ConfigureLowMemory(IServiceCollection services)
    {
        services.AddPostOffice()
            .AddPerformanceProfile(PerformanceProfile.LowMemory);
    }

    /// <summary>
    /// 🔍 MONITORING: How to monitor performance
    /// </summary>
    public static void MonitorPerformance()
    {
        var stats = PerformanceMonitoring.GetStats();
        Console.WriteLine(stats.ToString());
        
        // Example output:
        // PostOffice Performance Stats:
        //   Compiled Handlers: 5
        //   Compiled Resolvers: 5  
        //   Pooled Contexts: 12
        //   Pooled Lists: 8
        //   Pooled Builders: 15
    }

    /// <summary>
    /// Custom high-performance usage example
    /// </summary>
    public static async Task<string> HighPerformanceUsageExample(IServiceProvider services)
    {
        // Use the high-performance poster directly
        var poster = services.GetRequiredService<HighPerformancePoster>();
        
        var request = new FastRequest { Name = "Test", Value = 42 };
        
        // This will be BLAZING fast! ⚡
        // - No reflection overhead (compiled expressions)
        // - Stack allocation for small operations  
        // - Fast-path validation for simple rules
        // - Object pooling for reduced GC pressure
        return await poster.Send(request);
    }
}

// Example request types for performance testing

/// <summary>
/// Simple request with fast-path validation (uses Data Annotations)
/// </summary>
public class FastRequest : IMail<string>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Value { get; set; }
}

/// <summary>
/// Handler for FastRequest - will be compiled to eliminate reflection
/// </summary>
public class FastRequestHandler : DeliveryAsync<FastRequest, string>
{
    public override async Task<string> HandleAsync(FastRequest request)
    {
        // Simulate some work
        await Task.Delay(1); // In real code, this would be your business logic
        return $"Processed: {request.Name} with value {request.Value}";
    }
}

/// <summary>
/// FluentValidation validator (for fallback when fast-path isn't available)
/// </summary>
public class FastRequestValidator : AbstractValidator<FastRequest>
{
    public FastRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 100);

        RuleFor(x => x.Value)
            .InclusiveBetween(1, 1000);
    }
}

/// <summary>
/// Simple request for basic performance testing
/// </summary>
public class SimpleRequest : IMail<string>
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Simple handler
/// </summary>
public class SimpleRequestHandler : DeliveryAsync<SimpleRequest, string>
{
    public override Task<string> HandleAsync(SimpleRequest request)
    {
        return Task.FromResult($"Handled: {request.Message}");
    }
}

/// <summary>
/// Complex request for testing with multiple validators
/// </summary>
public class ComplexRequest : IMail<ComplexResponse>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ComplexResponse
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Complex handler
/// </summary>
public class ComplexRequestHandler : DeliveryAsync<ComplexRequest, ComplexResponse>
{
    public override Task<ComplexResponse> HandleAsync(ComplexRequest request)
    {
        return Task.FromResult(new ComplexResponse
        {
            Id = Random.Shared.Next(1, 10000),
            Status = "Processed",
            ProcessedAt = DateTime.UtcNow
        });
    }
} 