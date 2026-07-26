<p align="center">
  <img src="https://raw.githubusercontent.com/PowerShell/PowerShell/master/assets/ps_black_128.svg" width="80" alt="PowerShell Panel" />
</p>

<h1 align="center">PowerShell Panel</h1>

<p align="center">
  <strong>A visual command panel for PowerShell — no memorization, no typing, <em>anyone</em> can use it.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/WPF-Windows-blue?logo=windows" alt="WPF" />
  <img src="https://img.shields.io/badge/PowerShell-7.4-5391FE?logo=powershell" alt="PowerShell" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License" />
</p>

<p align="center">
  <a href="#english">English</a> ·
  <a href="#简体中文">简体中文</a> ·
  <a href="#繁體中文">繁體中文</a>
</p>

---

<a name="english"></a>
## 🇺🇸 English

### What is PowerShell Panel?

PowerShell Panel is a **desktop GUI wrapper** for PowerShell. It puts common system management tasks into clickable cards on the left, and shows a real-time terminal on the right — so you can see exactly what command is being run and what the output looks like.

Think of it as **GitHub Desktop for PowerShell**: you get the power, without the syntax.

### Features

- **67 built-in commands** across 8 categories: Files, Processes, Services, Network, Software, Hardware, Users, Text tools
- **Parameter dialogs** — commands like Ping, Kill Process, DNS Lookup pop up a form for you to fill in the blanks
- **Real-time terminal** — every command shows as `PS> ...` with colorized output on the right
- **3 languages** — English, 简体中文, 繁體中文; switch in Settings (⚙) without restart
- **Zero-dependency PowerShell** — uses `System.Management.Automation` in-process; no `powershell.exe` subprocess

### Screenshot

```
┌─────────────────────────────────────────────────────────┐
│  PowerShell Panel                              ⚙       │
│  Visual command panel · Click to fill, press Execute   │
├──────────────────────┬──────────────────────────────────┤
│ ┌──────────────────┐ │  PowerShell · Real-time output    │
│ │ type a command…  │▶│ ──────────────────────────────── │
│ └──────────────────┘ │                                  │
│                      │  PS> Get-Process | Sort CPU      │
│ 📁 Files & Directories│ ──────────────────────────────── │
│ ┌──────────────────┐ │  Name    Id    CPU(s)  Mem(MB)   │
│ │ List Files       │ │  chrome  1234  125.3   824.1     │
│ │ List files and…  │ │  node     567   82.1   342.7     │
│ └──────────────────┘ │  ...                             │
│ ┌──────────────────┐ │                                  │
│ │ Current Path     │ │                                  │
│ └──────────────────┘ │                                  │
│ ...                  │                                  │
├──────────────────────┴──────────────────────────────────┤
│ 🟢 Ready                         PowerShell 7 · WPF     │
└─────────────────────────────────────────────────────────┘
```

### Installation

Download the latest `PowerShellPanel.exe` from [Releases](https://github.com/9yanliang99/PowerShellPanel/releases).

Requires:
- **Windows 10+** (Windows 7/8 may work but not tested)
- **.NET 8 Desktop Runtime** ([download](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- **PowerShell 7** (optional — falls back to Windows PowerShell 5.1)

Or build from source:

```bash
git clone https://github.com/9yanliang99/PowerShellPanel.git
cd PowerShellPanel
dotnet build -c Release
# output at: src/PowerShellPanel/bin/Release/net8.0-windows/
```

### Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | WPF (.NET 8) |
| Architecture | MVVM |
| PowerShell | `System.Management.Automation` (in-process) |
| i18n | JSON resource files + markup extension |
| Terminal | Custom RichTextBox with colorization |

### Development

```bash
# Restore
dotnet restore

# Build & Run
dotnet build -c Debug
dotnet run --project src/PowerShellPanel

# Publish (single-file)
dotnet publish src/PowerShellPanel -c Release -r win-x64 --self-contained false
```

### License

MIT

---

<a name="简体中文"></a>
## 🇨🇳 简体中文

### 这是什么？

**PowerShell Panel** 是一个 PowerShell 的桌面可视化面板。左侧是分类好的命令卡片，右侧是实时终端——点击卡片填入命令，按执行即可运行，完全不需要记住任何 PowerShell 语法。

类比：**PowerShell 版的 GitHub Desktop**——你拥有 PowerShell 的全部能力，但不需要学命令行。

### 功能

- **67 条内置命令**，分为 8 大类：文件、进程、服务、网络、软件、硬件、用户、文本工具
- **参数弹窗** —— Ping、结束进程、DNS 解析等命令会弹出表单让你填写参数
- **实时终端** —— 每条命令以 `PS>` 开头显示在右侧，带语法着色
- **三语切换** —— 英语 / 简体中文 / 繁體中文，点击 ⚙ 设置即可切换
- **进程内 PowerShell** —— 使用 `System.Management.Automation` SDK 直接调用，不开子进程

### 安装

从 [Releases](https://github.com/9yanliang99/PowerShellPanel/releases) 下载 `PowerShellPanel.exe`。

需要：
- **Windows 10 及以上**
- **.NET 8 Desktop Runtime**（[下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)）

或从源码构建：

```bash
git clone https://github.com/9yanliang99/PowerShellPanel.git
cd PowerShellPanel
dotnet build -c Release
```

### 技术栈

| 层面 | 技术 |
|------|------|
| UI | WPF (.NET 8) |
| 架构 | MVVM |
| PowerShell | `System.Management.Automation` 同进程调用 |
| 国际化 | JSON 资源文件 + 自定义 MarkupExtension |
| 终端 | 自定义 RichTextBox + 语法着色 |

---

<a name="繁體中文"></a>
## 🇹🇼 繁體中文

### 這是什麼？

**PowerShell Panel** 是一個 PowerShell 的桌面視覺化面板。左側是分類好的命令卡片，右側是即時終端機——點擊卡片填入命令，按執行即可運行，完全不需要記住任何 PowerShell 語法。

類比：**PowerShell 版的 GitHub Desktop**——你擁有 PowerShell 的全部能力，但不需要學命令列。

### 功能特色

- **67 條內建命令**，分為 8 大類：檔案、處理程序、服務、網路、軟體、硬體、使用者、文字工具
- **參數對話框** —— Ping、結束處理程序、DNS 查詢等命令會彈出表單讓你填寫參數
- **即時終端機** —— 每條命令以 `PS>` 開頭顯示在右側，帶語法著色
- **三語切換** —— 英文 / 簡體中文 / 繁體中文，點擊 ⚙ 設定即可切換
- **處理程序內 PowerShell** —— 使用 `System.Management.Automation` SDK 直接呼叫，不開啟子處理程序

### 安裝

從 [Releases](https://github.com/9yanliang99/PowerShellPanel/releases) 下載 `PowerShellPanel.exe`。

需要：
- **Windows 10 以上**
- **.NET 8 Desktop Runtime**（[下載](https://dotnet.microsoft.com/zh-tw/download/dotnet/8.0)）

或從原始碼建置：

```bash
git clone https://github.com/9yanliang99/PowerShellPanel.git
cd PowerShellPanel
dotnet build -c Release
```

### 技術堆疊

| 層面 | 技術 |
|------|------|
| UI | WPF (.NET 8) |
| 架構 | MVVM |
| PowerShell | `System.Management.Automation` 同處理程序呼叫 |
| 國際化 | JSON 資源檔 + 自訂 MarkupExtension |
| 終端機 | 自訂 RichTextBox + 語法著色 |

---

<p align="center">
  <sub>Built with WPF, .NET 8, and ❤️ — because the command line should be for everyone.</sub>
</p>
