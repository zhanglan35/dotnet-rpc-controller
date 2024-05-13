using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RpcController.Client.Internal;
using RpcController.Samples.ServerSide.Controllers;
using RpcController.Samples.Shared;

namespace RpcController.AspNetCore;

public class RpcServerSideConventionTest : IClassFixture<ConventionHttpServerFixture>
{
    private readonly ConventionHttpServerFixture _fixture;

    public RpcServerSideConventionTest(ConventionHttpServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    public async Task Hello(string name)
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync($"/sample-rpc/hello?name={name}");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe($"Hello, {name}!");
    }

    [Fact]
    public async Task FromPath()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/from-path");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("From Path");
    }

    [Fact]
    public async Task FromRootPath()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/from-root-route");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("From Root Path");
    }

    [Fact]
    public async Task FromRoute()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/from-route/100/foo");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("100 foo");
    }

    [Fact]
    public async Task FromRouteNullable()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/from-route-nullable/100/foo");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("100 foo");
    }

    [Fact]
    public async Task FromQuery()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/from-query?value1=100&int=foo&int=bar&flag=true&value4=2021-01-01");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("100 foo,bar True 2021-01-01 00:00:00");
    }

    [Fact]
    public async Task FromQueryDefault()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/from-query-default");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("0___text");
    }

    [Fact]
    public async Task FromHeader()
    {
        var client = _fixture.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/sample-rpc/from-header");
        request.Headers.Add("string", "foo");
        request.Headers.Add("bool", "true");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("0 foo True");
    }

    [Fact]
    public async Task FromBody()
    {
        var client = _fixture.CreateClient();
        var user = new UserModel { Id = 100, Name = "foo", DateTime = new(2000, 01, 01) };
        var response = await client.PostAsJsonAsync("/sample-rpc/body", user);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserModel>(content, RpcHelper.JsonOptions);

        result.ShouldBe(user);
    }

    [Fact]
    public async Task FromForm()
    {
        var client = _fixture.CreateClient();
        var form = new FunctionFormModel
        {
            Name = "name",
            Value = "value",
            Flag = true,
            Number = 100
        };
        var formContent = new MultipartFormDataContent
        {
            { new StringContent(form.Name), "name" },
            { new StringContent(form.Value), "value" },
            { new StringContent(form.Flag.ToString()), "flag" },
            { new StringContent(form.Number.ToString()), "number" }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/sample-rpc/form")
        {
            Content = formContent
        };
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FunctionFormModel>(content, RpcHelper.JsonOptions);

        result.ShouldBe(form);
    }

    [Fact]
    public async Task FromFormFile()
    {
        var client = _fixture.CreateClient();
        var fileContent = "Hello, World!";
        var file = new StreamContent(new MemoryStream(Encoding.ASCII.GetBytes(fileContent)));
        file.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"file.txt\"");
        file.Headers.Add("Content-Type", "text/plain");

        var formContent = new MultipartFormDataContent
        {
            { file, "file", "file.txt" }
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/sample-rpc/upload-file")
        {
            Content = formContent
        };
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe(fileContent);
    }

    [Fact]
    public async Task FromFormFiles()
    {
        var client = _fixture.CreateClient();
        var files = new[]
        {
            "File content1",
            "File content2"
        };

        var formContent = new MultipartFormDataContent();

        foreach (var fileContent in files)
        {
            var file = new StreamContent(new MemoryStream(Encoding.ASCII.GetBytes(fileContent)));
            file.Headers.Add("Content-Disposition", "form-data; name=\"files\"; filename=\"file.txt\"");
            file.Headers.Add("Content-Type", "text/plain");

            formContent.Add(file, "files", "file.txt");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/sample-rpc/upload-files")
        {
            Content = formContent
        };
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<string[]>(content, RpcHelper.JsonOptions);

        result.ShouldNotBeNull().Length.ShouldBe(files.Length);

        for (var i = 0; i < files.Length; i++)
        {
            result[i].ShouldBe(files[i]);
        }
    }

    [Fact]
    public async Task CustomMethod()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/sample-rpc/custom-method");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("Custom Method");
    }
}

/// <summary>
/// Create a fixture to start the web application
/// </summary>
public class ConventionHttpServerFixture : IDisposable
{
    private readonly WebApplication _app;

    public ConventionHttpServerFixture()
    {
        _app = StartServer();
        Console.WriteLine("TestServer Started.");
    }

    public void Dispose()
    {
        StopServer();
    }

    public HttpClient CreateClient()
    {
        return _app.GetTestClient();
    }

    private WebApplication StartServer()
    {
        var builder = WebApplication.CreateBuilder();
        var services = builder.Services;

        builder.WebHost.UseTestServer();
        builder.Services
            .AddControllers(options => options.Conventions.Add(new RpcServerSideConvention()))
            .AddApplicationPart(typeof(SampleRpcController).Assembly);

        services.AddHttpContextAccessor();
        services.AddHttpClient();

        var app = builder.Build();

        app.MapControllers();

        app.Start();

        return app;
    }

    private void StopServer()
    {
        _app?.StopAsync().Wait();
        Console.WriteLine("TestServer stopped.");
    }
}
