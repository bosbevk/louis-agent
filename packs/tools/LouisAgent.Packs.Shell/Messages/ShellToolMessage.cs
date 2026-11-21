
using LouisAgent.Composition.Messages;
using System.ComponentModel;

namespace LouisAgent.Packs.Shell.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
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

    [Description(
        "Error: could not create the virtual environment at {Directory} (on Debian/Ubuntu install the 'python3-venv' package).\n{Output}")]
    VenvNotCreated,

    [Description("Script was still running after {Seconds}s and was killed.")]
    ScriptKilled,

    [Description("\nHint: the script has Windows (CRLF) line endings; convert it to LF.")]
    BashCrlfHint,

    [Description("pip install timed out.\n{Output}")]
    PipTimedOut,

    [Description("pip install FAILED (exit code {ExitCode}).\n{Output}")]
    PipFailed,

    [Description("created")] VenvCreated,

    [Description("not created yet; PythonInstallPackages creates it")]
    VenvNotCreatedYet,
}
