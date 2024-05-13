using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpcController.Client;
using RpcController.Client.Core;
using RpcController.Client.Options;

namespace RpcController.AspNetCore;

public static class RpcClientExtensions
{
    static List<IRpcClientHook> _hooks = [];

    /// <summary>
    /// Register hooks for each RpcClient
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="hooks"></param>
    public static void UseHooks(this RpcOptionsBuilder builder, params IRpcClientHook[] hooks)
    {
        _hooks = hooks.ToList();
    }

    static Dictionary<int, List<IRpcClientHook>> _scopedHooks = new();

    public static void UseScopedHooks(this RpcControllerOptions options, params IRpcClientHook[] hooks)
    {
        _scopedHooks.Add(options.GetHashCode(), hooks.ToList());
    }

    /// <summary>
    /// Register ControllerClients
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    public static void UseRpcClients(this IServiceCollection services, Action<RpcOptionsBuilder> configure)
    {
        var builder = new RpcOptionsBuilder();

        configure(builder);

        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IRpcClientHandler, RpcClientHandler>();

        foreach (var option in builder.Options)
        {
            var scopedHooks = _scopedHooks.GetValueOrDefault(option.GetHashCode()) ?? [];
            var rpcClientOptions = RpcClientOptions.From(option, [.._hooks, ..scopedHooks]);

            foreach (var controller in option.Controllers)
            {
                services.AddControllerClient(rpcClientOptions, controller);
            }
        }
    }

    /// <summary>
    /// Register TRpcController & RpcClient<TRpcController> into IServiceCollection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <param name="rpcType"></param>
    private static void AddControllerClient(this IServiceCollection services, RpcClientOptions options, Type rpcType)
    {
        var rpcClientType = typeof(RpcClient<>).MakeGenericType(rpcType);
        var rpcClientInterceptorType = typeof(RpcClientInterceptor<>).MakeGenericType(rpcType);

        services.TryAddSingleton(rpcClientType, services =>
        {
            var handler = services.GetRequiredService<IRpcClientHandler>();

            // new RpcClientInterceptor<TRpcController>(handler, options)
            return Activator.CreateInstance(rpcClientType, handler, options)!;
        });

        services.TryAddSingleton(rpcType, services =>
        {
            var handler = services.GetRequiredService<IRpcClientHandler>();
            var proxyGenerator = handler.GetProxyGenerator();
            // new RpcClientInterceptor<TRpcController>(handler, options)
            var interceptor = Activator.CreateInstance(rpcClientInterceptorType, handler, options) as IInterceptor;

            // Use DynamicProxy with interceptor to create TRpcController instance
            return proxyGenerator.CreateInterfaceProxyWithoutTarget(rpcType, interceptor);
        });
    }
}
