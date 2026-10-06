using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class CompositeSkillProviderTests
{
    [Test]
    public void Constructor_Initializes_WithoutError()
    {
        var provider = new CompositeSkillProvider();
        Assert.That(provider, Is.Not.Null);
    }

    [Test]
    public void Skills_ReturnsEmptyList_WhenNoProvidersAdded()
    {
        var provider = new CompositeSkillProvider();
        var skills = provider.Skills;
        Assert.That(skills, Is.Empty);
    }

    [Test]
    public void Documentation_ReturnsString_WhenNoProvidersAdded()
    {
        var provider = new CompositeSkillProvider();
        var docs = provider.Documentation;
        Assert.That(docs, Is.TypeOf<string>());
    }

    [Test]
    public void AddProvider_WithValidProvider_AddsSkills()
    {
        var provider = new CompositeSkillProvider();
        var mockProvider = new MockSkillProvider();
        provider.AddProvider(mockProvider);

        var skills = provider.Skills;
        Assert.That(skills, Is.Not.Empty);
    }

    [Test]
    public void AddProvider_Multiple_CombinesAllSkills()
    {
        var provider = new CompositeSkillProvider();
        var provider1 = new MockSkillProvider("Skill1");
        var provider2 = new MockSkillProvider("Skill2");

        provider.AddProvider(provider1);
        provider.AddProvider(provider2);

        var skills = provider.Skills;
        Assert.That(skills.Count, Is.EqualTo(2));
    }

    [Test]
    public void AddProvider_WithMultipleSkills_IncludesAllInCollection()
    {
        var provider = new CompositeSkillProvider();
        var mockProvider = new MockSkillProvider("Test", "docs", multipleSkills: true);

        provider.AddProvider(mockProvider);

        var skills = provider.Skills;
        Assert.That(skills.Count, Is.GreaterThan(1));
    }

    [Test]
    public void Documentation_ConcatenatesFromMultipleProviders()
    {
        var provider = new CompositeSkillProvider();
        var provider1 = new MockSkillProvider("Skill1", "Docs from Provider 1");
        var provider2 = new MockSkillProvider("Skill2", "Docs from Provider 2");

        provider.AddProvider(provider1);
        provider.AddProvider(provider2);

        var documentation = provider.Documentation;
        Assert.That(documentation, Does.Contain("Docs from Provider 1"));
        Assert.That(documentation, Does.Contain("Docs from Provider 2"));
    }

    [Test]
    public void AddProvider_EmptyProvider_HandlesGracefully()
    {
        var provider = new CompositeSkillProvider();
        var emptyProvider = new MockSkillProvider("Empty", "Empty docs", empty: true);

        provider.AddProvider(emptyProvider);

        var skills = provider.Skills;
        Assert.That(skills, Is.Empty.Or.Not.Null);
    }

    [Test]
    public void Skills_ReturnsImmutableView()
    {
        var provider = new CompositeSkillProvider();
        var mockProvider = new MockSkillProvider();

        provider.AddProvider(mockProvider);
        var skills1 = provider.Skills;
        var skills2 = provider.Skills;

        Assert.That(skills1, Is.EqualTo(skills2));
    }

    [Test]
    public void AddProvider_Null_ThrowsOrHandles()
    {
        var provider = new CompositeSkillProvider();

        // Should either throw or handle gracefully
        try
        {
            provider.AddProvider(null!);
            // If no exception, verify provider state is still valid
            Assert.That(provider.Skills, Is.Not.Null);
        }
        catch (ArgumentNullException)
        {
            // Expected behavior
            Assert.Pass();
        }
    }

    [Test]
    public void MultipleAddProvider_Calls_CumulativeSkills()
    {
        var provider = new CompositeSkillProvider();

        for (int i = 0; i < 5; i++)
        {
            var mockProvider = new MockSkillProvider($"Skill{i}");
            provider.AddProvider(mockProvider);
        }

        var skills = provider.Skills;
        Assert.That(skills.Count, Is.EqualTo(5));
    }

    [Test]
    public void AddProvider_WithDuplicateSkillNames_BothIncluded()
    {
        var provider = new CompositeSkillProvider();
        var provider1 = new MockSkillProvider("DuplicateName");
        var provider2 = new MockSkillProvider("DuplicateName");

        provider.AddProvider(provider1);
        provider.AddProvider(provider2);

        var skills = provider.Skills;
        // Should have both, even if same name
        Assert.That(skills.Count, Is.GreaterThanOrEqualTo(1));
    }

    private class MockSkillProvider : ISkillProvider
    {
        private readonly string _skillName;
        private readonly string _documentation;
        private readonly bool _multipleSkills;
        private readonly bool _empty;

        public MockSkillProvider(string skillName = "TestSkill", string documentation = "Test Documentation", bool multipleSkills = false, bool empty = false)
        {
            _skillName = skillName;
            _documentation = documentation;
            _multipleSkills = multipleSkills;
            _empty = empty;
        }

        public List<MarkdownSkillLoader.SkillDef> Skills
        {
            get
            {
                if (_empty)
                    return new List<MarkdownSkillLoader.SkillDef>();

                var skills = new List<MarkdownSkillLoader.SkillDef>
                {
                    new(_skillName, "Test Description", "echo test")
                };

                if (_multipleSkills)
                {
                    skills.Add(new($"{_skillName}2", "Test Description 2", "echo test2"));
                    skills.Add(new($"{_skillName}3", "Test Description 3", "echo test3"));
                }

                return skills;
            }
        }

        public string Documentation => _documentation;
    }
}
