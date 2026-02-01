using System;
using System.Text.Json;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Client;

internal static class UnityProcessActivator
{
    private const int PipeTimeoutMs = 2000;

    private static readonly IProcessActivator Activator = CreateActivator();

    private static IProcessActivator CreateActivator()
    {
        if (OperatingSystem.IsMacOS()) return new MacOSProcessActivator();
        if (OperatingSystem.IsWindows()) return new WindowsProcessActivator();
        return new NullProcessActivator();
    }

    public static bool ShouldFocus(bool noFocusFlag)
    {
        if (noFocusFlag)
            return false;

        var env = Environment.GetEnvironmentVariable("UNICLI_FOCUS");
        if (env != null)
            return env is "1" or "true";

        return true;
    }

    public static async Task<long> TryActivateAsync(string pipeName)
    {
        try
        {
            var unityPid = await GetUnityProcessIdAsync(pipeName);
            if (unityPid <= 0)
                return 0;

            return await Activator.ActivateProcessAsync(unityPid);
        }
        catch
        {
            return 0;
        }
    }

    public static async Task TryRestoreFocusAsync(long savedState)
    {
        if (savedState == 0)
            return;

        try
        {
            await Activator.RestoreFocusAsync(savedState);
        }
        catch
        {
            // Best-effort: silently ignore all errors
        }
    }

    private static async Task<int> GetUnityProcessIdAsync(string pipeName)
    {
        using var client = new PipeClient(pipeName);
        var connectResult = await client.ConnectAsync(timeoutMs: PipeTimeoutMs);
        if (connectResult.IsError)
            return 0;

        var request = new CommandRequest
        {
            command = "Project.Inspect",
            data = "",
            format = "json"
        };

        var result = await client.SendCommandAsync(request, timeoutMs: PipeTimeoutMs);
        if (result.IsError)
            return 0;

        var response = result.Match(
            onSuccess: r => r,
            onError: _ => (CommandResponse?)null);

        if (response == null || !response.success || string.IsNullOrEmpty(response.data))
            return 0;

        using var doc = JsonDocument.Parse(response.data);
        if (!doc.RootElement.TryGetProperty("processId", out var pidElement))
            return 0;

        return pidElement.GetInt32();
    }
}
