using System.Reflection;
using System.Text.Json;

namespace RpcController.Client.Options;

public class RpcControllerOptions
{
    public string? BaseAddress { get; set; }
    public bool ForwardAuthorization { get; set; } = true;
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    public Type[] Controllers { get; private set; } = Type.EmptyTypes;

    public void AddRpcControllersFromAssembly<T>()
    {
        var assembly = typeof(T).Assembly;

        AddRpcControllersFromAssembly(assembly);
    }

    public void AddRpcControllersFromAssembly(Assembly assembly)
    {
        Controllers = assembly.GetTypes()
            .Where(type => type.IsInterface && typeof(IRpcController).IsAssignableFrom(type))
            .ToArray();
    }

}
