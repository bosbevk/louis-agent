using System.Collections;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Messages.Holes;

namespace LouisAgent.Composition.Words;

/// <summary>
/// The words of something's parts, a page's steps or a pack's texts, keyed by an enum of the parts: every part has its
/// words, and they read in the enum's order, so what showed a list shows them the same.
/// </summary>
/// <remarks>
/// A Factory Method over a Registry (CL68.1, Louis's pick of three: "put this in a dictionary factory and make it a
/// rule"; a framework function in Composition, his pick, so every identity, pack, host and site can use it).
/// <see cref="Of"/> asks the switch for each part's words once, when the class that holds them loads, and refuses a
/// part it has none for, so nothing shows a gap. Chosen over the words in a JSON file beside the page, and over an
/// enum member with a description per text.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0132-words-come-from-a-dictionary-factory.md">ADR 0132: words come from a dictionary factory</seealso>
public sealed class PartWords<TPart, TValue> : IReadOnlyDictionary<TPart, TValue>
    where TPart : struct, Enum
{
    private readonly Dictionary<TPart, TValue> _words;

    private PartWords(Dictionary<TPart, TValue> words, IReadOnlyList<TValue> inOrder)
    {
        _words = words;
        InOrder = inOrder;
    }

    /// <summary>The words, in the enum's order, for a part of the framework that shows a list.</summary>
    public IReadOnlyList<TValue> InOrder { get; }

    /// <summary>
    /// The words of every part, each from <paramref name="words"/>, usually a switch with an arm per part. A part it
    /// throws for, or has nothing for, is refused, naming the part.
    /// </summary>
    public static PartWords<TPart, TValue> Of(Func<TPart, TValue> words)
    {
        var byPart = new Dictionary<TPart, TValue>();
        var inOrder = new List<TValue>();
        foreach (var part in Enum.GetValues<TPart>())
        {
            var value = WordsOf(part, words);
            byPart[part] = value;
            inOrder.Add(value);
        }

        return new PartWords<TPart, TValue>(byPart, inOrder);
    }

    /// <summary>One part's words, or the refusal that names it.</summary>
    private static TValue WordsOf(TPart part, Func<TPart, TValue> words)
    {
        TValue? value;
        try
        {
            value = words(part);
        }
        catch (ArgumentOutOfRangeException)
        {
            value = default;
        }

        if (value is not null) return value;

        var missing = new PartWordsMissingHoles(typeof(TPart).Name, part.ToString());
        throw new InvalidOperationException(MessageText.Format(WordsErrorMessage.PartWordsMissing, missing));
    }

    /// <inheritdoc/>
    public TValue this[TPart key] => _words[key];

    /// <inheritdoc/>
    public IEnumerable<TPart> Keys => _words.Keys;

    /// <inheritdoc/>
    public IEnumerable<TValue> Values => InOrder;

    /// <inheritdoc/>
    public int Count => _words.Count;

    /// <inheritdoc/>
    public bool ContainsKey(TPart key) => _words.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(TPart key, out TValue value) => _words.TryGetValue(key, out value!);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TPart, TValue>> GetEnumerator() => _words.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
