using System.Threading.Tasks;

namespace UniCli.Client;

internal interface IProcessActivator
{
    Task<long> ActivateProcessAsync(int pid);
    Task RestoreFocusAsync(long savedState);
}
