using ConsoleAppFramework;
using System.Threading.Tasks;

namespace UniCli.Client;

public partial class Commands
{
    /// <summary>
    /// Execute a command on the Unity Editor server
    /// </summary>
    public async Task<int> Exec(
        [Argument] string command,
        [Argument] string data = "",
        int timeout = 0,
        bool json = false,
        bool noFocus = false)
    {
        var focus = UnityProcessActivator.ShouldFocus(noFocus);
        var result = await CommandExecutor.ExecuteAsync(command, data, timeout, json, focusEditor: focus);
        return OutputWriter.Write(result, json);
    }
}
