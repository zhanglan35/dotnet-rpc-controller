namespace RpcController.Client.Hooks;

internal class ConfigureBaseAddressHook : RpcClientHook
{
    private readonly string _baseUrl;

    public ConfigureBaseAddressHook(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public override void Configure(HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(_baseUrl);
    }
}
