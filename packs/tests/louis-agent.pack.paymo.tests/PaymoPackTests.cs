using System.Text.RegularExpressions;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Paymo.Tests;

/// <summary>The pack's guide and its one setting.</summary>
public class PaymoPackTests
{
    /// <summary>The guide tells the model about tools, so every tool it names must exist.</summary>
    [Test]
    public void TheGuide_NamesOnlyToolsThatExist()
    {
        var pack = new PaymoPack(new PaymoOptions("key"));
        IFileInfo guide = pack.Prompts.GetFileInfo(PackToolset.Guide(pack.Name));
        using var reader = new StreamReader(guide.CreateReadStream());
        var toolNames = typeof(PaymoTools).GetMethods().Select(m => m.Name).ToHashSet();

        var referenced = Regex.Matches(reader.ReadToEnd(), @"`([A-Z][A-Za-z]+)\(").Select(m => m.Groups[1].Value)
            .Distinct().ToList();

        Assert.That(referenced, Does.Contain("LogTimeByTaskName"));
        Assert.That(referenced.Where(name => !toolNames.Contains(name)), Is.Empty);
    }

    /// <summary>Without a key the toolset says what it's missing, and with one it's ready.</summary>
    [Test]
    public void TheToolset_NeedsTheKey()
    {
        Assert.That(new PaymoPack(new PaymoOptions()).Toolsets.Single().MissingSettings(null!),
            Is.EqualTo(new[] { "PAYMO_API_KEY" }));
        Assert.That(new PaymoPack(new PaymoOptions("key")).Toolsets.Single().MissingSettings(null!), Is.Empty);
    }

    /// <summary>What the model calls, with the parameter names it was told about.</summary>
    [Test]
    public void TheLogTimeTool_HasItsParameters()
    {
        var parameters = typeof(PaymoTools).GetMethod(nameof(PaymoTools.LogTimeByTaskName))!.GetParameters()
            .Select(p => p.Name);

        Assert.That(parameters, Is.SupersetOf(new[] { "taskName", "startTime", "endTime", "hours", "notes", "date" }));
    }
}
