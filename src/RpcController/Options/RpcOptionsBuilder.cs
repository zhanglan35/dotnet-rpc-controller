namespace RpcController.Client.Options;

public class RpcOptionsBuilder
{
    private readonly List<RpcControllerOptions> _options = new();
    public IReadOnlyCollection<RpcControllerOptions> Options => _options.ToArray();

    public void AddOptions(Action<RpcControllerOptions> configure)
    {
        var options = new RpcControllerOptions();

        configure(options);

        _options.Add(options);
    }
}
