# PostOffice

[![NuGet](https://img.shields.io/nuget/v/CQRS.PostOffice.svg)](https://www.nuget.org/packages/CQRS.PostOffice/)

## Installation

```bash
dotnet add package CQRS.PostOffice
```

## Quick Start

### Basic Setup

```csharp
services.AddPostOffice();

public record HelloRequest(string Name) : IMail<string>;

public class HelloHandler : DeliveryAsync<HelloRequest, string>
{
    public override Task<string> HandleAsync(HelloRequest request)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}

var poster = services.GetRequiredService<Poster>();
var result = await poster.Send(new HelloRequest("World"));

```
### Validation

Validators are automatically discovered and registered. Create validators that inherit from `AbstractValidator<T>`:

### Exception-Based Validation

```csharp
services.AddPostOffice()
        .AddValidation();

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(150);
    }
}

try
{
    var result = await poster.Send(new CreateUserRequest("John", "john@example.com", 25));
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"- {error.ErrorMessage}");
    }
}
```

### Custom Response Validation

```csharp
public class ErrorOrValidationBehavior<TMail, TResponse> : IPostageMiddleware<TMail, TResponse>
    where TMail : IMail<TResponse>
    where TResponse : IErrorOr
{
    private readonly IValidator<TMail>? _validator;

    public ErrorOrValidationBehavior(IValidator<TMail>? validator = null)
    {
        _validator = validator;
    }

    public async Task<(bool handled, TResponse? result)> StampAsync(TMail mail, Func<TMail, Task<TResponse>> next)
    {
        if (_validator is null)
        {
            return (false, default(TResponse));
        }

        ValidationResult validatorResult = await _validator.ValidateAsync(mail);

        if (validatorResult.IsValid)
        {
            return (false, default(TResponse));
        }

        List<Error> errors = validatorResult.Errors.Select(x => Error.Validation(x.PropertyName, x.ErrorMessage)).ToList();
        return (true, (TResponse)(dynamic)errors);
    }
}

services.AddPostOffice();
services.AddTransient(typeof(IPostageMiddleware<,>), typeof(ErrorOrValidationBehavior<,>));

public record CreateUserCommand(string Name, string Email) : IMail<ErrorOr<string>>;

var result = await poster.Send(new CreateUserCommand("", "invalid-email"));

if (result.IsError)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error.Description}");
    }
}
```

## Middleware

### Adding Middleware

```csharp
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
            
        return (false, result);
    }
}
```

## License

MIT License
## Links

- [NuGet Package](https://www.nuget.org/packages/CQRS.PostOffice/)
- [Source Repository](https://github.com/Desolate1998/PostOffice)
