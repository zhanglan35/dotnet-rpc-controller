using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using Castle.DynamicProxy;
using RpcController.Client.Core;
using RpcController.Client.Metadata;
using Microsoft.AspNetCore.Http;
using RpcController.Client.Internal;

namespace RpcController.Client;

/// <summary>
/// Define all methods to handle a call to a RPC Server
/// </summary>
public interface IRpcClientHandler
{
    ProxyGenerator GetProxyGenerator();
    HttpClient CreateHttpClient<TController>(RpcClientOptions options);
    RpcControllerInfo CreateControllerInfo<TController>();
    TController CreateControllerProxy<TController>() where TController : class;
    HttpContext? GetHttpContext();
    (MethodInfo?, object[]?) GetCallingInfo<T>(T proxy, Action<T> callback);
    Task<HttpResponseMessage> SendRequestAsync(CallContext context);
    Task<T?> GetDataAsync<T>(HttpResponseMessage response);
    Task<object?> GetDataAsync(HttpResponseMessage response, Type type);
}

internal class RpcClientHandler : IRpcClientHandler
{
    private readonly ProxyGenerator _proxyGenerator = new();
    private readonly CallMethodInterceptor _callMethodInterceptor = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RpcClientHandler(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor? httpContextAccessor
        )
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public ProxyGenerator GetProxyGenerator()
    {
        return _proxyGenerator;
    }

    public HttpClient CreateHttpClient<TRpcController>(RpcClientOptions options)
    {
        var httpClient = _httpClientFactory.CreateClient(typeof(TRpcController).Name);

        foreach (var hook in options.Hooks)
        {
            hook.Configure(httpClient);
        }

        return httpClient;
    }

    public RpcControllerInfo CreateControllerInfo<TController>()
    {
        return new RpcControllerInfo(typeof(TController));
    }

    public TController CreateControllerProxy<TController>() where TController : class
    {
        return _proxyGenerator.CreateInterfaceProxyWithoutTarget<TController>(_callMethodInterceptor);
    }

    public HttpContext? GetHttpContext()
    {
        return _httpContextAccessor?.HttpContext;
    }

    public (MethodInfo, object[]) GetCallingInfo<T>(T proxy, Action<T> callback)
    {
        try
        {
            callback(proxy);
            throw new CallResultException("No method was called", null);
        }
        catch (CallMethodException ex)
        {
            return (ex.Method, ex.Arguments);
        }
        catch (Exception ex)
        {
            throw new CallResultException("The callback should throw a CallMethodException", null, ex);
        }
    }

    public async Task<HttpResponseMessage> SendRequestAsync(CallContext context)
    {
        try
        {
            foreach (var hook in context.Options.Hooks)
            {
                hook.BeforeRequest(context);
            }

            await context.SendAsync();
        }
        catch (Exception exception)
        {
            throw CallResultException.FailToSendRequest(exception);
        }

        try
        {
            foreach (var hook in context.Options.Hooks)
            {
                hook.AfterResponse(context);
            }

            return context.Response!;
        }
        catch (Exception exception)
        {
            throw CallResultException.FailToProcessResponse(context.Response!, exception);
        }
    }

    const string JsonContentType = MediaTypeNames.Application.Json;

    public async Task<T?> GetDataAsync<T>(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (statusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        try
        {
            string? content = await response.Content.ReadAsStringAsync() ?? string.Empty;

            return contentType switch
            {
                JsonContentType => JsonSerializer.Deserialize<T?>(content, RpcHelper.JsonOptions),
                _ => (T?) (object) content,
            };
        }
        catch (Exception ex)
        {
            throw CallResultException.FailToParseData(response, ex);
        }
    }

    public async Task<object?> GetDataAsync(HttpResponseMessage response, Type type)
    {
        var statusCode = response.StatusCode;
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (statusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        try
        {
            string? content = await response.Content.ReadAsStringAsync() ?? string.Empty;

            return contentType switch
            {
                JsonContentType => JsonSerializer.Deserialize(content, type, RpcHelper.JsonOptions),
                _ => content,
            };
        }
        catch (Exception ex)
        {
            throw CallResultException.FailToProcessResponse(response, ex);
        }
    }

}
