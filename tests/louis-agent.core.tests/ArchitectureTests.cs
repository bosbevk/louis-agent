using System.Text.RegularExpressions;
using louis_agent.core;

namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Project layout": core's folders split the code by concern, a file's namespace is its folder path,
// a file holds one top-level type, and dependencies point one way (rule 2). Read from the source files, so it checks
// what a reviewer would read.
public partial class ArchitectureTests
{
    private const string RootNamespace = "LouisAgent.Core";

    // A folder may use its own layer and the ones below; Tools never uses Llm (a tool doesn't call a model).
    private static readonly Dictionary<string, int> Layers = new()
    {
        ["Config"] = 0, ["Messages"] = 0,
        ["Guardrails"] = 1,
        ["Llm"] = 2, ["Tools"] = 2, ["Skills"] = 2, ["Usage"] = 2, ["Pricing"] = 2, ["Protocols"] = 2,
        ["Agent"] = 3,
        ["Hosting"] = 4,
    };

    // Today's violations, while CL1 (docs/plans/CL1-project-layout.md) moves the folders one by one: each move removes
    // its entries, and the list is empty when the layout is done. A ratchet: a new violation fails, and so does an entry
    // that no longer violates, so the list can only shrink.
    private static readonly HashSet<string> Pending =
    [
        "AgentHost.cs: namespace louis_agent.core, expected LouisAgent.Core",
        "AgentHost.cs: 2 top-level types, one per file",
        "AgentHost.cs: '(root)' isn't a concern folder",
        "AgentLog.cs: namespace louis_agent.core, expected LouisAgent.Core",
        "AgentLog.cs: '(root)' isn't a concern folder",
        "MarkdownSkillLoader.cs: namespace louis_agent.core, expected LouisAgent.Core",
        "MarkdownSkillLoader.cs: '(root)' isn't a concern folder",
        "config/AgentOptions.cs: namespace louis_agent.core.config, expected LouisAgent.Core.config",
        "config/AgentOptions.cs: 'config' isn't a concern folder",
        "config/EnvFile.cs: namespace louis_agent.core.config, expected LouisAgent.Core.config",
        "config/EnvFile.cs: 'config' isn't a concern folder",
        "config/LlmOptions.cs: namespace louis_agent.core.config, expected LouisAgent.Core.config",
        "config/LlmOptions.cs: 2 top-level types, one per file",
        "config/LlmOptions.cs: 'config' isn't a concern folder",
        "mcp/RiderMcpClient.cs: namespace louis_agent.core.mcp, expected LouisAgent.Core.mcp",
        "mcp/RiderMcpClient.cs: 2 top-level types, one per file",
        "mcp/RiderMcpClient.cs: 'mcp' isn't a concern folder",
        "mcp/RiderMcpToolDiscovery.cs: namespace louis_agent.core.mcp, expected LouisAgent.Core.mcp",
        "mcp/RiderMcpToolDiscovery.cs: 'mcp' isn't a concern folder",
        "messages/MessageEnums.cs: namespace louis_agent.core.messages, expected LouisAgent.Core.messages",
        "messages/MessageEnums.cs: 4 top-level types, one per file",
        "messages/MessageEnums.cs: 'messages' isn't a concern folder",
        "messages/Messages.cs: namespace louis_agent.core.messages, expected LouisAgent.Core.messages",
        "messages/Messages.cs: 'messages' isn't a concern folder",
        "providers/AnthropicPromptCaching.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/AnthropicPromptCaching.cs: 'providers' isn't a concern folder",
        "providers/CodeReviewSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/CodeReviewSkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/CompositeSkillProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/CompositeSkillProvider.cs: 'providers' isn't a concern folder",
        "providers/DefaultSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/DefaultSkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/DevOpsSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/DevOpsSkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/DotNetSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/DotNetSkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/IPromptCachingStrategy.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/IPromptCachingStrategy.cs: 'providers' isn't a concern folder",
        "providers/ISkillProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/ISkillProvider.cs: 'providers' isn't a concern folder",
        "providers/LlmClientFactory.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/LlmClientFactory.cs: 2 top-level types, one per file",
        "providers/LlmClientFactory.cs: 'providers' isn't a concern folder",
        "providers/MarkdownSkillProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/MarkdownSkillProvider.cs: 'providers' isn't a concern folder",
        "providers/NoPromptCaching.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/NoPromptCaching.cs: 'providers' isn't a concern folder",
        "providers/PaymoSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/PaymoSkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/PersonalitySkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/PersonalitySkillsProvider.cs: 'providers' isn't a concern folder",
        "providers/PromptCachingChatClient.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/PromptCachingChatClient.cs: 'providers' isn't a concern folder",
        "providers/TimeLoggingSkillsProvider.cs: namespace louis_agent.core.providers, expected LouisAgent.Core.providers",
        "providers/TimeLoggingSkillsProvider.cs: 'providers' isn't a concern folder",
        "tools/AgentEngine.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/AgentEngine.cs: 'tools' isn't a concern folder",
        "tools/BashTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/BashTools.cs: 'tools' isn't a concern folder",
        "tools/DevOpsTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/DevOpsTools.cs: 'tools' isn't a concern folder",
        "tools/DotNetTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/DotNetTools.cs: 'tools' isn't a concern folder",
        "tools/GitTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/GitTools.cs: 'tools' isn't a concern folder",
        "tools/PaymoTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/PaymoTools.cs: 'tools' isn't a concern folder",
        "tools/PowerShellTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/PowerShellTools.cs: 'tools' isn't a concern folder",
        "tools/ProcessRunner.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/ProcessRunner.cs: 'tools' isn't a concern folder",
        "tools/PythonTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/PythonTools.cs: 'tools' isn't a concern folder",
        "tools/ScriptTool.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/ScriptTool.cs: 3 top-level types, one per file",
        "tools/ScriptTool.cs: 'tools' isn't a concern folder",
        "tools/WebSearchProviders.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/WebSearchProviders.cs: 8 top-level types, one per file",
        "tools/WebSearchProviders.cs: 'tools' isn't a concern folder",
        "tools/WebTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/WebTools.cs: 'tools' isn't a concern folder",
        "tools/WorkspaceTools.cs: namespace louis_agent.core.tools, expected LouisAgent.Core.tools",
        "tools/WorkspaceTools.cs: 'tools' isn't a concern folder",
        "usage/CacheGaps.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/CacheGaps.cs: 3 top-level types, one per file",
        "usage/CacheGaps.cs: 'usage' isn't a concern folder",
        "usage/CostCalculator.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/CostCalculator.cs: 'usage' isn't a concern folder",
        "usage/DisplayCurrency.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/DisplayCurrency.cs: 'usage' isn't a concern folder",
        "usage/ExchangeRates.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/ExchangeRates.cs: 'usage' isn't a concern folder",
        "usage/IRatesMapper.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/IRatesMapper.cs: 4 top-level types, one per file",
        "usage/IRatesMapper.cs: 'usage' isn't a concern folder",
        "usage/IUsageMapper.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/IUsageMapper.cs: 3 top-level types, one per file",
        "usage/IUsageMapper.cs: 'usage' isn't a concern folder",
        "usage/IUsageSink.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/IUsageSink.cs: 'usage' isn't a concern folder",
        "usage/JsonlUsageSink.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/JsonlUsageSink.cs: 'usage' isn't a concern folder",
        "usage/PriceTable.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/PriceTable.cs: 4 top-level types, one per file",
        "usage/PriceTable.cs: 'usage' isn't a concern folder",
        "usage/PricingUsageSink.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/PricingUsageSink.cs: 'usage' isn't a concern folder",
        "usage/UsageRecord.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageRecord.cs: 4 top-level types, one per file",
        "usage/UsageRecord.cs: 'usage' isn't a concern folder",
        "usage/UsageRecordingChatClient.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageRecordingChatClient.cs: 'usage' isn't a concern folder",
        "usage/UsageReport.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageReport.cs: 2 top-level types, one per file",
        "usage/UsageReport.cs: 'usage' isn't a concern folder",
        "usage/UsageScope.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageScope.cs: 2 top-level types, one per file",
        "usage/UsageScope.cs: 'usage' isn't a concern folder",
        "usage/UsageSession.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageSession.cs: 'usage' isn't a concern folder",
        "usage/UsageText.cs: namespace louis_agent.core.usage, expected LouisAgent.Core.usage",
        "usage/UsageText.cs: 'usage' isn't a concern folder",
    ];

