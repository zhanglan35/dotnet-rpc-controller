using System.Collections;
using RpcController.Client.Internal;
using RpcController.Client.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RpcController.Client.Hooks;

/// <summary>
/// Process ModelBinding in the client side
/// </summary>
internal class ResolveModelBindingHook : RpcClientHook
{
    public override void BeforeRequest(CallContext context)
    {
        var methodInfo = context.Method;
        var arguments = context.Arguments ?? [];
        var url = methodInfo.Template;
        var method = methodInfo.HttpMethod;
        var request = context.Request;
        var queryString = QueryString.Empty;
        var formData = default(MultipartFormDataContent);

        for (var i = 0; i < arguments.Length; i++)
        {
            // Asserts that invocation.Arguments and methodInfo.Parameters has same order
            var argument = arguments[i];
            var parameter = methodInfo.Parameters[i];
            var bindingInfo = parameter.BindingInfo;
            var bindingSource = bindingInfo.BindingSource;

            if (bindingSource == BindingSource.Path)
            {
                ResolveRouteBinding(parameter, ref url, argument);
            }
            else if (bindingSource == BindingSource.Header)
            {
                ResolveHeaderBinding(parameter, request, argument);
            }
            else if (bindingSource == BindingSource.Form)
            {
                ResolveFormBinding(parameter, ref formData, argument);
            }
            else if (bindingSource == BindingSource.FormFile)
            {
                ResolveFormFileBinding(parameter, ref formData, argument);
            }
            else if (bindingSource == BindingSource.Body)
            {
                ResolveBodyBinding(parameter, request, argument);
            }
            else if (bindingSource == BindingSource.Query)
            {
                ResolveQueryBinding(parameter, out queryString, argument);
            }
        }

        request.RequestUri = new Uri(url + queryString.ToUriComponent(), uriKind: UriKind.Relative);
        request.Method = method;
        request.Headers.TryAddWithoutValidation("accept", "*/*");

        if (formData is not null && request.Content is null)
        {
            request.Content = formData;
        }
    }

    /* 处理 Form 参数绑定 */
    private void ResolveFormBinding(RpcParameterInfo parameter, ref MultipartFormDataContent? content, object argument)
    {
        if (ModelBindingHelper.IsSimpleType(argument.GetType()))
        {
            content ??= new();
            content.Add(new StringContent(argument?.ToString()), parameter.BinderModelName);
        }
        else
        {
            throw new NotSupportedException("不支持复杂类型的 Form 参数绑定");
        }
    }

    /* 处理 Router 参数绑定 */
    private void ResolveRouteBinding(RpcParameterInfo parameter, ref string url, object argument)
    {
        url = url.Replace("{" + parameter.BinderModelName + "}", argument.ToString());
    }

    /* 处理 Header 参数绑定 */
    private void ResolveHeaderBinding(RpcParameterInfo parameter, HttpRequestMessage request, object argument)
    {
        request.Headers.Add(parameter.BinderModelName, argument.ToString());
    }

    /* 处理 IFormFile 参数绑定 */
    private void ResolveFormFileBinding(RpcParameterInfo parameter, ref MultipartFormDataContent? content, object argument)
    {
        IEnumerable<IFormFile> files;

        content ??= new();

        if (argument is IEnumerable<IFormFile> fileArray)
        {
            files = fileArray;
        }
        else if (argument is IFormFile file)
        {
            files = [file];
        }
        else
        {
            throw new InvalidDataException(
                $"FormFile binding({parameter.BinderModelName}) must use IFormFile or IEnumerable<IFormFile> type."
            );
        }

        foreach (var file in files)
        {
            var stream = new StreamContent(file!.OpenReadStream());

            if (file.Headers is not null && file.ContentType is not null)
            {
                stream.Headers.ContentType = new(file.ContentType);
            }

            content.Add(stream, parameter.BinderModelName, file.FileName);
        }
    }

    /* 处理 Body 参数绑定 */
    private void ResolveBodyBinding(RpcParameterInfo parameter, HttpRequestMessage request, object argument)
    {
        var data = System.Text.Json.JsonSerializer.Serialize(argument);
        var content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");

        request.Content = content;
    }

    /* 处理 Query 参数绑定 */
    private void ResolveQueryBinding(RpcParameterInfo parameter, out QueryString queryString, object argument)
    {
        if (argument is not null && argument is not string && typeof(IEnumerable).IsAssignableFrom(argument.GetType()))
        {
            foreach (var item in (IEnumerable) argument)
            {
                ResolveQueryBinding(parameter, out queryString, item);
            }
        }
        else
        {
            string queryValue;

            if (argument is null)
            {
                queryValue = "";
            }
            else if (argument.GetType().IsEnum)
            {
                queryValue = ((int) argument).ToString();
            }
            else if (argument.GetType().IsAssignableFrom(typeof(DateTime)))
            {
                queryValue = ((DateTime) argument).ToString();
            }
            else
            {
                queryValue = argument.ToString();
            }

            queryString = queryString.Add(parameter.BinderModelName, queryValue);
        }
    }
}
