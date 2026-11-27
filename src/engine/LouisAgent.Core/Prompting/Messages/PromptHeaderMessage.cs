using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Prompting.Messages.Holes.Fields;
using LouisAgent.Core.Prompting.Messages.Holes.Reading;

namespace LouisAgent.Core.Prompting.Messages;

/// <summary>
/// Why a prompt's header (CL27) refused its file: each names the file and what to change, since a refused prompt
/// stops the host that reads it.
/// </summary>
public enum PromptHeaderMessage
{
    [Description("{Path}: the header opened with --- never closes: end it with a line of ---.")]
    [Holes(typeof(NotClosedHoles))]
    NotClosed,

    [Description("{Path}: the header isn't YAML the reader takes: {Reason}")]
    [Holes(typeof(NotYamlHoles))]
    NotYaml,

    [Description(
        "{Path}: the header has '{Key}', which nothing reads. It takes name, description and metadata (level, " +
        "toolsets, load, script, loaded).")]
    [Holes(typeof(KeyUnknownHoles))]
    KeyUnknown,

    [Description("{Path}: the header's '{Key}' must be text, such as toolsets: web, shops?")]
    [Holes(typeof(ValueNotTextHoles))]
    ValueNotText,

    [Description(
        "{Path}: the header's name '{Name}' must be '{Expected}': the file's own, or a skill's folder's for its " +
        "SKILL.md.")]
    [Holes(typeof(NameNotTheFilesHoles))]
    NameNotTheFiles,

    [Description(
        "{Path}: the header needs a description of at most {Limit} characters: what the prompt does and when it is " +
        "loaded.")]
    [Holes(typeof(DescriptionMissingHoles))]
    DescriptionMissing,

    [Description("{Path}: '{Level}' is not a level. Supported: {Supported}.")]
    [Holes(typeof(LevelUnknownHoles))]
    LevelUnknown,

    [Description("{Path}: '{Load}' is not how a skill loads. Supported: {Supported}.")]
    [Holes(typeof(LoadUnknownHoles))]
    LoadUnknown,

    [Description("{Path}: an identity's skill says how it loads under metadata: load: always, or load: on-demand.")]
    [Holes(typeof(LoadMissingHoles))]
    LoadMissing,
}