    [Test]
    public void NoViolations_BeyondThePendingList()
    {
        List<string> unexpected = Violations().Where(v => !Pending.Contains(v)).ToList();

        Assert.That(unexpected, Is.Empty, "Layout violations (docs/CLAUDE.md, Project layout):\n" + string.Join("\n", unexpected));
    }

    [Test]
    public void ThePendingList_OnlyHoldsRealViolations()
    {
        HashSet<string> current = Violations().ToHashSet();
        List<string> fixedAlready = Pending.Where(p => !current.Contains(p)).ToList();

        Assert.That(fixedAlready, Is.Empty, "No longer violations; remove them from Pending:\n" + string.Join("\n", fixedAlready));
    }

    private static IEnumerable<string> Violations()
    {
        string core = Path.Combine(TestPaths.RepoRoot, "louis-agent.core");
        IEnumerable<string> files = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(core, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("bin/") && !f.StartsWith("obj/"))
            .Order(StringComparer.Ordinal);

        foreach (string file in files)
        {
            string source = File.ReadAllText(Path.Combine(core, file));
            string folder = Path.GetDirectoryName(file)!.Replace('\\', '/');
            string concern = folder.Split('/')[0];

            string expected = folder.Length == 0 ? RootNamespace : $"{RootNamespace}.{folder.Replace('/', '.')}";
            string? actual = Namespace().Match(source) is { Success: true } m ? m.Groups[1].Value : null;
            if (actual != expected) yield return $"{file}: namespace {actual ?? "(none)"}, expected {expected}";

            int types = TopLevelType().Count(source);
            if (types > 1) yield return $"{file}: {types} top-level types, one per file";

            if (!Layers.TryGetValue(concern, out int layer))
            {
                yield return $"{file}: '{(folder.Length == 0 ? "(root)" : concern)}' isn't a concern folder";
                continue;
            }

            foreach (string used in CoreUsing().Matches(source).Select(u => u.Groups[1].Value).Distinct())
            {
                if (Layers.TryGetValue(used, out int usedLayer) && usedLayer > layer)
                    yield return $"{file}: {concern} (layer {layer}) uses {used} (layer {usedLayer})";
                if (concern == "Tools" && used is "Llm" or "Agent")
                    yield return $"{file}: Tools uses {used}";
            }
        }
    }

    [GeneratedRegex(@"^namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex Namespace();

    // Top-level declarations start at column 0 (file-scoped namespaces); nested types are indented.
    [GeneratedRegex(@"^(?:(?:public|internal|file|static|sealed|abstract|partial|readonly)\s+)*(?:class|record|enum|interface|struct)\s+\w",
        RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [GeneratedRegex(@"^using\s+LouisAgent\.Core\.(\w+)", RegexOptions.Multiline)]
    private static partial Regex CoreUsing();
}
