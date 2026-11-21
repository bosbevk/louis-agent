namespace LouisAgent.Api.Config;

/// <summary>
/// The guide's door (F109.3, Louis's pick C1; a list since F127.2, his pick "a list, one cap"): the identities a
/// visitor with no key and no sign-in may open a session as, and how many times a day from one address across them
/// all. None named, or a cap of 0, keeps the door shut, which is the default.
/// </summary>
/// <param name="Identities">
/// OPEN_IDENTITIES: the identities' names, as the catalogue knows them; none shuts the door.
/// </param>
/// <param name="SessionsPerDay">
/// OPEN_SESSIONS_PER_DAY: the sessions one address may open a day, across them all; 0 shuts the door.
/// </param>
/// <remarks>
/// A plain options record with a reader that takes the variable lookup, as <see cref="AdminOptions"/> is, so a test
/// needs no real environment; a value that isn't a whole number counts as 0, since a door should fail shut. An
/// allow-list: an identity it doesn't name is behind the key, as every identity was before the door.
/// </remarks>
public sealed record OpenDoorOptions(IReadOnlyList<string> Identities, int SessionsPerDay)
{
    /// <summary>The setting that names the open identities, by commas.</summary>
    public const string IdentitiesSetting = "OPEN_IDENTITIES";

    /// <summary>The setting that caps the sessions one address may open a day.</summary>
    public const string CapSetting = "OPEN_SESSIONS_PER_DAY";

    /// <summary>No door at all: what every host has unless its settings open one.</summary>
    public static OpenDoorOptions Shut { get; } = new([], 0);

    /// <summary>Whether a visitor may open any identity's sessions at all.</summary>
    public bool IsOpen => Identities.Count > 0 && SessionsPerDay > 0;

    /// <summary>
    /// The identity the door admits for the one asked for, named as the host names it; none if it isn't one.
    /// </summary>
    public string? Admits(string? identity) => identity is null
        ? null
        : Identities.FirstOrDefault(name => string.Equals(name, identity, StringComparison.OrdinalIgnoreCase));

    /// <summary>Reads the two settings; the variable lookup is replaceable for tests.</summary>
    public static OpenDoorOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string[] identities = (getVariable(IdentitiesSetting) ?? "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        int cap = int.TryParse(getVariable(CapSetting), out int parsed) && parsed > 0 ? parsed : 0;
        return new OpenDoorOptions(identities, cap);
    }
}
