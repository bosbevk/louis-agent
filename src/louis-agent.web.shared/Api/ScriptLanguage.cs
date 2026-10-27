namespace LouisAgent.Web.Shared.Api;

/// <summary>
/// The script languages the browser knows by name (ADR 0043): the code-review app's editor labels a script by the language of its file. The browser can't reference core, so this is its own copy of
/// core's <c>ScriptLanguage</c>; a test in <c>api.tests</c> checks the two agree. A member's lower-case name is its wire name.
/// </summary>
public enum ScriptLanguage
{
    Bash,
    PowerShell,
    Python,
}
