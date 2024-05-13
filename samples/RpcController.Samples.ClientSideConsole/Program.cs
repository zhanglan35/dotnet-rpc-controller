using RpcController.Client;
using RpcController.Samples.Shared;

namespace RpcController.Samples.ClientSideConsole;

public static class Program
{
    public static void Main()
    {
        var factory = RpcClientFactory.Create(rpc =>
        {
            rpc.AddGroup(options =>
            {
                options.BaseAddress = "http://localhost:5080";
                options.AddRpcControllersFromAssembly<ISampleRpcService>();
            });
        });

        try
        {
            var client = factory.Get<ISampleRpcService>();
            var result = client.FromQuery(100, [ "value21", "value22" ], false, new(2000, 1, 1));

            Console.WriteLine(result);
        }
        catch (CallResultException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
