using RpcController.Client.Metadata;
using Microsoft.AspNetCore.Http;

namespace RpcController.Client;

/// <summary>
/// CallContext should contains all information needed to make a call to a RPC Server.
/// - Method: RPC Method defined in IRpcController
/// - Arguments: Runtime arguments when calling the method
/// - HttpContext: Current HttpContext if running in a ASP.NET Core application
///     To provide some useful features like pass current Authorization Token
///     Referencfe: https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/http-context
/// - Request, Response, SendAsync: Stateful properties to make the call
/// </summary>
public record CallContext
{
    internal HttpClient HttpClient { get; }
    public RpcClientOptions Options { get; }
    public RpcMethodInfo Method { get; }
    public object[]? Arguments { get; }
    public HttpContext? HttpContext { get; }
    public HttpRequestMessage Request { get; }
    public HttpResponseMessage? Response { get; private set; }

    internal CallContext(
        HttpClient httpClient, RpcClientOptions options,
        RpcMethodInfo method, object[]? arguments, HttpContext? httpContext
        )
    {
        HttpClient = httpClient;
        Options = options;
        Method = method;
        Arguments = arguments;
        HttpContext = httpContext;
        Request = new HttpRequestMessage();
    }

    public async Task<HttpResponseMessage> SendAsync()
    {
        return Response = await HttpClient.SendAsync(Request);
    }
}
