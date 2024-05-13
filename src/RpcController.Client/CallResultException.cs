namespace RpcController.Client;

/// <summary>
/// Client Side Call Result Exception
/// </summary>
public class CallResultException : Exception
{
    public HttpResponseMessage? Response;

    public CallResultException(string msg, HttpResponseMessage? response, Exception? innerException = null) : base(msg, innerException)
    {
        Response = response;
    }

    public static CallResultException InvalidResponseStatus(HttpResponseMessage response)
    {
        var msg = string.Format("Invalid rpc response status code: {0}", response.StatusCode);

        return new CallResultException(msg, response);
    }

    public static CallResultException FailToSendRequest(Exception exception)
    {
        var msg = string.Format("Fail to send RPC request, error: {0}", exception.Message);

        return new CallResultException(msg, null, exception);
    }

    public static CallResultException FailToProcessResponse(HttpResponseMessage response, Exception ex)
    {
        var msg = string.Format("Fail to process RPC response, error: {0}", ex.Message);

        return new CallResultException(msg, response, ex);
    }

    public static CallResultException FailToParseData(HttpResponseMessage response, Exception ex)
    {
        var msg = string.Format("Fail to parse RPC response data, error: {0}", ex.Message);

        return new CallResultException(msg, response, ex);
    }

}
