using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Components.Blocks;

/// <summary>
/// How a box the agent asked for is drawn in the browser, by its kind: a form's fields (F127.4.3) and a choice that
/// holds a box (FX147) draw theirs alike, so a phone shows its number pad and the browser offers what it knows.
/// </summary>
public static class FieldInputs
{
    /// <summary>The browser's box for a kind, so a phone shows its number pad and an email its keyboard.</summary>
    public static string InputType(AskedKind kind) => kind switch
    {
        AskedKind.Phone => "tel",
        AskedKind.Email => "email",
        _ => "text"
    };

    /// <summary>What the browser may fill in for a kind from what it knows of the visitor.</summary>
    public static string AutoComplete(AskedKind kind) => kind switch
    {
        AskedKind.Name => "name",
        AskedKind.Phone => "tel",
        AskedKind.Email => "email",
        _ => "off"
    };
}
