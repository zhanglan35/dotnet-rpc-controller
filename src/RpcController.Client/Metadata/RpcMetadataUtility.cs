using RpcController.Client.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;

namespace RpcController.Client.Metadata;

internal static class RpcMetadataUtility
{
    public static string GetTemplate(RouteAttribute route, HttpMethodAttribute method)
    {
        var routeTemplate = route.Template?.Trim('/') ?? "";
        var methodTemplate = method.Template?.TrimEnd('/') ?? "";

        if (methodTemplate?.StartsWith("/") == true)
        {
            return methodTemplate;
        }
        else
        {
            return string.Format("/{0}/{1}", routeTemplate, methodTemplate);
        }
    }

    public static HttpMethod GetMethod(HttpMethodAttribute method)
    {
        return new HttpMethod(method.HttpMethods.First());
    }

    public static BindingInfo GetBindingInfo(RpcParameterInfo parameter)
    {
        BindingInfo bindingInfo;
        var parameterName = parameter.ParameterInfo.Name;
        var parameterType = parameter.ParameterInfo.ParameterType;
        var bindingSource = parameter.ParameterInfo.GetCustomAttributes(true)
            .FirstOrDefault(x => x is IBindingSourceMetadata);
        var bindingSourceName = bindingSource?.GetType()
            .GetProperty("Name")?.GetValue(bindingSource)?.ToString();

        if (bindingSource is not null)
        {
            bindingInfo = BindingInfo.GetBindingInfo([bindingSource]);
        }
        else if (parameter.MethodInfo.Template.Contains("{" + parameterName + "}"))
        {
            var attribute = new FromRouteAttribute();
            bindingInfo = BindingInfo.GetBindingInfo([attribute]);
        }
        else if (
            typeof(IFormFile).IsAssignableFrom(parameterType) ||
            typeof(IEnumerable<IFormFile>).IsAssignableFrom(parameterType)
            )
        {
            var attribute = new FromFormAttribute();
            bindingInfo = BindingInfo.GetBindingInfo([attribute]);
            bindingInfo.BindingSource = BindingSource.FormFile;
        }
        else if (
            IsMethodSupportBody(parameter.MethodInfo.HttpMethod.Method) &&
            !ModelBindingHelper.IsSimpleType(parameterType)
            )
        {
            var attribute = new FromBodyAttribute() {};
            bindingInfo = BindingInfo.GetBindingInfo([attribute]);
        }
        else
        {
            var attribute = new FromQueryAttribute();
            bindingInfo = BindingInfo.GetBindingInfo([attribute]);
        }

        if (
            bindingSource is not FromBodyAttribute &&
            bindingSource is not FromFormAttribute &&
            string.IsNullOrWhiteSpace(bindingSourceName)
            )
        {
            bindingInfo.BinderModelName = parameterName;
        }

        return bindingInfo;
    }

    private static bool IsMethodSupportBody(string method)
    {
        return method == "POST" ||
            method == "PUT" ||
            method == "PATCH";
    }
}
