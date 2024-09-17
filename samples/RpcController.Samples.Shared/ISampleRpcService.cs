using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RpcController.Samples.Shared;

/// <summary>
/// Sample RPC service interface.
/// </summary>
[HttpRoute("/sample-rpc")]
public interface ISampleRpcService : IRpcController
{
    /// <summary>
    /// Say hello to the specified name.
    /// </summary>
    [HttpGet("hello")]
    string Hello(string name);

    /// <summary>
    /// From Route Path
    /// </summary>
    [HttpGet("from-path")]
    string FromPath();

    /// <summary>
    /// From Root Route
    /// </summary>
    [HttpGet("/from-root-route")]
    string FromRootPath();

    /// <summary>
    /// Route Path Binding
    /// </summary>
    [HttpGet("from-route/{value1}/{value2}")]
    string FromRoute(int value1, string value2);

    /// <summary>
    /// Route Path Binding (Nullable)
    /// </summary>
    [HttpGet("from-route-nullable/{value11}/{value22}")]
    string FromRouteRename([FromRoute(Name = "value11")] int value1, [FromRoute(Name = "value22")] string value2);

    /// <summary>
    /// Query Binding
    /// </summary>
    [HttpGet("from-query")]
    string FromQuery(int value1, [FromQuery(Name = "int")] string[] value2, [FromQuery(Name = "flag")]bool value3, DateTime value4);

    /// <summary>
    /// Query Binding (Default Value)
    /// </summary>
    [HttpGet("from-query-default")]
    string FromQueryDefault(int? a = 0, int? a2 = null, string? b = null, string? b2 = "text");

    /// <summary>
    /// Header Binding
    /// </summary>
    [HttpGet("from-header")]
    string FromHeader(int value1, [FromHeader(Name = "string")] string value2, [FromHeader(Name = "bool")] bool value3);

    /// <summary>
    /// Body BInding
    /// </summary>
    [HttpPost("body")]
    UserModel FromBody(UserModel user);

    /// <summary>
    /// Form Binding
    /// </summary>
    [HttpPost("form")]
    FunctionFormModel FromForm([FromForm] FunctionFormModel model);

    /// <summary>
    /// FormData Binding (Form File)
    /// </summary>
    [HttpPost("upload-file")]
    Task<string> FromFormFile(IFormFile file);

    /// <summary>
    /// FormData Binding (FormFiles)
    /// </summary>
    [HttpPost("upload-files")]
    Task<string[]> FromFormFiles(IFormFile[] files);

    /// <summary>
    /// 下载文件
    /// </summary>
    [HttpGet("download-file-content")]
    Task<FileContentResult> DownloadFile();

    /// <summary>
    /// 下载文件
    /// </summary>
    [HttpGet("download-file-content-stream")]
    Task<FileStreamResult> DownloadFileStream();
}

public record FunctionFormModel
{
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;
    public bool Flag { get; set; }
    public int Number { get; set; }
}

public record UserModel
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime DateTime { get; set; }
}
