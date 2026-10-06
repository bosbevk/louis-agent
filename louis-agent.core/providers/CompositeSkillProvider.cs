namespace louis_agent.core.providers;

/// <summary>
/// Composite skill provider that combines skills from multiple sources (files, MCP, etc.)
/// </summary>
public class CompositeSkillProvider : ISkillProvider
{
    private readonly List<ISkillProvider> _providers = new();
    private string? _combinedDocumentation;

    public CompositeSkillProvider(params ISkillProvider[] providers)
    {
        _providers.AddRange(providers.Where(p => p != null));
    }

    public List<MarkdownSkillLoader.SkillDef> Skills
    {
        get
        {
            var combined = new Dictionary<string, MarkdownSkillLoader.SkillDef>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var provider in _providers)
            {
                foreach (var skill in provider.Skills)
                {
                    // Later providers override earlier ones with same name
                    combined[skill.Name] = skill;
                }
            }
            
            return combined.Values.ToList();
        }
    }

    public string Documentation
    {
        get
        {
            if (_combinedDocumentation != null)
                return _combinedDocumentation;

            var docs = new System.Text.StringBuilder();
            
            foreach (var provider in _providers)
            {
                var providerDocs = provider.Documentation?.Trim();
                if (!string.IsNullOrWhiteSpace(providerDocs))
                {
                    docs.AppendLine(providerDocs);
                    docs.AppendLine();
                }
            }

            _combinedDocumentation = docs.ToString();
            return _combinedDocumentation;
        }
    }

    public void AddProvider(ISkillProvider provider)
    {
        if (provider != null)
        {
            _providers.Add(provider);
            _combinedDocumentation = null; // Invalidate cache
        }
    }
}

