using LouisAgent.Choices;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Files;

namespace LouisAgent.Evals.Files;

/// <summary>
/// One thing code checks about a turn, as an eval file writes it: <c>used ReadDoc</c>, <c>under 30s</c>,
/// <c>never-says the admin key</c>. Its kind, and the one argument the kind takes, if any: a tool's name, the
/// seconds, the text, or an agent's address as it is written.
/// </summary>
/// <remarks>
/// Parsed once (ADR 0024), so code past the reader asks the kind and never reads the words again. The first word is
/// one of the seven by its wire name only, so "notused" is refused rather than read as a member's C# name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record Fact(FactKind Kind, string Argument)
{
    /// <summary>The letter that ends an <c>under</c> fact's seconds.</summary>
    private const char SecondsMark = 's';

    /// <summary>Reads a fact's words, or refuses them with the reason, as a <see cref="FormatException"/>.</summary>
    /// <remarks>Guard clauses: the first word first, then one switch on its kind for the rest (ADR 0051).</remarks>
    public static Fact Parse(string words)
    {
        string text = words.Trim();
        int space = text.IndexOf(' ');
        string first = space < 0 ? text : text[..space];
        string rest = space < 0 ? "" : text[(space + 1)..].Trim();

        var kind = KindOf(first);
        var notAFact = new NotAFactHoles(text, Kinds());
        if (kind is null) throw Refused(EvalFileMessage.NotAFact, notAFact);

        return kind.Value switch
        {
            FactKind.NoError => NoError(text, rest),
            FactKind.Used or FactKind.NotUsed => Tool(kind.Value, text, rest),
            FactKind.Under => Under(text, rest),
            FactKind.NeverSays => NeverSays(text, rest),
            _ => Address(kind.Value, text, rest)
        };
    }

    /// <summary>The fact as the file writes it.</summary>
    public override string ToString() => Kind switch
    {
        FactKind.NoError => Kind.Wire(),
        FactKind.Under => $"{Kind.Wire()} {Argument}{SecondsMark}",
        _ => $"{Kind.Wire()} {Argument}"
    };

    /// <summary>The kind whose wire name the first word is, the case ignored, or none.</summary>
    private static FactKind? KindOf(string first) =>
        Enum.GetValues<FactKind>().Cast<FactKind?>().FirstOrDefault(kind =>
            string.Equals(kind!.Value.Wire(), first, StringComparison.OrdinalIgnoreCase));

    /// <summary>The seven first words, read from the enum, for a refusal.</summary>
    private static string Kinds() => string.Join(", ", SettingChoice.Names<FactKind>());

    /// <summary><c>no-error</c> takes nothing after it.</summary>
    private static Fact NoError(string text, string rest)
    {
        var takesNothing = new FactTakesNothingHoles(text, FactKind.NoError.Wire());
        if (rest.Length > 0) throw Refused(EvalFileMessage.FactTakesNothing, takesNothing);
        return new Fact(FactKind.NoError, "");
    }

    /// <summary><c>used</c> and <c>not-used</c> name one tool, one word.</summary>
    private static Fact Tool(FactKind kind, string text, string rest)
    {
        var needsOneTool = new FactNeedsOneToolHoles(text, kind.Wire());
        if (rest.Length == 0 || rest.Contains(' ')) throw Refused(EvalFileMessage.FactNeedsOneTool, needsOneTool);
        return new Fact(kind, rest);
    }

    /// <summary><c>under</c> names a whole number of seconds above zero, ending in <c>s</c>.</summary>
    private static Fact Under(string text, string rest)
    {
        bool marked = rest.Length > 1 && rest[^1] == SecondsMark;
        bool counted = marked && int.TryParse(rest[..^1], out int seconds) && seconds > 0;
        var needsSeconds = new UnderNeedsSecondsHoles(text);
        if (!counted) throw Refused(EvalFileMessage.UnderNeedsSeconds, needsSeconds);
        return new Fact(FactKind.Under, rest[..^1]);
    }

    /// <summary><c>never-says</c> names the text the answer must not hold, whole.</summary>
    private static Fact NeverSays(string text, string rest)
    {
        var needsText = new NeverSaysNeedsTextHoles(text);
        if (rest.Length == 0) throw Refused(EvalFileMessage.NeverSaysNeedsText, needsText);
        return new Fact(FactKind.NeverSays, rest);
    }

    /// <summary><c>routed-to</c> and <c>continued</c> name an agent's address, kept as it is written.</summary>
    private static Fact Address(FactKind kind, string text, string rest)
    {
        var needsAddress = new FactNeedsAnAddressHoles(text, kind.Wire());
        if (!AgentAddress.TryParse(rest, out var address))
            throw Refused(EvalFileMessage.FactNeedsAnAddress, needsAddress);
        return new Fact(kind, address.ToString());
    }

    /// <summary>The refusal, in the message's words.</summary>
    private static FormatException Refused(EvalFileMessage message, object holes) =>
        new(MessageText.Format(message, holes));
}
