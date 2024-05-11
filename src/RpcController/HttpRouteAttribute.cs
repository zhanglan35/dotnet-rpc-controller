using Microsoft.AspNetCore.Mvc;

namespace RpcController;

/// <summary>
/// RouteAttribute could only be used on class
/// This is a workaround to allow RouteAttribute on interface
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class HttpRouteAttribute : RouteAttribute
{
    public HttpRouteAttribute(string template) : base(template)
    {
    }
}
