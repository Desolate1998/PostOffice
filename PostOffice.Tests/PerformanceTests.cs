using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace PostOffice.Tests;

public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Throughput_NoMiddleware_HandlesHighVolume()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice();
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var requestCount = 1000;
        var requests = Enumerable.Range(0, requestCount)
            .Select(i => new PerformanceTestRequest { Id = i, Data = $"Test {i}" })
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = requests.Select(request => poster.Send(request));
        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        var throughput = requestCount / stopwatch.Elapsed.TotalSeconds;
        
        _output.WriteLine($"Processed {requestCount} requests in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {throughput:F2} requests/second");
        
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, result => Assert.NotNull(result.Message));
        
        // Should handle at least 1000 requests per second
        Assert.True(throughput > 1000, $"Throughput was {throughput:F2} req/s, expected > 1000 req/s");
    }

    [Fact]
    public async Task Throughput_WithValidationMiddleware_MaintainsPerformance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<PerformanceTestRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var requestCount = 500; // Lower count due to validation overhead
        var requests = Enumerable.Range(0, requestCount)
            .Select(i => new PerformanceTestRequest 
            { 
                Id = i, 
                Data = $"Test {i}",
                Email = $"test{i}@example.com",
                Age = 25 + (i % 50)
            })
            .ToList();

        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = requests.Select(request => poster.Send(request));
        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        var throughput = requestCount / stopwatch.Elapsed.TotalSeconds;
        
        _output.WriteLine($"Processed {requestCount} requests with validation in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {throughput:F2} requests/second");
        
        Assert.Equal(requestCount, results.Length);
        
        // Should handle at least 200 requests per second even with validation
        Assert.True(throughput > 200, $"Throughput was {throughput:F2} req/s, expected > 200 req/s");
    }

    [Fact]
    public async Task Memory_Usage_StaysWithinBounds()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<PerformanceTestRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var initialMemory = GC.GetTotalMemory(true);
        
        // Act - Process many requests
        var requestCount = 1000;
        var batches = 10;
        var requestsPerBatch = requestCount / batches;

        for (int batch = 0; batch < batches; batch++)
        {
            var requests = Enumerable.Range(0, requestsPerBatch)
                .Select(i => new PerformanceTestRequest 
                { 
                    Id = batch * requestsPerBatch + i, 
                    Data = $"Batch {batch} Test {i}",
                    Email = $"test{i}@example.com",
                    Age = 25
                })
                .ToList();

            var tasks = requests.Select(request => poster.Send(request));
            await Task.WhenAll(tasks);

            // Force garbage collection between batches
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        _output.WriteLine($"Initial memory: {initialMemory:N0} bytes");
        _output.WriteLine($"Final memory: {finalMemory:N0} bytes");
        _output.WriteLine($"Memory increase: {memoryIncrease:N0} bytes");
        
        // Memory increase should be reasonable (less than 10MB for 1000 requests)
        Assert.True(memoryIncrease < 10 * 1024 * 1024, 
            $"Memory increased by {memoryIncrease:N0} bytes, expected less than 10MB");
    }

    [Fact]
    public async Task Latency_P99_UnderThreshold()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<PerformanceTestRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var requestCount = 100;
        var latencies = new List<long>();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var request = new PerformanceTestRequest 
            { 
                Id = i, 
                Data = $"Test {i}",
                Email = $"test{i}@example.com",
                Age = 25
            };

            var stopwatch = Stopwatch.StartNew();
            await poster.Send(request);
            stopwatch.Stop();

            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        latencies.Sort();
        var p50 = latencies[latencies.Count * 50 / 100];
        var p95 = latencies[latencies.Count * 95 / 100];
        var p99 = latencies[latencies.Count * 99 / 100];
        var average = latencies.Average();

        _output.WriteLine($"Average latency: {average:F2}ms");
        _output.WriteLine($"P50 latency: {p50}ms");
        _output.WriteLine($"P95 latency: {p95}ms");
        _output.WriteLine($"P99 latency: {p99}ms");

        // P99 should be under 50ms for simple operations
        Assert.True(p99 < 50, $"P99 latency was {p99}ms, expected < 50ms");
        Assert.True(average < 10, $"Average latency was {average:F2}ms, expected < 10ms");
    }

    [Fact]
    public async Task ConcurrentRequests_NoRaceConditions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<PerformanceTestRequestValidator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var poster = serviceProvider.GetRequiredService<Poster>();

        var concurrentRequests = 50;
        var iterationsPerRequest = 20;
        
        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async requestIndex =>
            {
                var results = new List<PerformanceTestResponse>();
                
                for (int i = 0; i < iterationsPerRequest; i++)
                {
                    var request = new PerformanceTestRequest 
                    { 
                        Id = requestIndex * iterationsPerRequest + i, 
                        Data = $"Concurrent {requestIndex}-{i}",
                        Email = $"test{requestIndex}{i}@example.com",
                        Age = 25
                    };

                    var result = await poster.Send(request);
                    results.Add(result);
                }

                return results;
            });

        var allResults = await Task.WhenAll(tasks);

        // Assert
        var totalResults = allResults.SelectMany(r => r).ToList();
        var expectedCount = concurrentRequests * iterationsPerRequest;
        
        Assert.Equal(expectedCount, totalResults.Count);
        
        // Verify all results are unique and valid
        var uniqueIds = totalResults.Select(r => r.Id).Distinct().ToList();
        Assert.Equal(expectedCount, uniqueIds.Count);
        
        Assert.All(totalResults, result => 
        {
            Assert.NotNull(result.Message);
            Assert.True(result.Id >= 0);
        });
    }
}

// Performance test entities
public class PerformanceTestRequest : IMail<PerformanceTestResponse>
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class PerformanceTestResponse
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class PerformanceTestRequestValidator : AbstractValidator<PerformanceTestRequest>
{
    public PerformanceTestRequestValidator()
    {
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Age).GreaterThan(0).When(x => x.Age > 0);
    }
}

public class PerformanceTestRequestHandler : DeliveryAsync<PerformanceTestRequest, PerformanceTestResponse>
{
    public override Task<PerformanceTestResponse> HandleAsync(PerformanceTestRequest request)
    {
        return Task.FromResult(new PerformanceTestResponse
        {
            Id = request.Id,
            Message = $"Processed: {request.Data}",
            ProcessedAt = DateTime.UtcNow
        });
    }
} 