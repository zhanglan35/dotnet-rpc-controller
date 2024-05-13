using  RpcController.Client.E2E;
using Microsoft.AspNetCore.Mvc;

namespace RpcController.Client;

/* 重现测试 */
public class RpcClientReproductionTest
{
    private static (RpcClientTestHelper, T, RpcClient<T>) CreateMock<T>() where T : class, IRpcController
    {
        return RpcClientTestHelper.Create<T>();
    }

    [HttpRoute("api/v1")]
    public interface IQueryStringError : IRpcController
    {
        [HttpGet("query")]
        Task<string> QueryAsync(string version, int number);
    }

    /* 2023-06-05 */
    /* Has multiple string type query string only send the first */
    [Fact]
    public async Task QueryStringError_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IQueryStringError>();

        helper.WhenRequest = req =>
        {
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/query");
            req.RequestUri.Query.ShouldBe("?version=123456&number=1");
        };
        helper.ConfigureResponse = res =>
        {
            res.Content = new StringContent("1");
        };

        (await controller.QueryAsync("123456", 1)).ShouldBe("1");
    }
}
