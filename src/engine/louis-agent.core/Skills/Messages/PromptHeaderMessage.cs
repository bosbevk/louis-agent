using System.ComponentModel;

namespace LouisAgent.Core.Skills.Messages;

/// <summary>
/// Why a prompt's header (CL27) refused its file: each names the file and what to change, since a refused prompt
/// stops the host that reads it.
/// </summary>
public enum PromptHeaderMessage
{
    [Description("{Path}: the header opened with --- never closes: end it with a line of ---.")]
    NotClosed,

    [Description("{Path}: the header isn't YAML the reader takes: {Reason}")]
    NotYaml,

    [Description(
        "{Path}: the header has '{Key}', which nothing reads. It takes name, description and metadata (level, " +
        "toolsets, load).")]
    KeyUnknown,

    [Description("{Path}: the header's '{Key}' must be text, such as toolsets: web, shops?")]
    ValueNotText,

    [Description("{Path}: the header's name '{Name}' must be the file's own, '{Expected}'.")]
    NameNotTheFiles,

    [Description(
        "{Path}: the header needs a description of at most {Limit} characters: what the prompt does and when it is " +
        "loaded.")]
    DescriptionMissing,

    [Description("{Path}: '{Level}' is not a level. Supported: {Supported}.")]
    LevelUnknown,

    [Description("{Path}: '{Load}' is not how a skill loads. Supported: {Supported}.")]
    LoadUnknown,
}
