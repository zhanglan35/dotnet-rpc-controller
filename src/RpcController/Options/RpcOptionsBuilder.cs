namespace RpcController.Client.Options;

public class RpcOptionsBuilder
{
    private readonly List<RpcGroupOptions> _options = new();
    public IReadOnlyCollection<RpcGroupOptions> Options => _options.ToArray();

    public void AddGroup(Action<RpcGroupOptions> configure)
    {
        var options = new RpcGroupOptions();

        configure(options);

        _options.Add(options);
    }
}
