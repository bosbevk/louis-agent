using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition.Packs.Files;
using Microsoft.Extensions.AI;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Api.IntegrationTests.Sessions;

/// <summary>
/// A made-up pack's tool that reads an account the chat was sent (F171.1), in the shape a real one takes: the model
/// it was given and the session's files, one call with its instructions and the file, a record back as text, and a
/// failure as text with its reason.
/// </summary>
public sealed class AccountReaderTools(IChatClient model, ISessionFiles files)
{
    /// <summary>What the tool asks the model to do with the file.</summary>
    private const string Instructions =
        "Read this municipal account. Give its account number, digits only, and its total due in rand.";

    /// <summary>Reads an account from a photo or a PDF sent in this chat.</summary>
    [Description("Reads an account from a photo or a PDF sent in this chat, by the file's name.")]
    public async Task<string> ReadAccount([Description("The file's name, as the chat names it")] string name,
        CancellationToken cancellationToken = default)
    {
        if (files.Find(name) is not { } file) return $"No file named {name} in this chat.";

        var document = new DataContent(file.Bytes, file.MediaType.Wire());
        var message = new ChatMessage(ChatRole.User, [new TextContent(Instructions), document]);
        try
        {
            var reading = await model.GetResponseAsync<AccountReading>([message],
                cancellationToken: cancellationToken);
            return JsonSerializer.Serialize(reading.Result);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return $"The reading failed: {failure.Message}";
        }
    }
}
