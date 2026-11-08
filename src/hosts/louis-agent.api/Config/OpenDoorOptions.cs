namespace LouisAgent.Api.Config;

/// <summary>
/// The guide's door (F109.3, Louis's pick C1): the one identity a visitor with no key and no sign-in may open a session
/// as, and how many times a day from one address. Unset, or a cap of 0, keeps the door shut, which is the default.
/// </summary>
/// <param name="Identity">OPEN_IDENTITY: the identity's name, as the catalogue knows it; none shuts the door.</param>
/// <param name="SessionsPerDay">OPEN_SESSIONS_PER_DAY: the sessions one address may open a day; 0 shuts the door.</param>
/// <remarks>
/// A plain options record with a reader that takes the variable lookup, as <see cref="AdminOptions"/> is, so a test
/// needs no real environment; a value that isn't a whole number counts as 0, since a door should fail shut.
/// </remarks>
public sealed record OpenDoorOptions(string? Identity, int SessionsPerDay)
{
    /// <summary>No door at all: what every host has unless its settings open one.</summary>
    public static OpenDoorOptions Shut { get; } = new(null, 0);

    /// <summary>Whether a visitor may open the named identity's sessions at all.</summary>
    public bool IsOpen => Identity is not null && SessionsPerDay > 0;

    /// <summary>Reads the two settings; the variable lookup is replaceable for tests.</summary>
    public static OpenDoorOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? identity = getVariable("OPEN_IDENTITY");
        int cap = int.TryParse(getVariable("OPEN_SESSIONS_PER_DAY"), out int parsed) && parsed > 0 ? parsed : 0;
        return new OpenDoorOptions(string.IsNullOrWhiteSpace(identity) ? null : identity.Trim(), cap);
    }
}
