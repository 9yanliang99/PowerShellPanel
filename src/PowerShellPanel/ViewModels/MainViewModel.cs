using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PowerShellPanel.Models;
using PowerShellPanel.Services;
using PowerShellPanel.Views;

namespace PowerShellPanel.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly PowerShellService _psService = new();
    private static LocalizationService L => LocalizationService.Instance;

    public MainViewModel()
    {
        _psService.OutputReceived += OnOutputReceived;
        _psService.ErrorReceived += OnErrorReceived;
        _psService.ExecutionCompleted += OnExecutionCompleted;
        _psService.LocationChanged += OnLocationChanged;

        PopulateCommands();
        ExecuteCommand = new RelayCommand(OnExecute, _ => !IsExecuting);
        CancelCommand = new RelayCommand(_ => _psService.Cancel(), _ => IsExecuting);
        FillCommand = new RelayCommand(OnFillCommand);
        AddCustomCommand = new RelayCommand(_ => OpenAddDialog());
        EditCustomCommand = new RelayCommand(p => { if (p is CommandItem c) OpenEditDialog(c); });
        DeleteCustomCommand = new RelayCommand(p => { if (p is CommandItem c) DeleteCustom(c); });
        ResetSession = new RelayCommand(_ => ResetSessionCmd());

        // Repopulate when language changes
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ReloadAll();
                StatusText = L["Status.Ready"];
            });
        };
    }

    // ═══════════════════════════════════════════════════
    //  Properties
    // ═══════════════════════════════════════════════════

    private string _terminalOutput = string.Empty;
    public string TerminalOutput
    {
        get => _terminalOutput;
        set { _terminalOutput = value; OnPropertyChanged(); }
    }

    private string _commandInput = string.Empty;
    public string CommandInput
    {
        get => _commandInput;
        set { _commandInput = value; OnPropertyChanged(); }
    }

    private bool _isExecuting;
    public bool IsExecuting
    {
        get => _isExecuting;
        set { _isExecuting = value; OnPropertyChanged(); }
    }

    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private string _currentDir = "";
    public string CurrentDir
    {
        get => _currentDir;
        set { _currentDir = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CommandCategory> Categories { get; } = new();

    // ═══════════════════════════════════════════════════
    //  Commands
    // ═══════════════════════════════════════════════════

    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand FillCommand { get; }
    public ICommand AddCustomCommand { get; }
    public ICommand EditCustomCommand { get; }
    public ICommand DeleteCustomCommand { get; }
    public ICommand ResetSession { get; }

    private void OnLocationChanged(string path)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentDir = path;
        });
    }

    private void ResetSessionCmd()
    {
        _psService.ResetSession();
        CurrentDir = "";
    }

    public async void ExecutePowerShellCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        TerminalOutput += $"\nPS> {command}\n{new string('─', 60)}\n";
        CommandInput = string.Empty;
        IsExecuting = true;
        StatusText = L["Status.Executing"];

        try
        {
            await _psService.ExecuteAsync(command);
        }
        catch (Exception ex)
        {
            TerminalOutput += $"[Error] {ex.Message}\n";
        }
    }

    public void ClearTerminal()
    {
        TerminalOutput = string.Empty;
    }

    // ═══════════════════════════════════════════════════
    //  Callbacks
    // ═══════════════════════════════════════════════════

    private void OnOutputReceived(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            TerminalOutput += line + "\n";
        });
    }

    private void OnErrorReceived(string line)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            TerminalOutput += $"[Error] {line}\n";
        });
    }

    private void OnExecutionCompleted(bool success)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsExecuting = false;
            StatusText = success ? L["Status.Done"] : L["Status.Error"];
        });
    }

    // ═══════════════════════════════════════════════════
    //  Card Click Handler
    // ═══════════════════════════════════════════════════

    private void OnFillCommand(object? parameter)
    {
        if (parameter is not CommandItem cmd) return;

        if (cmd.HasParameters)
        {
            var dialog = new ParameterDialog(cmd)
            {
                Owner = Application.Current.MainWindow,
            };
            // Update dialog labels for current language
            dialog.Title = L["Dialog.Title"];
            var result = dialog.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(dialog.ResultCommand))
                CommandInput = dialog.ResultCommand;
        }
        else
        {
            CommandInput = cmd.PowerShellCommand;
        }
    }

    private void OnExecute(object? parameter)
    {
        var command = parameter as string ?? CommandInput;
        ExecutePowerShellCommand(command!);
    }

    // ═══════════════════════════════════════════════════
    //  Custom Command CRUD
    // ═══════════════════════════════════════════════════

    private static readonly string[] CategoryKeys =
        ["Category.Files", "Category.Processes", "Category.Services",
         "Category.Network", "Category.Software", "Category.Hardware",
         "Category.Users", "Category.Text"];

    private string[] GetCategoryNames() =>
        CategoryKeys.Select(k => L[k]).Append(L["Custom.CategoryName"]).ToArray();

    private void OpenAddDialog()
    {
        var dialog = new CommandEditorDialog(GetCategoryNames())
        {
            Owner = Application.Current.MainWindow,
        };
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            CustomCommandService.Instance.Add(dialog.Result);
            ReloadAll();
        }
    }

    private void OpenEditDialog(CommandItem cmd)
    {
        var userCmd = CustomCommandService.Instance.Commands.FirstOrDefault(c => c.Id == cmd.Id);
        if (userCmd == null) return;

        var dialog = new CommandEditorDialog(GetCategoryNames(), userCmd)
        {
            Owner = Application.Current.MainWindow,
        };
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            dialog.Result.Id = userCmd.Id;
            CustomCommandService.Instance.Update(dialog.Result);
            ReloadAll();
        }
    }

    private void DeleteCustom(CommandItem cmd)
    {
        var msg = string.Format(L["Custom.DeleteConfirm"], cmd.Name);
        if (MessageBox.Show(msg, L["Custom.DeleteTitle"], MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            CustomCommandService.Instance.Delete(cmd.Id);
            ReloadAll();
        }
    }

    private void ReloadAll()
    {
        Categories.Clear();
        PopulateCommands();
    }

    /// <summary>
    /// Convert a UserCommand to a CommandItem for display.
    /// </summary>
    private static CommandItem ToCommandItem(UserCommand uc) => new()
    {
        Id = uc.Id,
        Name = uc.Name,
        Description = uc.Description,
        Category = uc.Category,
        PowerShellCommand = uc.PowerShellCommand,
        Parameters = uc.Parameters,
        IsCustom = true,
    };

    // ═══════════════════════════════════════════════════
    //  Command Library
    // ═══════════════════════════════════════════════════

    private void PopulateCommands()
    {
        var cats = new List<CommandCategory>
        {
            // ── ⭐ My Commands (custom) ──
            new(L["Custom.CategoryName"], CustomCommandService.Instance.Commands.Select(ToCommandItem).ToList()),

            // ── 📁 Files & Directories ──
            new(L["Category.Files"], new List<CommandItem>
            {
                new() { Id="ls",         Name=L["Cmd.ls.Name"],        Description=L["Cmd.ls.Desc"],        PowerShellCommand="Get-ChildItem | Select-Object Name, Length, LastWriteTime | Out-String -Width 200" },
                new() { Id="cd",         Name="cd",                    Description="Change the current working directory", PowerShellCommand="Set-Location -Path '{path}' -ErrorAction Stop; Write-Host (Get-Location).Path",
                    Parameters = new() { new() { Key="path", Label="Path", Placeholder="C:\\", Required=true }, }},
                new() { Id="pwd",        Name=L["Cmd.pwd.Name"],       Description=L["Cmd.pwd.Desc"],       PowerShellCommand="Get-Location | Out-String" },
                new() { Id="disk",       Name=L["Cmd.disk.Name"],      Description=L["Cmd.disk.Desc"],      PowerShellCommand="Get-PSDrive -PSProvider FileSystem | Select-Object Name, @{N='Total(GB)';E={[math]::Round($_.Used/1GB+$_.Free/1GB,1)}}, @{N='Free(GB)';E={[math]::Round($_.Free/1GB,1)}}, @{N='Used(GB)';E={[math]::Round($_.Used/1GB,1)}} | Out-String -Width 200" },
                new() { Id="newfolder",  Name=L["Cmd.newfolder.Name"], Description=L["Cmd.newfolder.Desc"],PowerShellCommand="New-Item -Path '{path}' -ItemType Directory -ErrorAction Stop | Out-String; Write-Host 'Folder created'",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.newfolder.Param.path"], Placeholder="C:\\MyFolder", Required=true }, }},
                new() { Id="recent",     Name=L["Cmd.recent.Name"],    Description=L["Cmd.recent.Desc"],    PowerShellCommand="Get-ChildItem -Path '{path}' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First {top} Name, LastWriteTime, Length | Out-String -Width 200",
                    Parameters = new() {
                        new() { Key="path", Label=L["Cmd.recent.Param.path"], Placeholder="C:\\Users\\...", DefaultValue=".", Required=true },
                        new() { Key="top",  Label=L["Cmd.recent.Param.top"],  Placeholder="10", DefaultValue="10", Type=ParameterType.Number, Required=false },
                    }},
                new() { Id="find-large", Name=L["Cmd.findlarge.Name"], Description=L["Cmd.findlarge.Desc"],PowerShellCommand="Get-ChildItem -Path '{path}' -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Length -gt {sizeMB}MB } | Sort-Object Length -Descending | Select-Object Name, Length, DirectoryName | Out-String -Width 200",
                    Parameters = new() {
                        new() { Key="path",   Label=L["Cmd.findlarge.Param.path"],   Placeholder="C:\\", DefaultValue=".", Required=true },
                        new() { Key="sizeMB", Label=L["Cmd.findlarge.Param.sizeMB"], Placeholder="100", DefaultValue="100", Type=ParameterType.Number, Required=false },
                    }},
                new() { Id="dirsize",    Name=L["Cmd.dirsize.Name"],   Description=L["Cmd.dirsize.Desc"],   PowerShellCommand="(Get-ChildItem -Path '{path}' -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB | ForEach-Object { Write-Host ('Total: ' + [math]::Round($_,2) + ' MB') }",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.dirsize.Param.path"], Placeholder=".", DefaultValue=".", Required=true }, }},
                new() { Id="grep",       Name=L["Cmd.grep.Name"],      Description=L["Cmd.grep.Desc"],      PowerShellCommand="Get-ChildItem -Path '{path}' -Recurse -Filter '{filter}' -File -ErrorAction SilentlyContinue | Select-String -Pattern '{pattern}' -SimpleMatch | Select-Object Filename, LineNumber, Line -First 50 | Out-String -Width 300",
                    Parameters = new() {
                        new() { Key="path",    Label=L["Cmd.grep.Param.path"],    Placeholder=".", DefaultValue=".", Required=true },
                        new() { Key="pattern", Label=L["Cmd.grep.Param.pattern"], Placeholder="TODO", DefaultValue="TODO", Required=true },
                        new() { Key="filter",  Label=L["Cmd.grep.Param.filter"],  Placeholder="*.*", DefaultValue="*.*", Required=false },
                    }},
                new() { Id="hash",       Name=L["Cmd.hash.Name"],      Description=L["Cmd.hash.Desc"],      PowerShellCommand="Get-FileHash -Path '{path}' -Algorithm SHA256 | Select-Object Algorithm, Hash | Out-String",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.hash.Param.path"], Placeholder="Select a file...", Required=true }, }},
                new() { Id="recycle",    Name=L["Cmd.recycle.Name"],   Description=L["Cmd.recycle.Desc"],   PowerShellCommand="Clear-RecycleBin -Force -ErrorAction Stop; Write-Host 'Recycle Bin emptied'" },
                new() { Id="zip",        Name=L["Cmd.zip.Name"],       Description=L["Cmd.zip.Desc"],       PowerShellCommand="Compress-Archive -Path '{source}' -DestinationPath '{dest}' -Force -ErrorAction Stop; Write-Host \"Created: {dest}\"",
                    Parameters = new() { new() { Key="source", Label=L["Cmd.zip.Param.source"], Placeholder="C:\\MyFolder", Required=true }, new() { Key="dest", Label=L["Cmd.zip.Param.dest"], Placeholder="C:\\archive.zip", Required=true }, }},
                new() { Id="unzip",      Name=L["Cmd.unzip.Name"],     Description=L["Cmd.unzip.Desc"],     PowerShellCommand="Expand-Archive -Path '{source}' -DestinationPath '{dest}' -Force -ErrorAction Stop; Write-Host \"Extracted to: {dest}\"",
                    Parameters = new() { new() { Key="source", Label=L["Cmd.unzip.Param.source"], Placeholder="C:\\archive.zip", Required=true }, new() { Key="dest", Label=L["Cmd.unzip.Param.dest"], Placeholder="C:\\Extracted", Required=true }, }},
                new() { Id="copyfile",   Name=L["Cmd.copyfile.Name"],  Description=L["Cmd.copyfile.Desc"],  PowerShellCommand="$op = Copy-Item -Path '{source}' -Destination '{dest}' -PassThru -ErrorAction Stop; Write-Host \"Copied: $($op.FullName)\"",
                    Parameters = new() { new() { Key="source", Label=L["Cmd.copyfile.Param.source"], Placeholder="C:\\file.txt", Required=true }, new() { Key="dest", Label=L["Cmd.copyfile.Param.dest"], Placeholder="D:\\Backup\\file.txt", Required=true }, }},
                new() { Id="deleteold",  Name=L["Cmd.deleteold.Name"], Description=L["Cmd.deleteold.Desc"],PowerShellCommand="$count = (Get-ChildItem -Path '{path}' -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-{days}) }).Count; Get-ChildItem -Path '{path}' -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-{days}) } | Remove-Item -Force -ErrorAction SilentlyContinue; Write-Host \"Deleted $count files older than {days} days\"",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.deleteold.Param.path"], Placeholder="C:\\Logs", DefaultValue=".", Required=true }, new() { Key="days", Label=L["Cmd.deleteold.Param.days"], Placeholder="30", DefaultValue="30", Type=ParameterType.Number, Required=true }, }},
                new() { Id="testpath",   Name=L["Cmd.testpath.Name"],  Description=L["Cmd.testpath.Desc"],  PowerShellCommand="if (Test-Path '{path}') { Write-Host 'EXISTS: {path}'; Get-Item '{path}' | Select-Object FullName, Length, LastWriteTime | Out-String -Width 200 } else { Write-Host 'NOT FOUND: {path}' }",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.testpath.Param.path"], Placeholder="C:\\Windows\\System32", Required=true }, }},
            }),

            // ── 🔧 Process Management ──
            new(L["Category.Processes"], new List<CommandItem>
            {
                new() { Id="ps",         Name=L["Cmd.ps.Name"],         Description=L["Cmd.ps.Desc"],         PowerShellCommand="Get-Process | Sort-Object CPU -Descending | Select-Object -First 20 Name, Id, @{N='CPU(s)';E={[math]::Round($_.CPU,1)}}, @{N='Mem(MB)';E={[math]::Round($_.WorkingSet64/1MB,1)}} | Out-String -Width 200" },
                new() { Id="ps-mem",     Name=L["Cmd.psmem.Name"],      Description=L["Cmd.psmem.Desc"],      PowerShellCommand="Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 10 Name, Id, @{N='Mem(MB)';E={[math]::Round($_.WorkingSet64/1MB,1)}} | Out-String -Width 200" },
                new() { Id="ps-search",  Name=L["Cmd.pssearch.Name"],   Description=L["Cmd.pssearch.Desc"],   PowerShellCommand="Get-Process -Name '*{name}*' -ErrorAction SilentlyContinue | Select-Object Name, Id, @{N='CPU(s)';E={[math]::Round($_.CPU,1)}}, @{N='Mem(MB)';E={[math]::Round($_.WorkingSet64/1MB,1)}} | Out-String -Width 200",
                    Parameters = new() { new() { Key="name", Label=L["Cmd.pssearch.Param.name"], Placeholder="chrome, explorer, node...", DefaultValue="chrome", Required=true }, }},
                new() { Id="ps-detail",  Name=L["Cmd.psdetail.Name"],   Description=L["Cmd.psdetail.Desc"],   PowerShellCommand="$p = Get-Process -Name '{name}' -ErrorAction Stop; $p | Select-Object Name,Id,CPU,WorkingSet64,StartTime,Path,Company | Format-List | Out-String -Width 300",
                    Parameters = new() { new() { Key="name", Label=L["Cmd.psdetail.Param.name"], Placeholder="explorer (name or PID)", DefaultValue="explorer", Required=true }, }},
                new() { Id="kill",       Name=L["Cmd.kill.Name"],       Description=L["Cmd.kill.Desc"],       PowerShellCommand="Stop-Process -Name '{name}' {force} -ErrorAction Stop; Write-Host 'Kill signal sent'", IsDangerous=true,
                    Parameters = new() { new() { Key="name", Label=L["Cmd.kill.Param.name"], Placeholder="notepad", Required=true }, new() { Key="force", Label=L["Cmd.kill.Param.force"], Type=ParameterType.Switch, SwitchFlag="-Force", DefaultValue="true" }, }},
                new() { Id="ps-start",   Name=L["Cmd.psstart.Name"],    Description=L["Cmd.psstart.Desc"],    PowerShellCommand="Start-Process '{path}'; Write-Host \"Launched: {path}\"",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.psstart.Param.path"], Placeholder="notepad.exe or C:\\file.txt", Required=true }, }},
                new() { Id="pstree",     Name=L["Cmd.pstree.Name"],     Description=L["Cmd.pstree.Desc"],     PowerShellCommand="Get-CimInstance Win32_Process | Select-Object ProcessId, Name, ParentProcessId | Sort-Object ParentProcessId, ProcessId | Out-String -Width 200" },
            }),

            // ── ⚙️ Services & System ──
            new(L["Category.Services"], new List<CommandItem>
            {
                new() { Id="services",       Name=L["Cmd.services.Name"],      Description=L["Cmd.services.Desc"],      PowerShellCommand="Get-Service -ErrorAction SilentlyContinue | Sort-Object Status, Name | Select-Object Name, DisplayName, Status | Out-String -Width 200" },
                new() { Id="service-detail", Name=L["Cmd.svcdetail.Name"],     Description=L["Cmd.svcdetail.Desc"],     PowerShellCommand="$svc = Get-Service -Name '{name}' -ErrorAction Stop; Write-Host \"Name: $($svc.Name)\"; Write-Host \"Display: $($svc.DisplayName)\"; Write-Host \"Status: $($svc.Status)\"; Write-Host \"StartMode: $((Get-CimInstance Win32_Service -Filter \\\"Name='{name}'\\\").StartMode)\"; Get-Service -Name '{name}' -RequiredServices | Select-Object Name, DisplayName, Status | Out-String -Width 200",
                    Parameters = new() { new() { Key="name", Label=L["Cmd.svcdetail.Param.name"], Placeholder="Wuauserv, Spooler, Dhcp...", Required=true }, }},
                new() { Id="start-service",  Name=L["Cmd.startSvc.Name"],      Description=L["Cmd.startSvc.Desc"],      PowerShellCommand="Start-Service -Name '{name}' -ErrorAction Stop; Write-Host \"Service '{name}' started\"",
                    Parameters = new() { new() { Key="name", Label=L["Cmd.startSvc.Param.name"], Placeholder="Spooler", Required=true }, }},
                new() { Id="stop-service",   Name=L["Cmd.stopSvc.Name"],       Description=L["Cmd.stopSvc.Desc"],       PowerShellCommand="Stop-Service -Name '{name}' {force} -ErrorAction Stop; Write-Host \"Service '{name}' stopped\"", IsDangerous=true,
                    Parameters = new() { new() { Key="name", Label=L["Cmd.stopSvc.Param.name"], Placeholder="Spooler", Required=true }, new() { Key="force", Label=L["Cmd.stopSvc.Param.force"], Type=ParameterType.Switch, SwitchFlag="-Force", DefaultValue="false" }, }},
                new() { Id="sysinfo",        Name=L["Cmd.sysinfo.Name"],        Description=L["Cmd.sysinfo.Desc"],        PowerShellCommand="Get-ComputerInfo | Select-Object WindowsProductName, OsArchitecture, CsProcessors, CsTotalPhysicalMemory, CsSystemType | Out-String -Width 200" },
                new() { Id="uptime",         Name=L["Cmd.uptime.Name"],         Description=L["Cmd.uptime.Desc"],         PowerShellCommand="$boot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime; $uptime = (Get-Date) - $boot; Write-Host \"Boot: $boot\"; Write-Host \"Uptime: $($uptime.Days)d $($uptime.Hours)h $($uptime.Minutes)m\"" },
                new() { Id="envvar",         Name=L["Cmd.envvar.Name"],         Description=L["Cmd.envvar.Desc"],         PowerShellCommand="Get-ChildItem Env: | Sort-Object Name | Out-String -Width 300" },
                new() { Id="eventlog",       Name=L["Cmd.eventlog.Name"],       Description=L["Cmd.eventlog.Desc"],       PowerShellCommand="Get-EventLog -LogName '{log}' -Newest {count} -ErrorAction SilentlyContinue | Select-Object TimeGenerated, EntryType, Source, Message | Out-String -Width 300",
                    Parameters = new() { new() { Key="log", Label=L["Cmd.eventlog.Param.log"], Placeholder="System", DefaultValue="System", Required=true, Type=ParameterType.Dropdown, Choices=new(){"System","Application","Security"} }, new() { Key="count", Label=L["Cmd.eventlog.Param.count"], Placeholder="20", DefaultValue="20", Type=ParameterType.Number, Required=false }, }},
                new() { Id="drivers",        Name=L["Cmd.drivers.Name"],        Description=L["Cmd.drivers.Desc"],        PowerShellCommand="Get-WindowsDriver -Online | Where-Object Inbox -eq $false | Select-Object Driver, Version, Date, ProviderName | Sort-Object ProviderName | Out-String -Width 200" },
                new() { Id="tasksched",      Name=L["Cmd.tasksched.Name"],      Description=L["Cmd.tasksched.Desc"],      PowerShellCommand="Get-ScheduledTask | Where-Object State -ne 'Disabled' | Select-Object TaskName, State, TaskPath | Sort-Object TaskPath | Out-String -Width 300" },
                new() { Id="regquery",       Name=L["Cmd.regquery.Name"],       Description=L["Cmd.regquery.Desc"],       PowerShellCommand="Get-ItemProperty -Path '{path}' -ErrorAction Stop | Out-String -Width 300",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.regquery.Param.path"], Placeholder="HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion", Required=true }, }},
                new() { Id="psver",          Name=L["Cmd.psver.Name"],         Description=L["Cmd.psver.Desc"],         PowerShellCommand="$PSVersionTable | Out-String; Write-Host ''; Get-Host | Select-Object Version, UI | Out-String" },
                new() { Id="getdate",        Name=L["Cmd.getdate.Name"],       Description=L["Cmd.getdate.Desc"],       PowerShellCommand="Write-Host \"UTC : $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss'))\"; Write-Host \"Local: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')\"; Write-Host \"Unix : $([DateTimeOffset]::Now.ToUnixTimeSeconds())\"; Write-Host \"Day  : $(Get-Date -Format 'dddd')\"" },
                new() { Id="cleartemp",      Name=L["Cmd.cleartemp.Name"],     Description=L["Cmd.cleartemp.Desc"],     PowerShellCommand="$tmp = [System.IO.Path]::GetTempPath(); $count = (Get-ChildItem $tmp -Recurse -File -ErrorAction SilentlyContinue).Count; Remove-Item \"$tmp*\" -Recurse -Force -ErrorAction SilentlyContinue; Write-Host \"Cleared $count temp files from $tmp\"" },
                new() { Id="checkdisk",      Name=L["Cmd.checkdisk.Name"],     Description=L["Cmd.checkdisk.Desc"],     PowerShellCommand="Repair-Volume -DriveLetter '{drive}' -Scan -ErrorAction Stop; Write-Host 'Drive {drive}: scan complete'",
                    Parameters = new() { new() { Key="drive", Label=L["Cmd.checkdisk.Param.drive"], Placeholder="C", DefaultValue="C", Required=true }, }},
            }),

            // ── 🌐 Network Tools ──
            new(L["Category.Network"], new List<CommandItem>
            {
                new() { Id="ping",        Name=L["Cmd.ping.Name"],        Description=L["Cmd.ping.Desc"],        PowerShellCommand="& ping {target} -n {count} 2>&1 | Out-String -Width 200",
                    Parameters = new() { new() { Key="target", Label=L["Cmd.ping.Param.target"], Placeholder="baidu.com or 8.8.8.8", DefaultValue="baidu.com", Required=true }, new() { Key="count", Label=L["Cmd.ping.Param.count"], Placeholder="4", DefaultValue="4", Type=ParameterType.Number, Required=false }, }},
                new() { Id="tracert",     Name=L["Cmd.tracert.Name"],     Description=L["Cmd.tracert.Desc"],     PowerShellCommand="Test-NetConnection -ComputerName '{target}' -TraceRoute | Select-Object -ExpandProperty TraceRoute | Out-String -Width 200",
                    Parameters = new() { new() { Key="target", Label=L["Cmd.tracert.Param.target"], Placeholder="baidu.com", DefaultValue="baidu.com", Required=true }, }},
                new() { Id="nslookup",    Name=L["Cmd.nslookup.Name"],    Description=L["Cmd.nslookup.Desc"],    PowerShellCommand="Resolve-DnsName -Name '{target}' -ErrorAction SilentlyContinue | Select-Object Name, Type, IPAddress, TTL | Out-String -Width 200",
                    Parameters = new() { new() { Key="target", Label=L["Cmd.nslookup.Param.target"], Placeholder="baidu.com", DefaultValue="baidu.com", Required=true }, }},
                new() { Id="port-test",   Name=L["Cmd.porttest.Name"],    Description=L["Cmd.porttest.Desc"],    PowerShellCommand="Test-NetConnection -ComputerName '{target}' -Port {port} -WarningAction SilentlyContinue | Out-String -Width 200",
                    Parameters = new() { new() { Key="target", Label=L["Cmd.porttest.Param.target"], Placeholder="baidu.com", DefaultValue="baidu.com", Required=true }, new() { Key="port", Label=L["Cmd.porttest.Param.port"], Placeholder="443", DefaultValue="443", Type=ParameterType.Number, Required=true }, }},
                new() { Id="ip",          Name=L["Cmd.ip.Name"],          Description=L["Cmd.ip.Desc"],          PowerShellCommand="Get-NetIPAddress -AddressFamily IPv4 | Where-Object InterfaceAlias -notlike '*Loopback*' | Select-Object InterfaceAlias, IPAddress, PrefixLength | Out-String -Width 200" },
                new() { Id="publicip",    Name=L["Cmd.publicip.Name"],    Description=L["Cmd.publicip.Desc"],    PowerShellCommand="try { $ip = (Invoke-RestMethod -Uri 'https://ifconfig.me/ip' -TimeoutSec 5 -ErrorAction Stop).Trim(); Write-Host \"Public IP: $ip\" } catch { try { $ip = (Invoke-RestMethod -Uri 'https://httpbin.org/ip' -TimeoutSec 5 -ErrorAction Stop).Trim(); Write-Host \"Public IP: $ip\" } catch { Write-Host 'Unable to detect public IP (network may be restricted)' } }" },
                new() { Id="netstat",     Name=L["Cmd.netstat.Name"],     Description=L["Cmd.netstat.Desc"],     PowerShellCommand="Get-NetTCPConnection -State Listen | Select-Object LocalAddress, LocalPort, OwningProcess | Sort-Object LocalPort | Out-String -Width 200" },
                new() { Id="connections", Name=L["Cmd.connections.Name"], Description=L["Cmd.connections.Desc"], PowerShellCommand="Get-NetTCPConnection -State Established | Select-Object LocalAddress, LocalPort, RemoteAddress, RemotePort, OwningProcess | Sort-Object RemotePort | Out-String -Width 250" },
                new() { Id="route",       Name=L["Cmd.route.Name"],       Description=L["Cmd.route.Desc"],       PowerShellCommand="Get-NetRoute -AddressFamily IPv4 | Select-Object DestinationPrefix, NextHop, InterfaceAlias, RouteMetric | Sort-Object RouteMetric | Out-String -Width 200" },
                new() { Id="arp",         Name=L["Cmd.arp.Name"],         Description=L["Cmd.arp.Desc"],         PowerShellCommand="Get-NetNeighbor -AddressFamily IPv4 | Select-Object IPAddress, LinkLayerAddress, State | Sort-Object IPAddress | Out-String -Width 200" },
                new() { Id="dns-cache",   Name=L["Cmd.dnscache.Name"],    Description=L["Cmd.dnscache.Desc"],    PowerShellCommand="Get-DnsClientCache | Select-Object -First 20 Entry, Data, @{N='TTL(s)';E={$_.TimeToLive}} | Out-String -Width 200" },
                new() { Id="net-adapter", Name=L["Cmd.netadapter.Name"],  Description=L["Cmd.netadapter.Desc"],  PowerShellCommand="Get-NetAdapter | Select-Object Name, Status, LinkSpeed, InterfaceDescription | Out-String -Width 200" },
                new() { Id="wifipass",    Name=L["Cmd.wifipass.Name"],    Description=L["Cmd.wifipass.Desc"],    PowerShellCommand="netsh wlan show profile name='{ssid}' key=clear | Select-String 'Key Content' | Out-String",
                    Parameters = new() { new() { Key="ssid", Label=L["Cmd.wifipass.Param.ssid"], Placeholder="Your Wi-Fi SSID", Required=true }, }},
                new() { Id="firewall",    Name=L["Cmd.firewall.Name"],    Description=L["Cmd.firewall.Desc"],    PowerShellCommand="Get-NetFirewallRule -Direction Inbound -Enabled True | Select-Object DisplayName, Action, Profile | Sort-Object DisplayName | Out-String -Width 250" },
                new() { Id="netshare",    Name=L["Cmd.netshare.Name"],    Description=L["Cmd.netshare.Desc"],    PowerShellCommand="Get-SmbShare | Select-Object Name, Path, Description, ShareState | Out-String -Width 200" },
                new() { Id="flushdns",    Name=L["Cmd.flushdns.Name"],    Description=L["Cmd.flushdns.Desc"],    PowerShellCommand="Clear-DnsClientCache; Write-Host 'DNS cache flushed successfully'" },
                new() { Id="restartadapter",Name=L["Cmd.restartadapter.Name"],Description=L["Cmd.restartadapter.Desc"],PowerShellCommand="Restart-NetAdapter -Name '{name}' -ErrorAction Stop; Write-Host \"Adapter '{name}' restarted\"",
                    Parameters = new() { new() { Key="name", Label=L["Cmd.restartadapter.Param.name"], Placeholder="Ethernet", DefaultValue="Ethernet", Required=true }, }},
                new() { Id="netconfig",   Name=L["Cmd.netconfig.Name"],   Description=L["Cmd.netconfig.Desc"],   PowerShellCommand="Get-NetIPConfiguration | Select-Object InterfaceAlias, IPv4Address, IPv4DefaultGateway, DNSServer | Out-String -Width 200" },
            }),

            // ── 📦 Software & Updates ──
            new(L["Category.Software"], new List<CommandItem>
            {
                new() { Id="installed",  Name=L["Cmd.installed.Name"],  Description=L["Cmd.installed.Desc"],  PowerShellCommand="Get-ItemProperty HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\*, HKLM:\\Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\* | Where-Object DisplayName | Select-Object DisplayName, DisplayVersion, Publisher | Sort-Object DisplayName | Out-String -Width 200" },
                new() { Id="hotfix",     Name=L["Cmd.hotfix.Name"],     Description=L["Cmd.hotfix.Desc"],     PowerShellCommand="Get-HotFix | Sort-Object InstalledOn -Descending | Select-Object -First 20 HotFixID, Description, InstalledOn | Out-String -Width 200" },
                new() { Id="winget-list",Name=L["Cmd.winget.Name"],     Description=L["Cmd.winget.Desc"],     PowerShellCommand="winget list | Select-Object -Skip 2 | Out-String -Width 200" },
                new() { Id="startup",    Name=L["Cmd.startup.Name"],    Description=L["Cmd.startup.Desc"],    PowerShellCommand="Get-CimInstance Win32_StartupCommand | Select-Object Name, Command, User | Out-String -Width 300" },
                new() { Id="choco",      Name=L["Cmd.choco.Name"],      Description=L["Cmd.choco.Desc"],      PowerShellCommand="choco list --local-only 2>&1 | Out-String -Width 200" },
                new() { Id="download",   Name=L["Cmd.download.Name"],   Description=L["Cmd.download.Desc"],   PowerShellCommand="Invoke-WebRequest -Uri '{url}' -OutFile '{path}' -ErrorAction Stop; Write-Host \"Downloaded to: {path}\"",
                    Parameters = new() { new() { Key="url", Label=L["Cmd.download.Param.url"], Placeholder="https://example.com/file.zip", Required=true }, new() { Key="path", Label=L["Cmd.download.Param.path"], Placeholder="C:\\Users\\Public\\Downloads\\file.zip", Required=true }, }},
                new() { Id="exportcsv",  Name=L["Cmd.exportcsv.Name"],  Description=L["Cmd.exportcsv.Desc"],  PowerShellCommand="Invoke-Expression '{command}' | Export-Csv -Path '{path}' -NoTypeInformation -Encoding UTF8 -ErrorAction Stop; Write-Host \"Exported to: {path}\"",
                    Parameters = new() { new() { Key="command", Label=L["Cmd.exportcsv.Param.command"], Placeholder="Get-Process | Select Name,Id,CPU", Required=true }, new() { Key="path", Label=L["Cmd.exportcsv.Param.path"], Placeholder="C:\\report.csv", Required=true }, }},
            }),

            // ── 🖥️ Hardware & Performance ──
            new(L["Category.Hardware"], new List<CommandItem>
            {
                new() { Id="cpu",        Name=L["Cmd.cpu.Name"],        Description=L["Cmd.cpu.Desc"],        PowerShellCommand="Get-CimInstance Win32_Processor | Select-Object Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize | Out-String -Width 200" },
                new() { Id="memory",     Name=L["Cmd.memory.Name"],     Description=L["Cmd.memory.Desc"],     PowerShellCommand="$os = Get-CimInstance Win32_OperatingSystem; Write-Host ('Total: ' + [math]::Round($os.TotalVisibleMemorySize/1MB,1) + ' GB'); Write-Host ('Free:  ' + [math]::Round($os.FreePhysicalMemory/1MB,1) + ' GB'); Write-Host ('Used:  ' + [math]::Round(($os.TotalVisibleMemorySize-$os.FreePhysicalMemory)/1MB,1) + ' GB')" },
                new() { Id="gpu",        Name=L["Cmd.gpu.Name"],        Description=L["Cmd.gpu.Desc"],        PowerShellCommand="Get-CimInstance Win32_VideoController | Select-Object Name, AdapterRAM, DriverVersion, CurrentHorizontalResolution, CurrentVerticalResolution | Out-String -Width 200" },
                new() { Id="diskhealth", Name=L["Cmd.diskhealth.Name"], Description=L["Cmd.diskhealth.Desc"], PowerShellCommand="Get-PhysicalDisk | Select-Object FriendlyName, MediaType, HealthStatus, OperationalStatus, Size | Out-String -Width 200" },
                new() { Id="battery",    Name=L["Cmd.battery.Name"],    Description=L["Cmd.battery.Desc"],    PowerShellCommand="Get-CimInstance Win32_Battery | Select-Object Name, EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime | Out-String -Width 200" },
                new() { Id="perf-cpu",   Name=L["Cmd.perfcpu.Name"],    Description=L["Cmd.perfcpu.Desc"],    PowerShellCommand="(Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average; Write-Host '% CPU usage'" },
                new() { Id="monitor",    Name=L["Cmd.monitor.Name"],    Description=L["Cmd.monitor.Desc"],    PowerShellCommand="Get-CimInstance Win32_DesktopMonitor | Select-Object Name, ScreenWidth, ScreenHeight, MonitorManufacturerName | Out-String -Width 200" },
                new() { Id="audiodev",   Name=L["Cmd.audiodev.Name"],   Description=L["Cmd.audiodev.Desc"],   PowerShellCommand="Get-PnpDevice -Class AudioEndpoint | Select-Object FriendlyName, Status, InstanceId | Out-String -Width 200" },
                new() { Id="printers",   Name=L["Cmd.printers.Name"],   Description=L["Cmd.printers.Desc"],   PowerShellCommand="Get-Printer | Select-Object Name, DriverName, PortName, Shared, Published | Out-String -Width 200" },
            }),

            // ── 👤 User & Security ──
            new(L["Category.Users"], new List<CommandItem>
            {
                new() { Id="whoami",      Name=L["Cmd.whoami.Name"],      Description=L["Cmd.whoami.Desc"],      PowerShellCommand="whoami; whoami /groups | Out-String -Width 300" },
                new() { Id="localusers",  Name=L["Cmd.localusers.Name"],  Description=L["Cmd.localusers.Desc"],  PowerShellCommand="Get-LocalUser | Select-Object Name, Enabled, LastLogon, Description | Out-String -Width 200" },
                new() { Id="groups",      Name=L["Cmd.groups.Name"],      Description=L["Cmd.groups.Desc"],      PowerShellCommand="Get-LocalGroup | Select-Object Name, Description | Sort-Object Name | Out-String -Width 200" },
                new() { Id="groupmember", Name=L["Cmd.groupmember.Name"], Description=L["Cmd.groupmember.Desc"], PowerShellCommand="Get-LocalGroupMember -Group '{group}' -ErrorAction Stop | Select-Object Name, ObjectClass, PrincipalSource | Out-String -Width 200",
                    Parameters = new() { new() { Key="group", Label=L["Cmd.groupmember.Param.group"], Placeholder="Administrators", DefaultValue="Administrators", Required=true }, }},
                new() { Id="whologged",   Name=L["Cmd.whologged.Name"],   Description=L["Cmd.whologged.Desc"],   PowerShellCommand="query user | Out-String -Width 200" },
                new() { Id="passpolicy",  Name=L["Cmd.passpolicy.Name"],  Description=L["Cmd.passpolicy.Desc"],  PowerShellCommand="net accounts | Out-String -Width 200" },
                new() { Id="privs",       Name=L["Cmd.privs.Name"],       Description=L["Cmd.privs.Desc"],       PowerShellCommand="whoami /priv | Out-String -Width 300" },
                new() { Id="execpolicy",  Name=L["Cmd.execpolicy.Name"],  Description=L["Cmd.execpolicy.Desc"],  PowerShellCommand="Get-ExecutionPolicy -List | Out-String -Width 200" },
                new() { Id="fileacl",     Name=L["Cmd.fileacl.Name"],     Description=L["Cmd.fileacl.Desc"],     PowerShellCommand="Get-Acl -Path '{path}' | Format-List | Out-String -Width 300",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.fileacl.Param.path"], Placeholder="C:\\Windows", DefaultValue="C:\\Windows", Required=true }, }},
                new() { Id="sessions",    Name=L["Cmd.sessions.Name"],    Description=L["Cmd.sessions.Desc"],    PowerShellCommand="query session | Out-String -Width 200" },
                new() { Id="restorept",   Name=L["Cmd.restorept.Name"],   Description=L["Cmd.restorept.Desc"],   PowerShellCommand="Get-ComputerRestorePoint | Select-Object SequenceNumber, Description, CreationTime | Sort-Object CreationTime -Descending | Out-String -Width 300" },
                new() { Id="reboot",      Name=L["Cmd.reboot.Name"],      Description=L["Cmd.reboot.Desc"],      PowerShellCommand="Restart-Computer -Force -Confirm:$false", IsDangerous=true },
                new() { Id="shutdown",    Name=L["Cmd.shutdown.Name"],    Description=L["Cmd.shutdown.Desc"],    PowerShellCommand="Stop-Computer -Force -Confirm:$false", IsDangerous=true },
            }),

            // ── 📋 Text & Encoding ──
            new(L["Category.Text"], new List<CommandItem>
            {
                new() { Id="clipboard",  Name=L["Cmd.clipboard.Name"],  Description=L["Cmd.clipboard.Desc"],  PowerShellCommand="Get-Clipboard | Out-String -Width 300" },
                new() { Id="base64enc",  Name=L["Cmd.base64enc.Name"],  Description=L["Cmd.base64enc.Desc"],  PowerShellCommand="[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('{text}')) | Out-String",
                    Parameters = new() { new() { Key="text", Label=L["Cmd.base64enc.Param.text"], Placeholder="Hello World", DefaultValue="Hello World", Required=true }, }},
                new() { Id="base64dec",  Name=L["Cmd.base64dec.Name"],  Description=L["Cmd.base64dec.Desc"],  PowerShellCommand="[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{text}')) | Out-String",
                    Parameters = new() { new() { Key="text", Label=L["Cmd.base64dec.Param.text"], Placeholder="SGVsbG8gV29ybGQ=", DefaultValue="SGVsbG8gV29ybGQ=", Required=true }, }},
                new() { Id="linecount",  Name=L["Cmd.linecount.Name"],  Description=L["Cmd.linecount.Desc"],  PowerShellCommand="$c = Get-Content '{path}'; Write-Host ('Lines: ' + $c.Count); Write-Host ('Words: ' + ($c | ForEach-Object { ($_ -split '\\s+').Count } | Measure-Object -Sum).Sum); Write-Host ('Chars: ' + ($c | Measure-Object -Character).Characters)",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.linecount.Param.path"], Placeholder="Select a file...", Required=true }, }},
                new() { Id="findstr",    Name=L["Cmd.findstr.Name"],    Description=L["Cmd.findstr.Desc"],    PowerShellCommand="Get-ChildItem -Path '{path}' -Recurse -File -ErrorAction SilentlyContinue | Select-String -Pattern '{pattern}' -SimpleMatch | Select-Object Filename, LineNumber, Line -First 50 | Out-String -Width 300",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.findstr.Param.path"], Placeholder=".", DefaultValue=".", Required=true }, new() { Key="pattern", Label=L["Cmd.findstr.Param.pattern"], Placeholder="text to find", Required=true }, }},
                new() { Id="guid",       Name=L["Cmd.guid.Name"],       Description=L["Cmd.guid.Desc"],       PowerShellCommand="[Guid]::NewGuid().ToString() | Out-String" },
                new() { Id="checksum",   Name=L["Cmd.checksum.Name"],   Description=L["Cmd.checksum.Desc"],   PowerShellCommand="Get-FileHash -Path '{path}' -Algorithm SHA256 | Select-Object Algorithm, Hash | Out-String; Write-Host '---'; Get-FileHash -Path '{path}' -Algorithm MD5 | Select-Object Algorithm, Hash | Out-String",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.checksum.Param.path"], Placeholder="Select a file...", Required=true }, }},
                new() { Id="folderreport",Name=L["Cmd.folderreport.Name"],Description=L["Cmd.folderreport.Desc"],PowerShellCommand="Get-ChildItem -Path '{path}' -Directory -ErrorAction SilentlyContinue | ForEach-Object { $size = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum; [PSCustomObject]@{ Name=$_.Name; 'Size(MB)'=[math]::Round($size/1MB,2) } } | Sort-Object 'Size(MB)' -Descending | Out-String -Width 200",
                    Parameters = new() { new() { Key="path", Label=L["Cmd.folderreport.Param.path"], Placeholder=".", DefaultValue=".", Required=true }, }},
            }),
        };

        foreach (var cat in cats)
            Categories.Add(cat);
    }

    // ═══════════════════════════════════════════════════
    //  INotifyPropertyChanged
    // ═══════════════════════════════════════════════════

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class CommandCategory
{
    public string Name { get; }
    public ObservableCollection<CommandItem> Commands { get; } = new();

    public CommandCategory(string name, IEnumerable<CommandItem> commands)
    {
        Name = name;
        foreach (var cmd in commands)
            Commands.Add(cmd);
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
