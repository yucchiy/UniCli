using System.Threading.Tasks;

namespace UniCli.Client;

internal sealed class NullProcessActivator : IProcessActivator
{
    public Task<long> ActivateProcessAsync(int pid) => Task.FromResult(0L);
    public Task RestoreFocusAsync(long savedState) => Task.CompletedTask;
}
