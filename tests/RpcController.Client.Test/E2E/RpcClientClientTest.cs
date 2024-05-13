using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;

namespace RpcController.Client.E2E;

public class RpcClientClientTest
{
    private static (RpcClientTestHelper, T, RpcClient<T>) CreateMock<T>() where T : class, IRpcController
    {
        return RpcClientTestHelper.Create<T>();
    }

    #region base

    [HttpRoute("api/v1")]
    public interface IWelcomeController : IRpcController
    {
        [HttpGet]
        Task<string> Welcome();
    }

    [Fact]
    public async Task Welcome_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IWelcomeController>();

        helper.WhenRequest = req =>
        {
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/");
        };
        helper.ConfigureResponse = res =>
        {
            res.Content = new StringContent("Hello World");
        };

        (await controller.Welcome()).ShouldBe("Hello World");
        (await controllerClient.CallAsync(x => x.Welcome())).Data.ShouldBe("Hello World");
    }

    [HttpRoute("api/v1")]
    public interface IGetBoolController : IRpcController
    {
        [HttpGet]
        Task<bool> GetAsync();
    }

    [Fact]
    public async Task GetBool_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IGetBoolController>();

        helper.ConfigureResponse = res =>
        {
            res.Content = JsonContent.Create(true);
        };

        (await controller.GetAsync()).ShouldBe(true);
        (await controllerClient.CallAsync(x => x.GetAsync())).Data.ShouldBe(true);
    }

    [HttpRoute("api/v1")]
    public interface IGetDoubleController : IRpcController
    {
        [HttpGet]
        Task<double> GetAsync();
    }

    [Fact]
    public async Task GetInt_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IGetDoubleController>();

        helper.ConfigureResponse = res =>
        {
            res.Content = JsonContent.Create(5);
        };

        (await controller.GetAsync()).ShouldBe(5);
        (await controllerClient.CallAsync(x => x.GetAsync())).Data.ShouldBe(5);
    }

    [HttpRoute("api/v1")]
    public interface IFromQueryController : IRpcController
    {
        [HttpPost("add")]
        Task<int> AddAsync(int a, int b);
    }

    [Fact]
    public async Task AddAsync_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromQueryController>();

        helper.WhenRequest = req =>
        {
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/add");
            req.RequestUri.Query.ShouldBe("?a=1&b=2");
        };
        helper.ConfigureResponse = res =>
        {
            res.Content = JsonContent.Create(3);
        };

        (await controller.AddAsync(1, 2)).ShouldBe(3);
        (await controllerClient.CallAsync(x => x.AddAsync(1, 2))).Data.ShouldBe(3);
    }

    [HttpRoute("api/v1")]
    public interface IFromRouteController : IRpcController
    {
        [HttpGet("add/{a}/{b}")]
        Task AddRouteAsync(int a, int b);
    }

    [Fact]
    public async Task AddRouteAsync_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromRouteController>();

        helper.WhenRequest = req =>
        {
            var requestBody = JsonSerializer.Serialize(new AddJsonDto { A = 1, B = 2 }, JsonSerializerOptions.Default);
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/add/1/2");
        };

        await controller.AddRouteAsync(1, 2);
    }

    [HttpRoute("api/v1")]
    public interface IFromJsonController : IRpcController
    {
        [HttpPost("add")]
        Task<AddJsonResult> AddJsonAsync(AddJsonDto dto);
    }

    public record AddJsonDto
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    public record AddJsonResult
    {
        public int Result { get; set; }
    }

    [Fact]
    public async Task AddJsonAsync_Test()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromJsonController>();

        helper.WhenRequest = req =>
        {
            var requestBody = JsonSerializer.Serialize(new AddJsonDto { A = 1, B = 2 }, JsonSerializerOptions.Default);
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/add");
            req.Content.ShouldNotBeNull();
            req.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
            req.Content.ReadAsStringAsync().Result.ShouldBe(requestBody);
        };
        helper.ConfigureResponse = res =>
        {
            res.Content = JsonContent.Create(new AddJsonResult { Result = 3 });
        };

        (await controller.AddJsonAsync(new() { A = 1, B = 2 })).Result.ShouldBe(3);
    }

    [HttpRoute("api/v1")]
    public interface IFromFormController : IRpcController
    {
        [HttpGet("add")]
        Task AddAsync([FromForm] int a, [FromForm] int b);
    }

    [Fact]
    public async Task AddFromForm()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromFormController>();

        helper.WhenRequest = req =>
        {
            var content = req.Content as MultipartFormDataContent;

            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/add");

            content!.First(x => x.Headers.ContentDisposition!.Name == "a")
                .ReadAsStringAsync().Result.ShouldBe("1");
            content!.First(x => x.Headers.ContentDisposition!.Name == "b")
                .ReadAsStringAsync().Result.ShouldBe("2");
        };

        await controller.AddAsync(1, 2);
        await controllerClient.CallAsync(x => x.AddAsync(1, 2));
    }

    [HttpRoute("api/v1")]
    public interface IFromFileController : IRpcController
    {
        [HttpPost("upload")]
        Task UploadAsync([FromForm] string userId, IFormFile file);
    }

    [Fact]
    public async Task UploadFromFile()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromFileController>();

        helper.WhenRequest = req =>
        {
            var content = req.Content as MultipartFormDataContent;
            var file = content!.FirstOrDefault(x => x.Headers.ContentDisposition!.Name == "file");

            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.LocalPath.ShouldBe("/api/v1/upload");

            content!.First(x => x.Headers.ContentDisposition!.Name == "userId")
                .ReadAsStringAsync().Result.ShouldBe("foo");

            file.ShouldNotBeNull();
            file.Headers.ContentDisposition!.FileName.ShouldBe("test.txt");
            file.ReadAsStringAsync().Result.ShouldBe("Hello World");
        };

        var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hello World"));
        var file = new FormFile(stream, 0, stream.Length, "test", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain",
        };

        await controller.UploadAsync("foo", file);
        await controllerClient.CallAsync(x => x.UploadAsync("foo", file));
    }

    [HttpRoute("api/v1")]
    public interface IFromFilesController : IRpcController
    {
        [HttpPost("upload")]
        Task UploadAsync(IFormFile[] files);
    }

    [Fact]
    public async Task UploadFromFiles()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromFilesController>();

        helper.WhenRequest = req =>
        {
            var content = req.Content as MultipartFormDataContent;
            var file1 = content!.FirstOrDefault(x => x.Headers.ContentDisposition!.FileName == "test1.txt");
            var file2 = content!.FirstOrDefault(x => x.Headers.ContentDisposition!.FileName == "test2.txt");

            file1.ShouldNotBeNull();
            file1.ReadAsStringAsync().Result.ShouldBe("Hello World");

            file2.ShouldNotBeNull();
            file2.ReadAsStringAsync().Result.ShouldBe("Hello World2");
        };

        var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hello World"));
        var stream2 = new MemoryStream(Encoding.ASCII.GetBytes("Hello World2"));
        var files = new FormFile[]
        {
            new (stream, 0, stream.Length, "test", "test1.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain",
            },
            new (stream2, 0, stream2.Length, "test", "test2.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain",
            },
        };

        await controller.UploadAsync(files);
        await controllerClient.CallAsync(x => x.UploadAsync(files));
    }

    #endregion

    #region default

    public enum FromDefaultEnum
    {
        A,
        B,
        C,
    }

    [HttpRoute("api/v1")]
    public interface IFromDefaultController : IRpcController
    {
        [HttpGet("from-query-default")]
        Task<string> QueryAsync(FromDefaultEnum[] enums, int? a = 0, int? a2 = null, string? b = null, string? b2 = "text");
    }

    [Fact]
    public async Task FromDefaultTest()
    {
        var (helper, controller, controllerClient) = CreateMock<IFromDefaultController>();
        var enums = new[] { FromDefaultEnum.A, FromDefaultEnum.B };

        helper.WhenRequest = req =>
        {
            req.RequestUri.ShouldNotBeNull();
            req.RequestUri.Query.ShouldBe("?enums=0&enums=1&a=0&a2=&b=hello&b2=text");
        };
        helper.ConfigureResponse = res =>
        {
            res.Content = new StringContent("3");
        };

        (await controller.QueryAsync(enums: enums, b: "hello")).ShouldBe("3");
        (await controllerClient.CallAsync(x => x.QueryAsync(enums: enums, b: "hello"))).Data.ShouldBe("3");
    }

    #endregion default
}
