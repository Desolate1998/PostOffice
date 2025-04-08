using Microsoft.Extensions.DependencyInjection;

namespace PostOffice;

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

        Func<object, Task<object>> handlerDelegate = async (msg) =>
        {
            var result = method.Invoke(handler, [msg]);
            var task = (Task)result!;
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty!.GetValue(task)!;
        };

        var finalResult = await handlerDelegate(mail);
        return (TResponse)finalResult!;
    }
}
