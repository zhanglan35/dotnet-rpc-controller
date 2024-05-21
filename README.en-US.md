<p align="center">
    <a href="README.md">中文</a> |
    <span>English</span>
</p>

# RpcController

.NET RPC Framework based on `ASP.NET Core Controller`.

- [Introduction](#introduction)
- [Quick Start](#quick-start)
- [Supported Attributes](#supported-attributes)
- [Client Side Exception Handler](#client-side-exception-handler)
- [Extensibility](#extensibility)

## Introduction

**Why need this library**

There are two service A and B, service B needs to call some api of service A.

It should be the simplest solution with `ASP.NET Core`:

```
ProjectFolder/
|
├── ServiceA/
|   ├── SampleRpcController.cs    <-- Implement RPC via interface (Server Side)
|   ├── Program.cs
|   └── ...
|
├── ServiceA.Shared/
|   ├── ISampleRpcService.cs      <-- Define RPC interface (Shared Project)
|   └── ...
|
├── ServiceB/
|   ├── AppController.cs          <-- Call RPC via interface (Client Side)
|   ├── Program.cs
|   └── ...
```

Just follow the feeling:

- Define a `RPC Interface` with some `Attributes` provided by `ASP.NET Core`

``` C#
// In ServiceA.Shared/ISampleRpcService.cs
[HttpRoute("/api/v1/public")]
public interface ISampleRpcService : IRpcController
{
    [HttpGet("int")]
    int Add(int a, int b);
}
```

- Implement the `RPC Interface` in Service A with Controller (Server Side)

``` C#
// In ServiceA/SampleRpcService.cs
public class SampleRpcController : ControllerBase, ISampleRpcService
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

- Call the RPC Server via `RPC Interface` in Service B (Client Side)

``` C#
// In ServiceB/AppController.cs
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

The most important thing is to define a `RPC interface` into a shared project that will be shared between ServerSide and ClientSide. The `RPC interface` looks almost like a `Controller` in `ASP.NET Core`, that's why this library called `RpcController`.

Compared to other RPC frameworks (gRPC, Orleans, etc), this library doesn't bring any new concepts.
The ServerSide can use most of ASP.NET Core features:

    - Controler
    - Middleware
    - Exception Handler
    - Authroize
    - Swagger Integration
    - etc

## Quick Start

### Install Package

``` shell
# in Shared project
dotnet add package RpcController

# in ASP.NET Core project (SerderSide or ClientSide)
dotnet add package RpcController.AspNetCore

# in other ClientSide project (like Console program)
dotnet add package RpcController.Client
```

### ServerSide setup

``` C#
// In ServiceA/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RpcServerSideConvention());
});

builder.Services.AddSwaggerGen(options =>
{
    options.IncludeRpcControllerXmlComments(); // Add XML comments as needed
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

### ClientSide setup (ASP.NET Core WebApi)

```C#
// In ServiceB/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure RpcClients by options
builder.Services.UseRpcClients(rpc =>
{
    rpc.AddGroup(options =>
    {
        options.BaseAddress = "http://localhost:5080";
        options.AddRpcControllersFromAssembly<ISampleRpcService>();
    });

    // add other rpc clients ...
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

### ClientSide setup (Console Program)

```C#

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

        var client = factory.Get<ISampleRpcService>();
        var result = client.Hello();

        Console.WriteLine(result);
    }
}
```

## Supported Attributes

Most of `HttpMethod` and `BindingSource` are supported: `HttpGet`, `HttpPost`, `FromQuery`, `FormRoute`, `FromBody`, etc.

You can refer to [ISampleRpcService.cs](/samples/RpcController.Samples.Shared/ISampleRpcService.cs)

`IRpcController` has same default behaviors as `ASP.NET Core`, you can omit the parameter attribute like:

``` C#
[HttpRoute("/sample-rpc")]
public interface ISampleRpcService : IRpcController
{
    [HttpGet("{user}")]
    string FromRoute(string user); // name will be route path parameter

    [HttpGet("hello")]
    string FromQuery(string user); // name will be query parameter

    [HttpPost("from-json-body")]
    string Hello(User user); // user will be json body
}
```

## Client Side Exception Handler

RPC Client will throw `CallResultException` if some error occurs, like the network failure, data issue, invalid business operation...

```C#

var rpcClient = rpcClientFactory.Get<ISomeRpcService>();

try
{
    var result = rpcClient.DoSomething();
}
catch (CallResultException ex)
{
    // process exception
}
```

`CallResultException.Response` will provide `HttpResponseMessage` if RPC Server responds.

In many cases you should let the error throw and use `ExceptionHandler`:

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

        // any other errors
    });
});

```

If you have a complex `call chain` like A => B => C => D => => E => F, and the F throws an error,
you can use this `ExceltionHandler` in each services, the error will be populated to A without any specific process in service BCDE.

## Extensibility

The extensibility of Server Side follow the same behavior as `ASP.NET Core`, just keep using these features.

If you need to extend ClientSide, you can register your custom `IRpcClientHook`:

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

`HttpRequestMessage` and `HttpResponseMessage` can be accessed on CallContext when processing request and response.

Register the defined `IRpcClientHook`:

``` C#
builder.Services.UseRpcClients(rpc =>
{
    rpc.UseHooks([ new MyRpcClientHook() ])                         // For all RPC clients
    rpc.AddGroup(options =>
    {
        options.UseScopedScopes([ new MyRpcClientHook() ]);         // For this grouped RPC clients
        options.BaseAddress = "http://localhost:5080";
        options.AddRpcControllersFromAssembly<ISampleRpcService>();
    });
});
```
