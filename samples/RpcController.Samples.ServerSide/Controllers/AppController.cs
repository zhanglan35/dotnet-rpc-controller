using Microsoft.AspNetCore.Mvc;

namespace RpcController.Samples.ServerSide.Controllers;

[Route("/")]
public class AppController
{
    [HttpGet("/home")]
    public string App()
    {
        return "RPCServerSide API";
    }
}
