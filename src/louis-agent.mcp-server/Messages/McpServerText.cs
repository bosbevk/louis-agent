using System.ComponentModel;

namespace LouisAgent.McpServer.Messages;

/// <summary>
/// What the MCP server answers its client: the skills it lists and shows, an unknown skill, a missing API key.
/// </summary>
public enum McpServerText
{
    [Description("Missing or invalid API key (x-api-key or Authorization: Bearer).")]
    InvalidApiKey,

    [Description("Unknown skill: '{Name}'")]
    UnknownSkill,

    [Description("Available skills:")]
    SkillListHeader,

    [Description("- {Name} ({Language}): {Description}")]
    SkillListLine,

    [Description("Skill: {Name}\nDescription: {Description}\nLanguage: {Language}\nCommand: {Command}")]
    SkillInfo,
}
