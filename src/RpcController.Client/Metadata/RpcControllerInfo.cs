using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace RpcController.Client.Metadata;

public class RpcControllerInfo
{
    public Type ControllerType { get; }
    public RouteAttribute RouteAttribute { get; }
    public IReadOnlyDictionary<int, RpcMethodInfo> MethodDict { get; }
    public IReadOnlyList<RpcMethodInfo> Methods { get; }

    public RpcControllerInfo(Type controllerType)
    {
        ControllerType = controllerType;
        RouteAttribute = controllerType.GetCustomAttributes()
            .FirstOrDefault(x => x is RouteAttribute)as RouteAttribute
                ?? throw new ArgumentException("IRpcController must have only one RouteAttribute");
        Methods = controllerType.GetMethods()
            .Select(x => new RpcMethodInfo(this, x))
            .ToArray();
        MethodDict = Methods.ToDictionary(x => x.MethodInfo.MetadataToken);
    }
}
