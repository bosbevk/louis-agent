namespace LouisAgent.Web.Evals.Admin.Studio.Schema;

/// <summary>
/// What a check's setting holds, as its schema says, so the form draws the right input and writes the right JSON:
/// text, a whole number, a number, yes or no, a list of words, or a reader picked from the catalogue's.
/// </summary>
public enum FieldKind
{
    Text,
    Whole,
    Number,
    YesNo,
    List,
    Reader,
}
