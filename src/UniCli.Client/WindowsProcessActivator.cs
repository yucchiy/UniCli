using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UniCli.Client;

internal sealed class WindowsProcessActivator : IProcessActivator
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public Task<long> ActivateProcessAsync(int pid)
    {
        var savedHwnd = GetForegroundWindow();

        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.MainWindowHandle != IntPtr.Zero)
                SetForegroundWindow(process.MainWindowHandle);
        }
        catch
        {
            // Best-effort: process may have exited
        }

        return Task.FromResult(savedHwnd.ToInt64());
    }

    public Task RestoreFocusAsync(long savedState)
    {
        var hwnd = new IntPtr(savedState);
        if (hwnd != IntPtr.Zero)
            SetForegroundWindow(hwnd);

        return Task.CompletedTask;
    }
}
