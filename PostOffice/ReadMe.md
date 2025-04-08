# Welcome to the post office
##Getting started

 Register the service
``` sharp
builder.Services.AddPostOffice();
```
Create the command/query
``` csharp
public class DoSomethingCommand:IMail<int>
{
    public int MeowNumber { get; set; }
}
```

Create the handler

``` sharp
public class DoSomethingHandler(ILogger<DoSomethingHandler> logger) : DeliveryAsync<DoSomethingCommand, int>
{
    public override async Task<int> HandleAsync(DoSomethingCommand request)
    {
        logger.LogInformation("Meow number: {MeowNumber}", request.MeowNumber);

        return request.MeowNumber + 10;
    }
}
```
Inject the poster class
```csharp
MeowController(Poster poster) : ControllerBase

```
Send the command
``` csharp

    [HttpGet("Meow")]
    public async Task<IActionResult> Meow([FromHeader]int number)
    {
        DoSomethingCommand command = new DoSomethingCommand()
        {
            MeowNumber = number
        };

        var resp = await poster.Send(command);

        return Ok(resp);
    }
```
