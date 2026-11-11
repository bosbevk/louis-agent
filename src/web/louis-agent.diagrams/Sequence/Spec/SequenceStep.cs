using System.Text.Json.Serialization;

namespace LouisAgent.Diagrams.Sequence.Spec;

/// <summary>One thing that happens in a sequence, in its turn: a message, a reply or a note.</summary>
/// <remarks>
/// Composite: each kind of step is a record of its own, and a block will be a step that holds steps, so a nested
/// loop needs no special case and the reader never polices which fields go together. The file names a step's kind
/// in its <c>step</c> field, which System.Text.Json reads to pick the record, in any place among the fields.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "step")]
[JsonDerivedType(typeof(MessageStep), "message")]
[JsonDerivedType(typeof(ReplyStep), "reply")]
[JsonDerivedType(typeof(NoteStep), "note")]
public abstract record SequenceStep
{
    /// <summary>The participants the step names, so the reader can check each is in the chart.</summary>
    public abstract IReadOnlyList<string> Names();
}

/// <summary>One participant calls another: a solid arrow with its words above it.</summary>
public sealed record MessageStep(string From, string To, string Text) : SequenceStep
{
    /// <summary>The caller and the one called.</summary>
    public override IReadOnlyList<string> Names() => [From, To];
}

/// <summary>The answer coming back: a dashed arrow, in the tone of whoever answers.</summary>
public sealed record ReplyStep(string From, string To, string Text) : SequenceStep
{
    /// <summary>The one answering and the one answered.</summary>
    public override IReadOnlyList<string> Names() => [From, To];
}

/// <summary>A word to the reader, on a panel over the lifelines it names: one, or a span of them.</summary>
public sealed record NoteStep(IReadOnlyList<string> Over, string Text) : SequenceStep
{
    /// <summary>The lifelines it stands over.</summary>
    public override IReadOnlyList<string> Names() => Over;
}
