using System.Text.Json;

namespace RpcController.Client.Internal;

internal class RpcHelper
{
    public static JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
