# RpcController

A RPC Framework based on `ASP.NET Core Controller`.

## Why need this library

There are two service A and B, service B needs to call some api of service A.

It should be the simplest solution with `ASP.NET Core`:

```
ProjectFolder/
|
├── ServiceA/
|   ├── PublicController.cs    <-- Implement RPC via interface (Server Side)
|   └── ...
|
├── ServiceA.Shared/
|   ├── IPublicController.cs   <-- Define RPC Interface (Shared Project)
|   └── ...
|
├── ServiceB/
|   ├── AppController.cs       <-- Call RPC via interface (Client Side)
|   └── ...
```

``` C#
// In ServiceA.Shared/IPublicController.cs
// Declare RPC interface
[HttpRoute("/api/v1/public")]
public interface IPublicController : IRpcController
{
    [HttpGet("int")]
    int Add(int a, int b);
}
```

``` C#
// In ServiceA/PublicController.cs
// Implement the RPC interface in Server (ASP.NET Core)
public class PublicController : IPublicController
{
    public int Add(int a, int b)
    {
        return a + b
    }
}
```

``` C#
// In ServiceB/AppController.cs
// Import IPublicController as Client interface to call the RPC Server.
public class AppController
{
    private readonly IPublicController _publicController;

    public AppController(IPublicController publicController)
    {
        _publicController = publicController;
    }

    public int CallRPC()
    {
        return _publicController.Add(1, 2);
    }
}
```

Compared to other frameworks (gRPC, Orleans, etc...), this library doesn't bring any concepts, just follow the feeling:

- Define a `RPC Interface` with some `Attributes` provided by `ASP.NET Core`
- Implement the `RPC Interface` in Service B with Controller (Server-Side)
- Call the RPC Server via `RPC Interface` (Client-Side)

**Just Enough, Let the Magic Do the Rest**
