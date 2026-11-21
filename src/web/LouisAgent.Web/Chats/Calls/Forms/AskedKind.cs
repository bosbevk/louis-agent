namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>
/// What a form's field asks for, so the box suits it: a name, a phone number, an email address, one line, or a box
/// that grows. The web's own reading of the site pack's kinds, which it can't reference; both sides' tests hold the
/// names equal (ADR 0027).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum AskedKind
{
    Name,
    Phone,
    Email,
    Text,
    LongText,
}
