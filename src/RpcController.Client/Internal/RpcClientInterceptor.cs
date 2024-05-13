using System.Reflection;
using Castle.DynamicProxy;
using RpcController.Client.Metadata;

namespace RpcController.Client.Core;

/// <summary>
/// TController Dynamic Proxy Interceptor
/// </summary>
internal class RpcClientInterceptor<TRpcController> : IInterceptor where TRpcController : class, IRpcController
{
    /* Design Details */
    // 1. Use Castle Dynamic Proxy to intercept the calling method of TController
    // 2. Use ICallControllerHandler to process public calling logic
    // 3. Use ControllerClientOptions to configure custom logic
    // 4. Use _invokers to process different return type: Sync、Task、Task<T>
    // 5. Castle cannot process the return value of Task<T>, so use TaskCompletionSource<T> as a workround

    /* Performance Details */
    // 1. Use space to exchange time, build all the calling handler in the constructor.
    // 2. Use the invokers to avoid reflection to generate TaskCompletionSource<T> every time.

    private readonly HttpClient _httpClient;
    private readonly RpcControllerInfo _controllerInfo;
    private readonly IRpcClientHandler _handler;
    private readonly RpcClientOptions _options;
    private readonly Dictionary<MethodInfo, Action<IInvocation>> _invokers;

    public RpcClientInterceptor(IRpcClientHandler handler, RpcClientOptions options)
    {
        _handler = handler;
        _options = options;
        _httpClient = handler.CreateHttpClient<TRpcController>(options);
        _controllerInfo = handler.CreateControllerInfo<TRpcController>();
        _invokers = _controllerInfo.Methods.ToDictionary(x => x.MethodInfo, GenerateInvoker);
    }

    public void Intercept(IInvocation invocation)
    {
        _invokers[invocation.Method](invocation);
    }

    private Action<IInvocation> GenerateInvoker(RpcMethodInfo methodInfo)
    {
        if (!methodInfo.IsAsync)
        {
            return invocation => invocation.ReturnValue = InterceptAsync(invocation).GetAwaiter().GetResult();
        }
        else if (methodInfo.ReturnType is null)
        {
            return invocation => invocation.ReturnValue = InterceptAsync(invocation);
        }
        else
        {
            var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(methodInfo.ReturnType);
            var tcsTypeTaskProperty = tcsType.GetProperty("Task")!;
            var tcsTypeSetResultMethod = tcsType.GetMethod("SetResult")!;
            var tcsTypeSetExceptionMethod = tcsType.GetMethod("SetException", [typeof(Exception)])!;

            return invocation =>
            {
                var tcs = Activator.CreateInstance(tcsType);
                invocation.ReturnValue = tcsTypeTaskProperty.GetValue(tcs, null);

                InterceptAsync(invocation).ContinueWith(async task =>
                {
                    try
                    {
                        var value = await task;
                        tcsTypeSetResultMethod.Invoke(tcs, [value]);
                    }
                    catch (Exception ex)
                    {
                        tcsTypeSetExceptionMethod.Invoke(tcs, [ex]);
                    }
                });
            };
        }
    }

    private async Task<object?> InterceptAsync(IInvocation invocation)
    {
        var method = _controllerInfo.MethodDict[invocation.Method.MetadataToken];
        var callContext = new CallContext(
            httpClient: _httpClient,
            arguments: invocation.Arguments,
            options: _options,
            method: method,
            httpContext: _handler.GetHttpContext()
        );
        var response = await _handler.SendRequestAsync(callContext);

        if (response!.IsSuccessStatusCode)
        {
            if (method.ReturnType is null)
            {
                return null;
            }

            return await _handler.GetDataAsync(response, method.ReturnType!);
        }
        else
        {
            throw CallResultException.ErrorResponseStatus(response);
        }
    }
}
