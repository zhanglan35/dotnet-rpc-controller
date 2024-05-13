using RpcController.AspNetCore;
using RpcController.Samples.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeApplicationXmlComments();
});

builder.Services.UseRpcClients(rpc =>
{
    rpc.AddGroup(options =>
    {
        options.BaseAddress = "http://localhost:5080";
        options.AddRpcControllersFromAssembly<ISampleRpcService>();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
