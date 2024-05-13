using RpcController.Client.Metadata;

namespace RpcController.Client;

/// <summary>
/// Use ControllerClient to call methods inside IRpcController
/// Return CallResult if success
/// Throw CallResultException if failed
/// </summary>
public interface IRpcClient<TRpcController> where TRpcController : class, IRpcController
{
    Task<CallResult> CallAsync(Action<TRpcController> method);
    Task<CallResult<T>> CallAsync<T>(Func<TRpcController, T> method);
    Task<CallResult> CallAsync(Func<TRpcController, Task> method);
    Task<CallResult<T>> CallAsync<T>(Func<TRpcController, Task<T>> method);
}

public class RpcClient<TController> : IRpcClient<TController>
    where TController : class, IRpcController
{
    private readonly HttpClient _httpClient;
    private readonly TController _controllerProxy;
    private readonly RpcControllerInfo _controllerInfo;
    private readonly IRpcClientHandler _handler;
    private readonly RpcClientOptions _options;

    public RpcClient(IRpcClientHandler handler, RpcClientOptions options)
    {
        _handler = handler;
        _options = options;
        _controllerProxy = handler.CreateControllerProxy<TController>();
        _httpClient = handler.CreateHttpClient<TController>(options);
        _controllerInfo = handler.CreateControllerInfo<TController>();
    }

    public async Task<HttpResponseMessage> SendRequestAsync(Action<TController> method)
    {
        var (methodInfo, arguments) = _handler.GetCallingInfo(_controllerProxy, method);
        var controllerMethod = _controllerInfo.MethodDict[methodInfo!.MetadataToken];
        var callContext = new CallContext(
            httpClient: _httpClient,
            arguments: arguments,
            options: _options,
            method: controllerMethod,
            httpContext: _handler.GetHttpContext()
        );

        return await _handler.SendRequestAsync(callContext);
    }

    private CallResult CreateCallResult(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return new CallResult(response);
        }
        else
        {
            var msg = string.Format("StatusCode {0} is not successful", response.StatusCode);

            throw new CallResultException(msg, response);
        }
    }

    private async Task<CallResult<T>> CreateCallResultAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var msg = string.Format("StatusCode {0} is not successful", response.StatusCode);

            throw new CallResultException(msg, response);
        }

        return new CallResult<T>(response, await _handler.GetDataAsync<T>(response));
    }

    public async Task<CallResult> CallAsync(Action<TController> method)
    {
        var response = await SendRequestAsync(method);

        return CreateCallResult(response);
    }

    public async Task<CallResult<T>> CallAsync<T>(Func<TController, T> method)
    {
        var response = await SendRequestAsync(x => method(x));

        return await CreateCallResultAsync<T>(response!);
    }

    public async Task<CallResult> CallAsync(Func<TController, Task> method)
    {
        return await CallAsync(x => method(x).GetAwaiter().GetResult());
    }

    public async Task<CallResult<T>> CallAsync<T>(Func<TController, Task<T>> method)
    {
        return await CallAsync(x => method(x).GetAwaiter().GetResult());
    }
}
