using System.ComponentModel;

namespace LouisAgent.Composition.Tools.Scripts.Messages;

/// <summary>What the model reads when a script can't run because its language isn't on this machine.</summary>
public enum RunnerToolMessage
{
    [Description(
        "Python 3 is not installed or not on PATH. Install it (https://www.python.org/downloads/ or 'winget install Python.Python.3.12'), or set PYTHON_PATH to the interpreter. On Windows, the 'python' Microsoft Store alias does not count as an install.")]
    PythonNotInstalled,

    [Description(
        "PowerShell is not available. Install PowerShell 7 (https://aka.ms/powershell or 'winget install Microsoft.PowerShell'), or set POWERSHELL_PATH to pwsh/powershell.exe.")]
    PowerShellNotInstalled,

    [Description(
        "Bash is not available. On Windows install Git for Windows (https://git-scm.com, includes Git Bash) or set BASH_PATH to bash.exe. WSL's bash.exe in System32 is deliberately not used.")]
    BashNotInstalled,
}
