namespace LouisAgent.Core.Tools.Shell.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the shell toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum ShellToolMessage
{
    [Description("Error: pass exactly one of 'script' or 'scriptPath'.")]
    PassScriptOrScriptPath,

    [Description("Error: pass exactly one of 'code' or 'scriptPath'.")]
    PassCodeOrScriptPath,

    [Description("Error: script '{Path}' not found.")]
    ScriptNotFound,

    [Description("Error: scriptPath must be a .ps1 file.")]
    ScriptPathNotPs1,

    [Description("Error: specify at least one package.")]
    PackagesRequired,

    [Description("Error: '{Specifier}' is not a valid package specifier.")]
    PackageSpecifierInvalid,

    [Description("Error: could not create the virtual environment at {Directory} (on Debian/Ubuntu install the 'python3-venv' package).\n{Output}")]
    VenvNotCreated,

    [Description("Bash is not available. On Windows install Git for Windows (https://git-scm.com, includes Git Bash) or set BASH_PATH to bash.exe. WSL's bash.exe in System32 is deliberately not used.")]
    BashNotInstalled,

    [Description("PowerShell is not available. Install PowerShell 7 (https://aka.ms/powershell or 'winget install Microsoft.PowerShell'), or set POWERSHELL_PATH to pwsh/powershell.exe.")]
    PowerShellNotInstalled,

    [Description("Python 3 is not installed or not on PATH. Install it (https://www.python.org/downloads/ or 'winget install Python.Python.3.12'), or set PYTHON_PATH to the interpreter. On Windows, the 'python' Microsoft Store alias does not count as an install.")]
    PythonNotInstalled,

    [Description("Script was still running after {Seconds}s and was killed.")]
    ScriptKilled,

    [Description("\nHint: the script has Windows (CRLF) line endings; convert it to LF.")]
    BashCrlfHint,

    [Description("pip install timed out.\n{Output}")]
    PipTimedOut,

    [Description("pip install FAILED (exit code {ExitCode}).\n{Output}")]
    PipFailed,

    [Description("created")]
    VenvCreated,

    [Description("not created yet; PythonInstallPackages creates it")]
    VenvNotCreatedYet,
}
