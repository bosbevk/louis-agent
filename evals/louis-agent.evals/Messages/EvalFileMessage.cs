using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// What the eval file's reader says when it turns a file away, in words the person who wrote it reads.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum EvalFileMessage
{
    [Description("eval folder '{Folder}' has no eval.json")]
    NoFile,

    [Description("eval file '{File}' can't be read: {Reason}")]
    Unreadable,

    [Description("eval file '{File}' holds no eval")]
    Empty,

    [Description("eval file '{File}' has schema_version {Version}; this reader reads 1")]
    SchemaVersion,

    [Description("eval file '{File}' names no modes, so there is nothing to run")]
    NoModes,

    [Description("eval file '{File}' names no checks, so there is nothing to grade with")]
    NoChecks,

    [Description("eval file '{File}' names the mode '{Name}' twice")]
    ModeTwice,

    [Description("eval file '{File}' names the check '{Name}' twice; give one its own name")]
    CheckTwice
}
