using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Messages;
using LouisAgent.McpServer.Messages;
using ModelContextProtocol.Server;

namespace LouisAgent.McpServer.Tools;

/// <summary>
/// Read-only views of the loaded skills. Running a skill is done by the engine's ExecuteSkill tool.
/// </summary>
[McpServerToolType]
public class SkillsToolProvider
{
    private readonly AgentEngine _engine;

    /// <summary>
    /// Reads the skills from the engine the server already built.
    /// </summary>
    public SkillsToolProvider(AgentEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Lists every skill by name, with its language and what it does.
    /// </summary>
    [McpServerTool]
    [Description("List all available skills (run one with ExecuteSkill)")]
    public string ListSkills()
    {
        // One line per skill in name order, under the header (which keeps its newline even when there are none).
        var lines = _engine.Skills.OrderBy(s => s.Name).Select(s => MessageText.Format(McpServerText.SkillListLine,
            new
            {
                s.Name,
                Language = s.Language.Wire(),
                s.Description
            }));
        return $"{MessageText.Format(McpServerText.SkillListHeader)}\n{string.Join("\n", lines)}";
    }

    /// <summary>
    /// Shows one skill in full, its command included, so the client can see what it would run.
    /// </summary>
    [McpServerTool]
    [Description("Get detailed information about a specific skill, including its command")]
    public string GetSkillInfo(
        [Description("Name of the skill to get information about")]
        string skillName)
    {
        var skill = _engine.Skills.FirstOrDefault(s =>
            s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return MessageText.Format(McpServerText.UnknownSkill, new { Name = skillName });
        }

        return MessageText.Format(McpServerText.SkillInfo,
            new
            {
                skill.Name,
                skill.Description,
                Language = skill.Language.Wire(),
                skill.Command
            });
    }

    /// <summary>
    /// Hands over all the guidance the agent itself reads, so the client's model can use the tools the same way.
    /// </summary>
    [McpServerTool]
    [Description("Get the agent's full guidance: default instructions plus the .NET, Python, PowerShell, Bash, "
                 + "self-extension and skill documentation")]
    public string GetSkillDocumentation()
    {
        return _engine.SkillDocumentation;
    }
}
