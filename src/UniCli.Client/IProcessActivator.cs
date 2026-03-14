using System.Threading.Tasks;

namespace UniCli.Client;

internal interface IProcessActivator
{
    bool SupportsActivation { get; }
    Task<long> ActivateProcessAsync(int pid);
    Task RestoreFocusAsync(long savedState);
}
