using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RpcController.Client.Metadata;

public class RpcParameterInfo
{
    public RpcControllerInfo ControllerInfo { get; }
    public RpcMethodInfo MethodInfo { get; }
    public ParameterInfo ParameterInfo { get; }
    public BindingInfo BindingInfo { get; }
    public bool IsEnumerable { get; }
    public bool IsGenericType { get; }
    public Type ParameterType { get; }
    public Type? EnumerateType { get; }
    public string BinderModelName => BindingInfo.BinderModelName ?? ParameterInfo.Name;

    public RpcParameterInfo(RpcControllerInfo controllerInfo, RpcMethodInfo methodInfo, ParameterInfo parameterInfo)
    {
        ControllerInfo = controllerInfo;
        MethodInfo = methodInfo;
        ParameterInfo = parameterInfo;
        BindingInfo = RpcMetadataUtility.GetBindingInfo(this);
        IsEnumerable = typeof(IEnumerable).IsAssignableFrom(parameterInfo.ParameterType);
        IsGenericType = parameterInfo.ParameterType.IsGenericType;
        ParameterType = parameterInfo.ParameterType;

        if (IsEnumerable && IsGenericType && !ParameterType.Equals(typeof(string)))
        {
            EnumerateType = parameterInfo.ParameterType.GetGenericArguments().First();
        }
        else if (IsEnumerable && ParameterType.IsArray)
        {
            EnumerateType = ParameterType.GetElementType();
        }
    }
}
