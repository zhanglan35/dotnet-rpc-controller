namespace RpcController.Client;

public interface IRpcClientHook
{
    void Configure(HttpClient httpClient);
    void BeforeRequest(CallContext context);
    void AfterResponse(CallContext context);
}

public abstract class RpcClientHook : IRpcClientHook
{
    public virtual void Configure(HttpClient httpClient)
    {

    }

    public virtual void BeforeRequest(CallContext context)
    {
    }

    public virtual void AfterResponse(CallContext context)
    {
    }
}
