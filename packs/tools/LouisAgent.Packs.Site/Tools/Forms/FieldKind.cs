namespace LouisAgent.Packs.Site.Tools.Forms;

/// <summary>
/// What a form's field asks for, so the visitor's browser offers the right box: a name, a phone number, an email
/// address, one line of text, or a box that grows for a longer answer such as a job told in their own words.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum FieldKind
{
    Name,
    Phone,
    Email,
    Text,
    LongText,
}
