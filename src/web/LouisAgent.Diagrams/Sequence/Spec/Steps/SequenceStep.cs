using System.Text.Json.Serialization;

namespace LouisAgent.Diagrams.Sequence.Spec.Steps;

/// <summary>One thing that happens in a sequence, in its turn: a message, a reply, a note or a block of steps.</summary>
/// <remarks>
/// Composite: each kind of step is a record of its own, and a block is a step that holds steps, so a nested loop
/// needs no special case and the reader never polices which fields go together. The file names a step's kind
/// in its <c>step</c> field, which System.Text.Json reads to pick the record, in any place among the fields.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "step")]
[JsonDerivedType(typeof(MessageStep), "message")]
[JsonDerivedType(typeof(ReplyStep), "reply")]
[JsonDerivedType(typeof(NoteStep), "note")]
[JsonDerivedType(typeof(BlockStep), "block")]
public abstract record SequenceStep
{
    /// <summary>The participants the step names, so the reader can check each is in the chart.</summary>
    public abstract IReadOnlyList<string> Names();

    /// <summary>This step and every step inside it, in order, so a walk over a chart reaches into its blocks.</summary>
    public virtual IEnumerable<SequenceStep> All() => [this];
}
