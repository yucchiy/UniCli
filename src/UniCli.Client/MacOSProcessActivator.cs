using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Client;

internal sealed class MacOSProcessActivator : IProcessActivator
{
    private const int TimeoutMs = 2000;

    public async Task<long> ActivateProcessAsync(int pid)
    {
        var previousPid = await GetFrontmostProcessIdAsync();
        await SetFrontmostAsync(pid);
        return previousPid;
    }

    public async Task RestoreFocusAsync(long savedState)
    {
        var pid = (int)savedState;
        if (pid > 0)
            await SetFrontmostAsync(pid);
    }

    private async Task<long> GetFrontmostProcessIdAsync()
    {
        var output = await RunOsascriptAsync(
            "tell application \"System Events\" to get unix id of first process whose frontmost is true");

        if (int.TryParse(output?.Trim(), out var pid))
            return pid;

        return 0;
    }

    private async Task SetFrontmostAsync(int pid)
    {
        await RunOsascriptAsync(
            $"tell application \"System Events\" to set frontmost of (first process whose unix id is {pid}) to true");
    }

    private static async Task<string?> RunOsascriptAsync(string script)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "osascript",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.StartInfo.ArgumentList.Add("-e");
        process.StartInfo.ArgumentList.Add(script);

        process.Start();

        using var cts = new CancellationTokenSource(TimeoutMs);
        try
        {
            var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);
            return process.ExitCode == 0 ? output : null;
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            return null;
        }
    }
}
