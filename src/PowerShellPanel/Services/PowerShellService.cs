using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace PowerShellPanel.Services;

/// <summary>
/// Wraps System.Management.Automation with a PERSISTENT Runspace.
/// Sessions preserve: current directory, variables, loaded modules, etc.
/// </summary>
public class PowerShellService : IDisposable
{
    private Runspace _runspace;
    private PowerShell? _currentPs;
    private readonly CancellationTokenSource _cts = new();

    public event Action<string>? OutputReceived;
    public event Action<string>? ErrorReceived;
    public event Action<bool>? ExecutionCompleted;

    /// <summary>Fires after each command, carrying the new current directory.</summary>
    public event Action<string>? LocationChanged;

    public PowerShellService()
    {
        _runspace = RunspaceFactory.CreateRunspace();
        _runspace.Open();
    }

    /// <summary>
    /// Execute a PowerShell command in the persistent session.
    /// </summary>
    public async Task ExecuteAsync(string command, CancellationToken cancel = default)
    {
        Cancel();

        // Create a new PowerShell instance sharing the persistent runspace
        _currentPs = PowerShell.Create();
        _currentPs.Runspace = _runspace;
        var ps = _currentPs;

        // Stream info output
        ps.Streams.Information.DataAdding += (_, args) =>
        {
            if (args.ItemAdded is InformationRecord info)
                OutputReceived?.Invoke(info.ToString());
        };

        ps.Streams.Verbose.DataAdding += (_, args) =>
        {
            if (args.ItemAdded is VerboseRecord v)
                OutputReceived?.Invoke(v.Message);
        };

        ps.Streams.Debug.DataAdding += (_, args) =>
        {
            if (args.ItemAdded is DebugRecord d)
                OutputReceived?.Invoke(d.Message);
        };

        ps.AddScript(command);

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, _cts.Token);

            await Task.Run(() =>
            {
                var results = ps.Invoke();
                foreach (var obj in results)
                    OutputReceived?.Invoke(obj.ToString());

                foreach (var err in ps.Streams.Error)
                    ErrorReceived?.Invoke(err.ToString());
            }, linked.Token);

            ExecutionCompleted?.Invoke(!ps.HadErrors);

            // Fire location changed to keep UI in sync
            NotifyLocationChanged();
        }
        catch (OperationCanceledException)
        {
            OutputReceived?.Invoke("[操作已取消]");
            ExecutionCompleted?.Invoke(false);
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(ex.Message);
            ExecutionCompleted?.Invoke(false);
        }
    }

    /// <summary>
    /// Get the current working directory from the session.
    /// </summary>
    private void NotifyLocationChanged()
    {
        try
        {
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript("(Get-Location).Path");
            var result = ps.Invoke();
            if (result.Count > 0)
                LocationChanged?.Invoke(result[0].ToString());
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// Reset the session — close old runspace and create a fresh one.
    /// </summary>
    public void ResetSession()
    {
        Cancel();
        try { _runspace.Dispose(); } catch { }
        _runspace = RunspaceFactory.CreateRunspace();
        _runspace.Open();
        OutputReceived?.Invoke("[Session reset — fresh PowerShell environment]");
    }

    public void Cancel()
    {
        _currentPs?.Stop();
        _currentPs?.Dispose();
        _currentPs = null;
    }

    public void Dispose()
    {
        Cancel();
        _runspace.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
