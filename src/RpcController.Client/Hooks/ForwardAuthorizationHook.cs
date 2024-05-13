using Microsoft.Net.Http.Headers;

namespace RpcController.Client.Hooks;

internal class ForwardAuthorizationHook : RpcClientHook
{
    public override void BeforeRequest(CallContext context)
    {
        var authorization = context.HttpContext?.Request.Headers[HeaderNames.Authorization];

        if (authorization is not null && authorization.Value.Count > 0)
        {
            context.Request.Headers.Remove(HeaderNames.Authorization);
            context.Request.Headers.Add(HeaderNames.Authorization, authorization.Value.ToArray());
        }
    }
}
