using System.Text.RegularExpressions;
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.DotNet.Tests;

/// <summary>The pack's guide.</summary>
public class DotNetGuideTests
{
    /// <summary>The guide tells the model about tools, so a rename can't leave it naming one that's gone.</summary>
    [Test]
    public void TheGuide_NamesOnlyToolsThatExist()
    {
        var pack = new DotNetPack();
        IFileInfo guide = pack.Prompts.GetFileInfo(PackToolset.Guide(pack.Name));
        using var reader = new StreamReader(guide.CreateReadStream());
        var toolNames = typeof(DotNetTools).GetMethods().Select(m => m.Name).ToHashSet();

        var referenced = Regex.Matches(reader.ReadToEnd(), @"`(DotNet[A-Za-z]+)`").Select(m => m.Groups[1].Value)
            .Distinct().ToList();

        Assert.That(referenced, Does.Contain("DotNetBuild").And.Contain("DotNetTest"));
        Assert.That(referenced.Where(name => !toolNames.Contains(name)), Is.Empty);
    }
}
