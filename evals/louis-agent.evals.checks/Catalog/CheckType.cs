using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Catalog;

/// <summary>
/// The base for a check type whose settings are a record: it reads the record from the eval file, and you only
/// say how to build your check from it.
/// </summary>
/// <remarks>
/// Factory Method (Gang of Four): reading and refusing bad settings happens once here, and each entry overrides
/// only the step that builds its own check. Settings go through a typed record, never a dictionary, so a wrong or
/// missing field is caught where the file is read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public abstract class CheckType<TParameters>(string type, CheckKind kind) : ICheckType
{
    /// <inheritdoc />
    public string Type => type;

    /// <inheritdoc />
    public CheckKind Kind => kind;

    /// <inheritdoc />
    public Type Parameters => typeof(TParameters);

    /// <inheritdoc />
    public ICheck Create(JsonElement parameters, CheckContext context) => Create(Read(parameters), context);

    /// <summary>
    /// Builds your check from settings that have already been read and checked, with the eval's folder and the
    /// checks built before it, should it need them.
    /// </summary>
    protected abstract ICheck Create(TParameters parameters, CheckContext context);

    /// <summary>
    /// Reads the settings into the record. If they don't fit, the refusal names this check type, and the JSON
    /// reader's own reason names the field.
    /// </summary>
    private TParameters Read(JsonElement parameters)
    {
        try
        {
            return parameters.Deserialize<TParameters>(EvalJson.Strict)
                   ?? throw Refuse(CheckCatalogMessage.NoParameters, new { Type = type });
        }
        catch (JsonException error)
        {
            throw Refuse(CheckCatalogMessage.BadParameters, new
            {
                Type = type,
                Reason = error.Message
            });
        }
    }

    /// <summary>The refusal with its text filled in.</summary>
    private static InvalidDataException Refuse(CheckCatalogMessage message, object values) =>
        new(MessageText.Format(message, values));
}
