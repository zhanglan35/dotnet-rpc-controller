using Microsoft.AspNetCore.Mvc;

namespace RpcController.Samples.Shared;

/// <summary>
/// Sample RPC service interface.
/// </summary>
[HttpRoute("/sample-rpc")]
public interface ISampleRpcService : IRpcController
{
    /// <summary>
    /// Say hello to the specified name.
    /// </summary>
    [HttpGet("hello")]
    string Hello(string name);
}
