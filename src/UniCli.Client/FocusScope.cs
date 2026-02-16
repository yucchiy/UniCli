using System;
using System.Threading.Tasks;

namespace UniCli.Client;

internal sealed class FocusScope : IAsyncDisposable
{
    public static readonly FocusScope Noop = new(0);

    private readonly long _savedState;

    private FocusScope(long savedState)
    {
        _savedState = savedState;
    }

    public static async Task<FocusScope> ActivateAsync(string projectRoot)
    {
        var savedState = await UnityProcessActivator.TryActivateAsync(projectRoot);
        return new FocusScope(savedState);
    }

    public async ValueTask DisposeAsync()
    {
        if (_savedState != 0)
            await UnityProcessActivator.TryRestoreFocusAsync(_savedState);
    }
}
