namespace RpcController.Client;

/// <summary>
/// Client Side Call Result
/// </summary>
public class CallResult
{
    public HttpResponseMessage? Response { get; }

    public CallResult(HttpResponseMessage? response)
    {
        Response = response;
    }
}

public class CallResult<T> : CallResult
{
    public T? Data { get; }

    public CallResult(HttpResponseMessage? response, T? data) : base(response)
    {
        Data = data;
    }
}
