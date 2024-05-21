using Castle.DynamicProxy;
using RpcController.Client.Core;
using RpcController.Client.Hooks;
using RpcController.Client.Options;

namespace RpcController.Client;

public class RpcClientFactory
{
    private readonly IRpcClientHandler _clientHandler;
    private readonly Dictionary<int, object> _rpcDict = new();
    private readonly Dictionary<int, object> _rpcClientDict = new();

    internal RpcClientFactory(IRpcClientHandler clientHandler, RpcOptionsBuilder builder)
    {
        _clientHandler = clientHandler;
        Configure(builder);
    }

    public static RpcClientFactory Create(Action<RpcOptionsBuilder> configure)
    {
        var httpClientFactory = new RpcHttpClientFactory();
        var handler = new RpcClientHandler(httpClientFactory, null);
        var builder = new RpcOptionsBuilder();

        configure(builder);

        return new RpcClientFactory(handler, builder);
    }

    private void Configure(RpcOptionsBuilder builder)
    {
        var hooks = RpcClientFactoryExtensions.Hooks;

        foreach (var option in builder.Options)
        {
            if (!RpcClientFactoryExtensions.ScopedHooks.TryGetValue(option.GetHashCode(), out var scopedHooks))
            {
                scopedHooks = [];
            }

            var rpcClientOptions = RpcClientOptions.From(option, [..hooks, ..scopedHooks]);

            foreach (var controller in option.Controllers)
            {
                var rpcClientType = typeof(IRpcClient<>).MakeGenericType(controller);

                _rpcDict.Add(controller.MetadataToken, CreateRpc(rpcClientOptions, controller));
                _rpcClientDict.Add(rpcClientType.MetadataToken, CreateRpcClient(rpcClientOptions, controller));
            }
        }
    }

    private object CreateRpc(RpcClientOptions options, Type type)
    {
        var proxyGenerator = _clientHandler.GetProxyGenerator();
        var interceptorType = typeof(RpcClientInterceptor<>).MakeGenericType(type);
        var interceptor = (IInterceptor) Activator.CreateInstance(interceptorType, _clientHandler, options);
        var rpcClient = proxyGenerator.CreateInterfaceProxyWithoutTarget(type, interceptor);

        return rpcClient!;
    }

    private object CreateRpcClient(RpcClientOptions options, Type rpcType)
    {
        var rpcClientType = typeof(RpcClient<>).MakeGenericType(rpcType);

        return Activator.CreateInstance(rpcClientType, _clientHandler, options)!;
    }

    public T Get<T>() where T : IRpcController
    {
        return (T) Get(typeof(T));
    }

    public IRpcClient<T> GetClient<T>() where T : class, IRpcController
    {
        return (IRpcClient<T>) GetClient(typeof(T));
    }

    internal object Get(Type type)
    {
        if (_rpcDict.TryGetValue(type.MetadataToken, out var rpc))
        {
            return rpc;
        }
        else
        {
            throw new TypeLoadException($"RPC: {type.Name} not found");
        }
    }

    internal object GetClient(Type type)
    {
        if (_rpcClientDict.TryGetValue(type.MetadataToken, out var rpcClient))
        {
            return rpcClient;
        }
        else
        {
            throw new TypeLoadException($"RPC Client: {type.Name} not found");
        }
    }
}

internal class RpcHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient = new();

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}


public static class RpcClientFactoryExtensions
{
    internal static List<IRpcClientHook> Hooks { get; private set; } = [];
    internal static Dictionary<int, List<IRpcClientHook>> ScopedHooks { get; private set; } = [];

    /// <summary>
    /// Register hooks for each RpcClient
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="hooks"></param>
    public static void UseHooks(this RpcOptionsBuilder builder, params IRpcClientHook[] hooks)
    {
        Hooks = hooks.ToList();
    }

    /// <summary>
    /// Register hooks for each RpcClient
    /// </summary>
    /// <param name="options"></param>
    /// <param name="hooks"></param>
    public static void UseScopedHooks(this RpcGroupOptions options, params IRpcClientHook[] hooks)
    {
        ScopedHooks.Add(options.GetHashCode(), hooks.ToList());
    }
}
