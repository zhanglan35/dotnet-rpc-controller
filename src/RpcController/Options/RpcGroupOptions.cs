using System.Reflection;
using System.Text.Json;

namespace RpcController.Client.Options;

public class RpcGroupOptions
{
    public string? BaseAddress { get; set; }
    public bool ForwardAuthorization { get; set; } = true;
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    private readonly List<Type> _controllers = new();
    public Type[] Controllers => _controllers.ToArray();

    public void AddRpcControllersFromAssembly<T>()
    {
        var assembly = typeof(T).Assembly;

        AddRpcControllersFromAssembly(assembly);
    }

    public void AddRpcControllersFromAssembly(Assembly assembly)
    {
        var controllers = assembly.GetTypes()
            .Where(type => type.IsInterface && typeof(IRpcController).IsAssignableFrom(type))
            .ToArray();

        _controllers.AddRange(controllers);
    }

}
