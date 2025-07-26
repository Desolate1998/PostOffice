using BenchmarkDotNet.Attributes;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PostOffice.Configuration;
using PostOffice.Core;

namespace PostOffice.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ValidationBenchmarks
{
    private Poster _posterWithValidation = null!;
    private Poster _posterWithMultipleValidators = null!;
    private Poster _posterWithoutValidation = null!;
    
    private ValidationRequest _validRequest = null!;
    private ValidationRequest _invalidRequest = null!;
    private ComplexValidationRequest _complexValidRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup: Single validator
        var servicesSingle = new ServiceCollection();
        servicesSingle.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<ValidationRequestValidator>();
        var providerSingle = servicesSingle.BuildServiceProvider();
        _posterWithValidation = providerSingle.GetRequiredService<Poster>();

        // Setup: Multiple validators
        var servicesMultiple = new ServiceCollection();
        servicesMultiple.AddPostOffice()
            .AddValidation()
            .AddValidatorsFromAssemblyContaining<ComplexValidationRequestValidator1>();
        var providerMultiple = servicesMultiple.BuildServiceProvider();
        _posterWithMultipleValidators = providerMultiple.GetRequiredService<Poster>();

        // Setup: No validation
        var servicesNoValidation = new ServiceCollection();
        servicesNoValidation.AddPostOffice();
        var providerNoValidation = servicesNoValidation.BuildServiceProvider();
        _posterWithoutValidation = providerNoValidation.GetRequiredService<Poster>();

        _validRequest = new ValidationRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30
        };

        _invalidRequest = new ValidationRequest
        {
            Name = "",
            Email = "invalid",
            Age = -5
        };

        _complexValidRequest = new ComplexValidationRequest
        {
            Name = "John Doe",
            Email = "john@example.com",
            Age = 30,
            Address = "123 Main St",
            Phone = "+1234567890",
            Website = "https://example.com",
            Description = "A valid description with enough characters"
        };
    }

    [Benchmark(Baseline = true)]
    public async Task<ValidationResponse> NoValidation()
    {
        return await _posterWithoutValidation.Send(_validRequest);
    }

    [Benchmark]
    public async Task<ValidationResponse> SingleValidator_ValidInput()
    {
        return await _posterWithValidation.Send(_validRequest);
    }

    [Benchmark]
    public async Task SingleValidator_InvalidInput()
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

    [Benchmark]
    public async Task<ComplexValidationResponse> MultipleValidators_ValidInput()
    {
        return await _posterWithMultipleValidators.Send(_complexValidRequest);
    }

    [Benchmark]
    public async Task MultipleValidators_InvalidInput()
    {
        var invalidComplex = new ComplexValidationRequest
        {
            Name = "",
            Email = "invalid",
            Age = -5,
            Address = "",
            Phone = "invalid",
            Website = "not-a-url",
            Description = "Short"
        };

        try
        {
            await _posterWithMultipleValidators.Send(invalidComplex);
        }
        catch (ValidationException)
        {
            // Expected
        }
    }
}

// Validation benchmark entities
public class ValidationRequest : IMail<ValidationResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class ValidationResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

public class ComplexValidationRequest : IMail<ComplexValidationResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ComplexValidationResponse
{
    public string Message { get; set; } = string.Empty;
    public int Score { get; set; }
}

// Validators
public class ValidationRequestValidator : AbstractValidator<ValidationRequest>
{
    public ValidationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(120);
    }
}

public class ComplexValidationRequestValidator1 : AbstractValidator<ComplexValidationRequest>
{
    public ComplexValidationRequestValidator1()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(120);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5);
    }
}

public class ComplexValidationRequestValidator2 : AbstractValidator<ComplexValidationRequest>
{
    public ComplexValidationRequestValidator2()
    {
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[\d\s\-\(\)]+$");
        RuleFor(x => x.Website).Must(BeValidUrl).WithMessage("Must be a valid URL");
        RuleFor(x => x.Description).MinimumLength(10).MaximumLength(500);
    }

    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}

// Handlers
public class ValidationRequestHandler : DeliveryAsync<ValidationRequest, ValidationResponse>
{
    public override Task<ValidationResponse> HandleAsync(ValidationRequest request)
    {
        return Task.FromResult(new ValidationResponse
        {
            Message = $"Validated {request.Name}",
            IsValid = true
        });
    }
}

public class ComplexValidationRequestHandler : DeliveryAsync<ComplexValidationRequest, ComplexValidationResponse>
{
    public override Task<ComplexValidationResponse> HandleAsync(ComplexValidationRequest request)
    {
        return Task.FromResult(new ComplexValidationResponse
        {
            Message = $"Complex validation passed for {request.Name}",
            Score = 100
        });
    }
} 