using System.ComponentModel;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Packs.Files;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Hosting.Catalogue;

/// <summary>
/// A made-up pack's tool that reads a file the chat was sent (F171.1), in the shape a real one takes: the model it
/// was given and the session's files, then one call with its instructions and the file, asking for a record. A
/// failure is text with its reason, never an exception, and a cancellation stays one.
/// </summary>
public sealed class ReadSampleTools(IChatClient model, ISessionFiles files)
{
    /// <summary>What the tool asks the model to do with the file.</summary>
    public const string Instructions = "Read this account: its number, digits only, and its total due.";

    /// <summary>Reads a sample account from a file sent in this chat.</summary>
    [Description("Reads a sample account from a file sent in this chat.")]
    public async Task<string> ReadSample([Description("The file's name")] string name,
        CancellationToken cancellationToken = default)
    {
        if (files.Find(name) is not { } file) return $"No file named {name} in this chat.";

        var document = new DataContent(file.Bytes, file.MediaType.Wire());
        var message = new ChatMessage(ChatRole.User, [new TextContent(Instructions), document]);
        try
        {
            var reading = await model.GetResponseAsync<SampleAccount>([message],
                cancellationToken: cancellationToken);
            return JsonSerializer.Serialize(reading.Result);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return $"The reading failed: {failure.Message}";
        }
    }
}
