# RpcController

.NET RPC Framework based on `ASP.NET Core Controller`.

## Why need this library

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
    options.IncludeApplicationXmlComments(); // Add XML comments as needed
});

// Configure services ...

var app = builder.Build();

// Configure app ...

app.Run();
```

### ClientSide setup

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
