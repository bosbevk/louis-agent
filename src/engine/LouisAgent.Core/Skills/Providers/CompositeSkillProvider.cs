using System.Text;

namespace LouisAgent.Core.Skills.Providers;

/// <summary>
/// Several prompt sources read as one, in the order they were added, each with a blank line after it.
/// </summary>
/// <remarks>
/// Composite: <c>SkillLoader</c> puts the principles, the mode, the guides and the identity's skills together, and
/// the engine reads the whole as it reads one file.
/// </remarks>
public class CompositeSkillProvider : ISkillProvider
{
    private readonly List<ISkillProvider> _providers = [];
    private string? _combinedDocumentation;

    /// <summary>The sources given, any null one left out.</summary>
    public CompositeSkillProvider(params ISkillProvider?[] providers) =>
        _providers.AddRange(providers.OfType<ISkillProvider>());

    /// <summary>Every source's text, trimmed, a blank one left out; worked out once until a source is added.</summary>
    public string Documentation
    {
        get
        {
            if (_combinedDocumentation is not null) return _combinedDocumentation;

            var docs = new StringBuilder();
            foreach (string? text in _providers.Select(p => p.Documentation?.Trim()))
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                docs.AppendLine(text);
                docs.AppendLine();
            }

            _combinedDocumentation = docs.ToString();
            return _combinedDocumentation;
        }
    }

    /// <summary>Adds a source at the end; a null one is left out.</summary>
    public void AddProvider(ISkillProvider? provider)
    {
        if (provider is null) return;

        _providers.Add(provider);
        // The combined text is worked out again on the next read.
        _combinedDocumentation = null;
    }
}
