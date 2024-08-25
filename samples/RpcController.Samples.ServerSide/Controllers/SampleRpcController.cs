using System.Text;
using Microsoft.AspNetCore.Mvc;
using RpcController.Samples.Shared;

namespace RpcController.Samples.ServerSide.Controllers;

public class SampleRpcController : ISampleRpcService
{
    public string Hello(string name) => $"Hello, {name}!";

    public string FromPath() => "From Path";

    public string FromRootPath() =>$"From Root Path";

    public string FromRoute(int value1, string value2) => $"{value1} {value2}";

    public string FromRouteRename(int value1, string value2) => $"{value1} {value2}";

    public string FromQuery(int value1, string[] value2, bool value3, DateTime value4) => $"{value1} {string.Join(',', value2)} {value3} {value4:yyyy-MM-dd HH:mm:ss}";

    public string FromQueryDefault(int? a = 0, int? a2 = null, string? b = null, string? b2 = "text") => $"{a}_{a2}_{b}_{b2}";

    public string FromHeader(int value1, string value2, bool value3) => $"{value1} {value2} {value3}";

    public UserModel FromBody(UserModel user) => user;

    public FunctionFormModel FromForm(FunctionFormModel model) => model;

    public async Task<string> FromFormFile(IFormFile file)
    {
        var content = new byte[file.Length];

        await file.OpenReadStream().ReadAsync(content);

        return Encoding.ASCII.GetString(content);
    }

    public async Task<string[]> FromFormFiles(IFormFile[] files)
    {
        var result = new List<string>();

        foreach (var file in files)
        {
            var content = new byte[file.Length];
            await file.OpenReadStream().ReadAsync(content);
            result.Add(Encoding.ASCII.GetString(content));
        }

        return result.ToArray();
    }

    public Task<FileContentResult> DownloadFile()
    {
        return Task.FromResult(
            new FileContentResult(Encoding.ASCII.GetBytes("Download File Content"), "text/plain")
            {
                FileDownloadName = "download.txt"
            }
        );
    }

    /// <summary>
    /// define other method
    /// </summary>
    /// <returns></returns>
    [HttpGet("custom-method")]
    public string CustomMethod() => "Custom Method";

}
