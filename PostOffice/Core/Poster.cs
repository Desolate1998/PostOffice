using Microsoft.Extensions.DependencyInjection;
using PostOffice.Middleware;

namespace PostOffice.Core;

public class Poster(IServiceProvider provider)
{
    public async Task<TResponse> Send<TResponse>(IMail<TResponse> mail)
    {
        var mailType = mail.GetType();
        var responseType = typeof(TResponse);

        var handlerType = typeof(DeliveryAsync<,>).MakeGenericType(mailType, responseType);
        var handler = provider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod("HandleAsync")
                     ?? throw new InvalidOperationException($"HandleAsync not found on {handlerType.Name}");

        // Create the final handler delegate
        Func<object, Task<TResponse>> finalHandler = async (msg) =>
        {
            var result = method.Invoke(handler, [msg]);
            var task = (Task)result!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return (TResponse)resultProperty!.GetValue(task)!;
        };

        // Try to get middleware pipeline, if none registered, call handler directly
        var pipelineType = typeof(IMiddlewarePipeline<,>).MakeGenericType(mailType, responseType);
        var pipeline = provider.GetService(pipelineType);

        if (pipeline != null)
        {
            var executeMethod = pipelineType.GetMethod("ExecuteAsync")
                               ?? throw new InvalidOperationException($"ExecuteAsync not found on {pipelineType.Name}");

            var pipelineDelegate = (Func<object, Task<TResponse>>)(async (msg) => (TResponse)await finalHandler(msg));
            var result = executeMethod.Invoke(pipeline, [mail, pipelineDelegate]);
            var task = (Task<TResponse>)result!;
            return await task;
        }

        return await finalHandler(mail);
    }
}
