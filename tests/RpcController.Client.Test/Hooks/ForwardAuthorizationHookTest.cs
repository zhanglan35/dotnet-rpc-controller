using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace RpcController.Client.Hooks;

public class ForwardAuthorizationHookTest
{
    static CallContext MockCallContext()
    {
        var httpContext = new DefaultHttpContext();
        var callContext = new CallContext(default!, default!, default!, default!, httpContext);

        return callContext;
    }

    [Fact]
    public void Test_Header_Forwarded()
    {
        var hook = new ForwardAuthorizationHook();
        var callContext = MockCallContext();
        var httpContext = callContext.HttpContext!;
        var expect = "Bearer Test";

        httpContext.Request.Headers.Add(HeaderNames.Authorization, expect);

        hook.BeforeRequest(callContext);

        callContext.Request.Headers.GetValues(HeaderNames.Authorization).First().ShouldBe(expect);
    }

    [Fact]
    public void Test_Header_Override()
    {
        var hook = new ForwardAuthorizationHook();
        var callContext = MockCallContext();
        var httpContext = callContext.HttpContext!;
        var expect = "Bearer Test";

        httpContext.Request.Headers.Add(HeaderNames.Authorization, expect);
        callContext.Request.Headers.Add(HeaderNames.Authorization, "ApiKey Test");

        hook.BeforeRequest(callContext);

        callContext.Request.Headers.GetValues(HeaderNames.Authorization).First().ShouldBe(expect);
    }

    [Fact]
    public void Test_Header_Not_Forwarded()
    {
        var hook = new ForwardAuthorizationHook();
        var callContext = MockCallContext();

        hook.BeforeRequest(callContext);

        callContext.Request.Headers.TryGetValues(HeaderNames.Authorization, out var header);
        header.ShouldBeNull();
    }

}
