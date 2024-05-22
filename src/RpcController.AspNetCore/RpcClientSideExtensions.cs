using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpcController.Client;
using RpcController.Options;

namespace RpcController.AspNetCore;

public static class RpcClientSideExtensions
{
    /// <summary>
    /// Register ControllerClients
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    public static void UseRpcClients(this IServiceCollection services, Action<RpcOptionsBuilder> configure)
    {
        var builder = new RpcOptionsBuilder();

        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IRpcClientHandler, RpcClientHandler>();

        configure(builder);

        services.TryAddSingleton(services =>
        {
            return new RpcClientFactory(services.GetRequiredService<IRpcClientHandler>(), builder);
        });

        foreach (var option in builder.Options)
        {
            foreach (var controller in option.Controllers)
            {
                var rpcType = controller;

                var rpcClientType = typeof(IRpcClient<>).MakeGenericType(controller);

                services.TryAddSingleton(rpcType, services =>
                {
                    var factory = services.GetRequiredService<RpcClientFactory>();

                    return factory.Get(rpcType);
                });

                services.TryAddSingleton(rpcClientType, services =>
                {
                    var factory = services.GetRequiredService<RpcClientFactory>();

                    return factory.GetClient(rpcType);
                });
            }
        }
    }
}
