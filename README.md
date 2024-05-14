<p align="center">
    <span>中文</span> |
    <a href="README.en-US.md">English</a>
</p>

# RpcController

基于 `ASP.NET Core Controller` 的 .NET RPC 框架。

- [简介](#简介)
- [快速开始](#快速开始)
- [支持的 Attributes](#支持的-attributes)
- [客户端异常处理](#客户端异常处理)
- [扩展性](#扩展性)

## 简介

**为什么需要这个 Library**

假设有两个服务 A 和 B，服务 B 需要调用服务 A 的某些 api。

这应该是基于 `ASP.NET Core` 最简单的解决方案：

```
ProjectFolder/
|
├── ServiceA/
|   ├── SampleRpcController.cs    <-- 实现 RPC 接口 (服务端)
|   ├── Program.cs
|   └── ...
|
├── ServiceA.Shared/
|   ├── ISampleRpcService.cs      <-- 定义 RPC 接口 (公共项目)
|   └── ...
|
├── ServiceB/
|   ├── AppController.cs          <-- 调用 RPC 接口 (客户端)
|   ├── Program.cs
|   └── ...
```

只需遵循以下步骤:

- 使用 `ASP.NET Core` 中的 `Attributes` 定义一个 RPC 接口

``` C#
// ServiceA.Shared/ISampleRpcService.cs
[HttpRoute("/api/v1/public")]
public interface ISampleRpcService : IRpcController
{
    [HttpGet("int")]
    int Add(int a, int b);
}
```

- 在服务 A 中用 `Controller` 实现 RPC 接口 (服务器端)

``` C#
// ServiceA/SampleRpcService.cs
public class SampleRpcController : ControllerBase, ISampleRpcService
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

- 在服务 B 中通过 `RPC 接口` 调用 RPC 服务 (客户端)

``` C#
// ServiceB/AppController.cs
public class AppController
{
    private readonly ISampleRpcService _sampleRpcService;

    public AppController(ISampleRpcService sampleRpcService)
    {
        _sampleRpcService = sampleRpcService;
    }

    public int CallRPC()
    {
        return _sampleRpcService.Add(1, 2);
    }
}
```

最重要的是将 `RPC 接口` 定义在一个公共项目中，这个接口被服务端和客户端项目共同引用。`RPC 接口` 看起来几乎就像是 `ASP.NET Core` 中的 `Controller`，这也是为什么这个库被称为 `RpcController`。

与其他 RPC 框架（gRPC、Orleans 等）相比，这个库没有引入任何新概念。

服务端可以使用几乎全部 ASP.NET Core 所支持的特性：

    - Controler
    - Middleware
    - Exception Handler
    - Authroize
    - Swagger Integration
    - ...

## 快速开始

### 安装 Nuget 依赖

``` shell
# in Shared project
dotnet add package RpcController

# in ASP.NET Core project (SerderSide or ClientSide)
dotnet add package RpcController.AspNetCore

# in other ClientSide project (like Console program)
dotnet add package RpcController.Client
```

### 服务器配置

``` C#
// In ServiceA/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RpcServerSideConvention());
});

builder.Services.AddSwaggerGen(options =>
{
    options.IncludeApplicationXmlComments(); // 根据需要添加 XML 注释
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

### 客户端配置

```C#
// ServiceB/Program.cs
var builder = WebApplication.CreateBuilder(args);

// 配置 RpcClients
builder.Services.UseRpcClients(rpc =>
{
    rpc.AddGroup(options =>
    {
        options.BaseAddress = "http://localhost:5080";
        options.AddRpcControllersFromAssembly<ISampleRpcService>();
    });

    // 注册其他 rpc 服务 ...
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

## 支持的 Attributes

支持绝大多数 `HttpMethod` 和 `BindingSource`，例如: `HttpGet`, `HttpPost`, `FromQuery`, `FormRoute`, `FromBody` 等。

可以参考 [ISampleRpcService.cs](/samples/RpcController.Samples.Shared/ISampleRpcService.cs)

## 客户端异常处理

如果发生某些错误，如网络故障、数据异常、错误的业务行为等，RPC 客户端将抛出 `CallResultException。`

```C#

var rpcClient = rpcClientFactory.Get<ISomeRpcService>();

try
{
    var result = rpcClient.DoSomething();
}
catch (CallResultException ex)
{
    // 处理异常
}
```

如果 RPC 服务器返回了响应，`CallResultException.Response` 将会提供对应的 `HttpResponseMessage`。

在许多情况下，你应该让错误尽可能抛出并使用 `ExceptionHandler` 统一处理：

``` C#
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var exceptionHandlerPathFeature =context.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error is CallResultException callResultError)
        {
            context.Response.StatusCode = (int) callResultError.Response.StatusCode;
            context.Response.ContentType = callResultError.Response.ContentType;
            await context.Response.WriteAsync(callResultError.Response.ReadAsStringAsync());
        }

        // 处理其他错误
    });
});

```

这样的话，当你有一个复杂的 `调用链`，如 A => B => C => D => E => F，然后 F 抛出错误，
只要每个服务中都用这个 `ExceptionHandler`，错误将被传播到 A，而无需在服务 BCDE 中进行任何特定处理。

## 扩展性

服务器端的行为与 `ASP.NET Core` 基本一致，可以延用已有的特性进行扩展。

客户端可以实现自定义的 `IRpcClientHook` 进行扩展：

``` C#

public abstract class MyRpcClientHook : IRpcClientHook
{
    public virtual void Configure(HttpClient httpClient)
    {
        // before the RPC Client create
    }

    public virtual void BeforeRequest(CallContext context)
    {
        // before the RPC request send
    }

    public virtual void AfterResponse(CallContext context)
    {
        // after the RPC request send
    }
}
```

在处理请求和响应时，可以在 `CallContext` 中访问 `HttpRequestMessage` 和 `HttpResponseMessage`。

接下来，将已定义的 `IRpcClientHook` 进行注册

``` C#
builder.Services.UseRpcClients(rpc =>
{
    rpc.UseHooks([ new MyRpcClientHook() ])                         // 对全局都生效
    rpc.AddGroup(options =>
    {
        options.UseScopedScopes([ new MyRpcClientHoo() ]);         // 只对该 Group 生效
        options.BaseAddress = "http://localhost:5080";
        options.AddRpcControllersFromAssembly<ISampleRpcService>();
    });
});
```
