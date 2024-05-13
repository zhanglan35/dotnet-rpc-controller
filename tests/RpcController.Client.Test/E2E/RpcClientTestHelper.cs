using Microsoft.AspNetCore.Http;
using RpcController.Client.Core;
using RpcController.Client.Hooks;

namespace RpcController.Client.E2E;

public class RpcClientTestHelper
{
    public Action<HttpRequestMessage> WhenRequest { get; set; } = request => {};
    public Action<HttpResponseMessage> ConfigureResponse { get; set; } = response => response.StatusCode = System.Net.HttpStatusCode.NoContent;
    public HttpResponseMessage Response { get; } = new();

    public (T, RpcClient<T>) Mock<T>() where T : class, IRpcController
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpMessageHandler = new MockHttpMessageHandler(this);
        var handler = new RpcClientHandler(httpClientFactory, httpContextAccessor);
        var options = new RpcClientOptions(new RpcClientHook[]
        {
            new ResolveModelBindingHook(),
            new ConfigureBaseAddressHook("http://localhost:80"),
        });

        httpContextAccessor.HttpContext = default;
        httpClientFactory
            .CreateClient(Arg.Any<string>())
            .Returns(new HttpClient(httpMessageHandler));

        var interceptor = new RpcClientInterceptor<T>(handler, options);
        var controller = handler.GetProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(interceptor);
        var controllerClient = new RpcClient<T>(handler, options);

        return (controller, controllerClient);
    }

    // Static Helper
    public static (RpcClientTestHelper, T, RpcClient<T>) Create<T>()
        where T : class, IRpcController
    {
        var helper = new RpcClientTestHelper();
        var (controller, controllerClient) = helper.Mock<T>();

        return (helper, controller, controllerClient);
    }

}

class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly RpcClientTestHelper _helper;

    public MockHttpMessageHandler(RpcClientTestHelper helper)
    {
        _helper = helper;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _helper.WhenRequest(request);
        _helper.ConfigureResponse(_helper.Response);

        return Task.FromResult(_helper.Response);
    }
}
