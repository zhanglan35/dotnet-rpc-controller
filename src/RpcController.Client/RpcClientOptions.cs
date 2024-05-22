using RpcController.Client.Hooks;
using RpcController.Options;

namespace RpcController.Client;

/// <summary>
/// ContractClientOptions
/// To customize the behavior of ContractClient
/// </summary>
public class RpcClientOptions
{
    /* Internal Hooks */
    private static readonly RpcClientHook ForwardAuthorizationHook = new ForwardAuthorizationHook();
    private static readonly RpcClientHook ResolveModelBindingHook = new ResolveModelBindingHook();

    public IReadOnlyCollection<IRpcClientHook> Hooks { get; }

    public RpcClientOptions(IReadOnlyCollection<IRpcClientHook> hooks)
    {
        Hooks = hooks;
    }

    public static RpcClientOptions From(RpcGroupOptions options, List<IRpcClientHook>? hooks = null)
    {
        hooks ??= [];

        if (!string.IsNullOrWhiteSpace(options.BaseAddress))
        {
            hooks.Add(new ConfigureBaseAddressHook(options.BaseAddress!));
        }

        if (options.ForwardAuthorization)
        {
            hooks.Add(ForwardAuthorizationHook);
        }

        /* This hook is required */
        hooks.Add(ResolveModelBindingHook);

        return new(hooks);
    }
}
