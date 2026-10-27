
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Scripts.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the scripted tools (built from a script), returned to the model: what went wrong and what to do instead.</summary>
public enum ScriptToolMessage
{
    [Description("Error: unknown parameter '{Name}'. Parameters: {Known}.")]
    ScriptToolUnknownParameter,

    [Description("Error: missing required parameter '{Name}' ({Type}).")]
    ScriptToolMissingParameter,

    [Description("Error: parameter '{Name}' must be a {Type}, got {Value}.")]
    ScriptToolWrongType,

    [Description("Timeout must be a number of seconds between 1 and 600, got '{Value}'.")]
    ScriptTimeoutInvalid,

    [Description("Parameter '{Name}' has unsupported type '{Type}' (use {Supported}).")]
    ScriptParameterTypeUnsupported,

    [Description(
        "Cannot parse parameter line: '{Line}'. Expected: - `name` (string|integer|number|boolean, required|optional): description")]
    ScriptParameterLineInvalid,

    [Description("Tool name '{Name}' must be PascalCase letters/digits (3-64 chars), e.g. 'CsvSummary'.")]
    ScriptToolNameInvalid,

    [Description("Missing '- Implementation:' line followed by a fenced code block.")]
    ScriptImplementationMissing,

    [Description("Parameter '{Name}' is declared more than once.")]
    ScriptParameterDuplicate,

    [Description("'- Implementation:' must be followed by a fenced code block (```python, ```powershell or ```bash).")]
    ScriptImplementationNotFenced,

    [Description("Unsupported implementation language '{Language}' (use python, powershell or bash).")]
    ScriptLanguageUnsupported,

    [Description("The implementation code block is not closed with ```.")]
    ScriptFenceNotClosed,

    [Description("The implementation code block is empty.")]
    ScriptCodeEmpty,

    [Description("{Description} (Agent-built tool.)")]
    ScriptToolDescription,

    [Description("{Description} (Agent-built tool, pending user approval.)")]
    ScriptToolDescriptionPending,
}
