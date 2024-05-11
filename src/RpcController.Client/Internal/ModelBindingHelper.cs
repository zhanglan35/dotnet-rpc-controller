namespace RpcController.Client.Internal;

internal static class ModelBindingHelper
{
    // These types will be treated as simple types by the model binding system.
    // Reference asp.net core document:
    // https://learn.microsoft.com/zh-cn/aspnet/core/mvc/models/model-binding?view=aspnetcore-7.0#simple-types
    public static readonly Type[] SimpleTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(char),
        // typeof(DateOny),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(decimal),
        typeof(double),
        typeof(Enum),
        typeof(Guid),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        // typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
        typeof(Uri),
        typeof(Version),
    ];

    public static bool IsSimpleType(Type type)
    {
        return SimpleTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
    }

    public static bool IsMethodSupportBody(string method)
    {
        return method == "POST" ||
            method == "PUT" ||
            method == "PATCH";
    }
}
