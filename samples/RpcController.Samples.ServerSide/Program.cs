using RpcController.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = default(WebApplication)!; // Workaround fo setup swagger

builder.Services
    .AddControllers(options =>
    {
        options.Conventions.Add(new RpcServerSideConvention());
    })
    .AddRpcControllerAsServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeApplicationXmlComments();
    // Include xml comments for controllers which implement RpcService interface
    options.IncludeRpcControllerXmlComments(app.Services);
});

app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
