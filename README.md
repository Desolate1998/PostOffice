# PostOffice - High-Performance CQRS Messaging Library

A high-performance CQRS message processing library with middleware pipeline, FluentValidation integration, and optimized performance.

[![NuGet](https://img.shields.io/nuget/v/CQRS.PostOffice.svg)](https://www.nuget.org/packages/CQRS.PostOffice/)
[![Downloads](https://img.shields.io/nuget/dt/CQRS.PostOffice.svg)](https://www.nuget.org/packages/CQRS.PostOffice/)

## Overview

PostOffice is a high-performance CQRS messaging library that enables you to send messages through a configurable middleware pipeline with optimized performance characteristics. Designed for building scalable APIs, microservices, and real-time applications that require both reliability and performance.

## Quick Start

### Installation

```bash
dotnet add package CQRS.PostOffice
```

### Basic Setup

```csharp
// Configure services
services.AddPostOffice();

// Create a request
public class HelloRequest : IMail<string>
{
    public string Name { get; set; } = string.Empty;
}

// Create a handler
public class HelloHandler : DeliveryAsync<HelloRequest, string>
{
    public override Task<string> HandleAsync(HelloRequest request)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}

// Send the message
var poster = services.GetRequiredService<Poster>();
var result = await poster.Send(new HelloRequest { Name = "World" });
// Returns: "Hello, World!"
```

## Message Handlers

### Simple Message Handler

The core of PostOffice is the message handler pattern. Create handlers that inherit from `DeliveryAsync<TRequest, TResponse>`:

```csharp
// Define your request
public class GetUserRequest : IMail<User>
{
    public int UserId { get; set; }
}

// Create your handler
public class GetUserHandler : DeliveryAsync<GetUserRequest, User>
{
    private readonly IUserRepository _userRepository;

    public GetUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override async Task<User> HandleAsync(GetUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new NotFoundException($"User {request.UserId} not found");
            
        return user;
    }
}

// Use it
var result = await poster.Send(new GetUserRequest { UserId = 123 });
```

### Auto-Discovery

PostOffice automatically discovers and registers all handlers that inherit from `DeliveryAsync<,>`. No manual registration needed!

```csharp
// Just call AddPostOffice() - all handlers are auto-registered
services.AddPostOffice();
```

## Middleware System

### What is Middleware?

Middleware allows you to add cross-cutting concerns like logging, validation, caching, and authentication to your message pipeline. Middleware executes before and after your handlers.

### Adding Middleware

```csharp
// Configure PostOffice with middleware
services.AddPostOffice()
    .AddMiddleware<LoggingMiddleware<,>>()
    .AddMiddleware<CachingMiddleware<,>>();
```

### Creating Custom Middleware

```csharp
public class LoggingMiddleware<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
{
    private readonly ILogger<LoggingMiddleware<TMail, TResponse>> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware<TMail, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(
        TMail mail, Func<TMail, Task<TResponse>> next)
    {
        _logger.LogInformation("Processing {MailType}", typeof(TMail).Name);
        
        var stopwatch = Stopwatch.StartNew();
        var result = await next(mail);
        stopwatch.Stop();
        
        _logger.LogInformation("Processed {MailType} in {ElapsedMs}ms", 
            typeof(TMail).Name, stopwatch.ElapsedMilliseconds);
            
        return (false, result); // Continue to next middleware
    }
}
```

### Middleware Execution Order

Middleware executes in the order they're added:

```csharp
services.AddPostOffice()
    .AddMiddleware<AuthenticationMiddleware<,>>()    // 1st
    .AddMiddleware<LoggingMiddleware<,>>()           // 2nd
    .AddMiddleware<ValidationMiddleware<,>>()        // 3rd
    .AddMiddleware<CachingMiddleware<,>>();          // 4th
```

### Built-in Middleware

PostOffice includes several built-in middleware:

```csharp
services.AddPostOffice()
    .AddValidation()              // FluentValidation (throws exceptions)
    .AddCustomResponseValidation() // Custom responses (returns values)
    .AddFastPathValidation()      // Ultra-fast simple validation
    .AddPooledValidation()        // Memory-optimized validation
    .AddSpanValidation();         // Stack-allocated validation
```

## Validation

### Simple Validation with Data Annotations

Add validation attributes to your request models:

```csharp
public class CreateUserRequest : IMail<User>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; set; }
}
```

### FluentValidation Integration

For more complex validation rules, use FluentValidation:

```csharp
// Install FluentValidation
dotnet add package FluentValidation

// Create a validator
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Age)
            .GreaterThan(0).WithMessage("Age must be greater than 0")
            .LessThanOrEqualTo(120).WithMessage("Age cannot exceed 120");
    }
}

// Register validators
services.AddPostOffice()
    .AddValidation()  // Throws exceptions on validation failure
    .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
```

### Custom Response Validation

Instead of throwing exceptions, return custom error responses:

```csharp
// Configure custom response validation
services.AddPostOffice()
    .AddCustomResponseValidation()
    .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// Your request returns a custom response type
public class CreateUserRequest : IMail<ApiResponse<User>>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

// Handler returns structured response
public class CreateUserHandler : DeliveryAsync<CreateUserRequest, ApiResponse<User>>
{
    public override async Task<ApiResponse<User>> HandleAsync(CreateUserRequest request)
    {
        // Your business logic here
        var user = new User { Name = request.Name, Email = request.Email, Age = request.Age };
        
        return ApiResponse<User>.SuccessResult(user, "User created successfully");
    }
}

// Usage - validation errors return structured responses instead of exceptions
var result = await poster.Send(new CreateUserRequest { Name = "", Email = "invalid" });
// Returns: { "success": false, "message": "Validation failed", "errors": ["Name is required", "Invalid email format"] }
```

## Performance Optimizations

### Enable Performance Features

```csharp
// Enable all performance optimizations
services.AddPostOffice()
    .AddMaxPerformance();

// Or choose specific optimizations
services.AddPostOffice()
    .AddHighPerformancePoster()    // Compiled expressions
    .AddFastPathValidation()       // Fast validation
    .AddOptimizedPipeline()        // Optimized middleware
    .AddSpanValidation()           // Memory-efficient validation
    .AddPooledValidation();        // Object pooling
```

### Performance Profiles

Choose pre-configured performance profiles:

```csharp
services.AddPostOffice()
    .AddPerformanceProfile(PerformanceProfile.MaxThroughput);  // High-volume APIs

services.AddPostOffice()
    .AddPerformanceProfile(PerformanceProfile.LowLatency);     // Real-time applications

services.AddPostOffice()
    .AddPerformanceProfile(PerformanceProfile.LowMemory);      // Memory-constrained environments
```

## Complete Example

Here's a complete example showing all features together:

```csharp
// Program.cs
services.AddPostOffice()
    .AddMaxPerformance()
    .AddCustomResponseValidation()
    .AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>()
    .AddMiddleware<LoggingMiddleware<,>>();

// Request with validation
public class CreateUserRequest : IMail<ApiResponse<User>>
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

// FluentValidation rules
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

// Handler with business logic
public class CreateUserHandler : DeliveryAsync<CreateUserRequest, ApiResponse<User>>
{
    private readonly IUserRepository _userRepository;

    public CreateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override async Task<ApiResponse<User>> HandleAsync(CreateUserRequest request)
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(),
            Name = request.Name, 
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);
        
        return ApiResponse<User>.SuccessResult(user, "User created successfully");
    }
}

// Custom middleware for logging
public class LoggingMiddleware<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
{
    private readonly ILogger<LoggingMiddleware<TMail, TResponse>> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware<TMail, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(
        TMail mail, Func<TMail, Task<TResponse>> next)
    {
        _logger.LogInformation("Processing {MailType}", typeof(TMail).Name);
        var result = await next(mail);
        _logger.LogInformation("Processed {MailType} successfully", typeof(TMail).Name);
        return (false, result);
    }
}

// Usage
var poster = services.GetRequiredService<Poster>();

// Valid request
var validResult = await poster.Send(new CreateUserRequest 
{ 
    Name = "John Doe", 
    Email = "john@example.com" 
});
// Returns: { "success": true, "data": { "id": "...", "name": "John Doe", ... }, "message": "User created successfully" }

// Invalid request
var invalidResult = await poster.Send(new CreateUserRequest 
{ 
    Name = "", 
    Email = "invalid-email" 
});
// Returns: { "success": false, "message": "Validation failed", "errors": ["Name is required", "Invalid email format"] }
```

## Performance Benchmarks

| Scenario              | Standard   | PostOffice    | Improvement        |
| --------------------- | ---------- | ------------- | ------------------ |
| Simple Operations     | ~2,400ns   | **~300ns**    | **8x faster**      |
| Validation            | ~25,500ns  | **~1,000ns**  | **25x faster**     |
| Memory Usage          | 2,776B     | **~1,000B**   | **65% less**       |

## Why Choose PostOffice?

### Performance First
- Compiled expressions instead of slow reflection
- Object pooling for reduced GC pressure
- Fast-path optimizations for common scenarios
- Memory-efficient implementations throughout

### Flexibility
- Multiple validation strategies for different needs
- Custom response handling - return anything you want
- Middleware pipeline for cross-cutting concerns
- Performance profiles for different scenarios

### Production Ready
- Thread-safe implementations
- Comprehensive testing with 100+ tests
- Professional architecture with clean separation
- Excellent documentation and examples

### Developer Experience
- Fluent configuration API
- Rich IntelliSense support
- Helpful error messages
- Performance monitoring built-in

## License

MIT License - feel free to use in commercial projects!

## Links

- [NuGet Package](https://www.nuget.org/packages/CQRS.PostOffice/)
- [Source Repository](https://github.com/Desolate1998/PostOffice)
- [Issues](https://github.com/Desolate1998/PostOffice/issues)
