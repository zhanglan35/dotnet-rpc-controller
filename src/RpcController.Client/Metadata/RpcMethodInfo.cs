using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RpcController.Client.Metadata;

public class RpcMethodInfo
{
    public RpcControllerInfo ControllerInfo { get; }
    public MethodInfo MethodInfo { get; }
    public HttpMethodAttribute? MethodAttribute { get; }
    public Type? ReturnType { get; }
    public bool IsAsync { get; }
    public string Template { get; }
    public HttpMethod HttpMethod { get; }
    public IReadOnlyList<RpcParameterInfo> Parameters { get; }

    public RpcMethodInfo(RpcControllerInfo controllerInfo, MethodInfo methodInfo)
    {
        ControllerInfo = controllerInfo;
        MethodInfo = methodInfo;
        MethodAttribute = methodInfo.GetCustomAttributes()
            .FirstOrDefault(x => x is HttpMethodAttribute) as HttpMethodAttribute
            ?? throw new ArgumentException("Method must have a HttpMethodAttribute");
        Template = RpcMetadataUtility.GetTemplate(ControllerInfo.RouteAttribute, MethodAttribute);
        HttpMethod = RpcMetadataUtility.GetMethod(MethodAttribute);
        ReturnType = methodInfo.ReturnType;
        IsAsync = typeof(Task).IsAssignableFrom(ReturnType);
        Parameters = methodInfo.GetParameters()
            .Select(x => new RpcParameterInfo(controllerInfo, this, x))
            .ToArray();

        if (IsAsync)
        {
            ReturnType = ReturnType.GetGenericArguments().FirstOrDefault();
        }
    }
}
