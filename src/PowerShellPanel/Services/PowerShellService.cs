using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PowerShellPanel.Services;

/// <summary>
/// Wraps System.Management.Automation for in-process PowerShell execution.
/// Output is streamed in real-time via callbacks.
/// </summary>
public class PowerShellService : IDisposable
{
    private PowerShell? _currentPs;
    private readonly CancellationTokenSource _cts = new();

    public event Action<string>? OutputReceived;
    public event Action<string>? ErrorReceived;
    public event Action<bool>? ExecutionCompleted;

    /// <summary>
    /// Execute a PowerShell command string asynchronously, streaming output in real-time.
    /// </summary>
    public async Task ExecuteAsync(string command, CancellationToken cancel = default)
    {
        Cancel();

        _currentPs = PowerShell.Create();
        var ps = _currentPs;

        // Stream pipeline output
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

    public void Cancel()
    {
        _currentPs?.Stop();
        _currentPs?.Dispose();
        _currentPs = null;
    }

    public void Dispose()
    {
        Cancel();
        _cts.Cancel();
        _cts.Dispose();
    }
}
