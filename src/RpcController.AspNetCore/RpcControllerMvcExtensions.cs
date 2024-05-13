using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RpcController.AspNetCore;

public static class RpcControllerMvcExtensions
{
    /// <summary>
    /// Register IRpcController as Dependency Injection
    /// </summary>
    public static IMvcBuilder AddRpcControllerAsServices(this IMvcBuilder builder, Type? type = null)
    {
        var assembly = type?.Assembly ?? Assembly.GetCallingAssembly();
        var controllers = assembly.GetTypes()
            .Where(x => !x.IsAbstract && !x.IsInterface && x.IsImplement(typeof(IRpcController)))
            .ToArray();

        foreach (var controller in controllers)
        {
            foreach (var @interface in controller.GetInterfaces())
            {
                builder.Services.TryAddScoped(@interface, controller);
            }
        }

        return builder;
    }

    static bool IsImplement(this Type type, Type interfaceType)
    {
        return type.GetInterfaces()
            .Any(x => x.IsGenericType
                ? x.GetGenericTypeDefinition() == interfaceType
                : interfaceType.IsAssignableFrom(type)
            );
    }

}
