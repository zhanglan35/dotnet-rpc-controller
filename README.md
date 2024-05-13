# RpcController

.NET RPC Framework based on `ASP.NET Core Controller`.

## Why need this library

There are two service A and B, service B needs to call some api of service A.

It should be the simplest solution with `ASP.NET Core`:

```
ProjectFolder/
|
├── ServiceA/
|   ├── SampleRpcService.cs    <-- Implement RPC via interface (Server Side)
|   ├── Program.cs
|   └── ...
|
├── ServiceA.Shared/
|   ├── ISampleRpcService.cs   <-- Define RPC Interface (Shared Project)
|   └── ...
|
├── ServiceB/
|   ├── AppController.cs       <-- Call RPC via interface (Client Side)
|   ├── Program.cs
|   └── ...
```

``` C#
// Declare RPC interface
// In ServiceA.Shared/ISampleRpcService.cs
[HttpRoute("/api/v1/public")]
public interface ISampleRpcService : IRpcController
{
    [HttpGet("int")]
    int Add(int a, int b);
}
```

``` C#
// Implement the RPC interface in Server (ASP.NET Core)
// In ServiceA/SampleRpcService.cs
public class SampleRpcService : ISampleRpcService
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

``` C#
// In ServiceB/AppController.cs
// Import ISampleRpcService as Client interface to call the RPC Server.
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

**Just Enough, Let the Magic Do the Rest**

Compared to other frameworks (gRPC, Orleans, etc...), this library doesn't bring any new concepts, just follow the feeling:

- Define a `RPC Interface` with some `Attributes` provided by `ASP.NET Core`
- Implement the `RPC Interface` in Service B with Controller (Server-Side)
- Call the RPC Server via `RPC Interface` (Client-Side)

The most important change is to extract the controller defination into an interface of a shared project,
this interface will be shared between ServerSide and ClientSide.

You can continue to use Controler, Middleware, Swagger and any other ASP.NET Core features in ServerSide.

## Quick Start

Install Package

```
// Not released yet
```

ServerSide setup

``` C#
// In ServiceA/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Conventions.Add(new RpcControllerConvention()));

builder.Services.AddSwaggerGen(options =>
{
    options.IncludeApplicationXmlComments(); // Add XML comments as needed
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

ClientSide setup

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
