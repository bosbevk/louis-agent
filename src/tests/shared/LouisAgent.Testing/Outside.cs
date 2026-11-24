using NUnit.Framework;

namespace LouisAgent.Testing;

/// <summary>
/// How an integration test asks for what is outside the test process (F18 step 7.6). A tool the CI image has (git,
/// bash, an interpreter, dotnet, Mongo) is <see cref="Tool"/>: a missing one skips the test on a developer's machine,
/// and fails it where <c>INTEGRATION_REQUIRED</c> is set (the CI step), so CI cannot go green by quietly running
/// nothing. What only a running stack or the internet gives (the browser, SearXNG) is <see cref="Stack"/>, and a
/// model's key is <see cref="Key"/>: both always skip when the variable isn't set, since CI has neither.
/// </summary>
public static class Outside
{
    public enum Outcome
    {
        Run,
        Skip,
        Fail
    }

    public const string RequiredVariable = "INTEGRATION_REQUIRED";

    /// <summary>A tool that is there runs; a missing one fails where it is required, and skips elsewhere.</summary>
    /// <remarks>A switch on the two answers, read like a table (ADR 0051).</remarks>
    public static Outcome Decide(bool available, bool required) => (available, required) switch
    {
        (true, _) => Outcome.Run,
        (false, true) => Outcome.Fail,
        (false, false) => Outcome.Skip
    };

    /// <summary>The switch that says CI is meant to have every tool: on for <c>1</c> or <c>true</c>.</summary>
    public static bool IsRequired(string? value) =>
        value is not null && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Runs on, skips or fails the test by <see cref="Decide"/>, with <c>required</c> read from the environment.
    /// </summary>
    public static void Tool(bool available, string reason) => Tool(available, reason,
        IsRequired(Environment.GetEnvironmentVariable(RequiredVariable)));

    /// <summary>Runs on, skips or fails the test by <see cref="Decide"/>.</summary>
    public static void Tool(bool available, string reason, bool required)
    {
        switch (Decide(available, required))
        {
            case Outcome.Fail:
                Assert.Fail($"{reason}, and {RequiredVariable} is set: this machine is meant to have it."); break;
            case Outcome.Skip: Assert.Ignore(reason); break;
        }
    }

    /// <summary>The address in <paramref name="variable"/>; the test is skipped when it isn't set.</summary>
    public static string Stack(string variable, string what)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(value)) Assert.Ignore($"{variable} isn't set: no {what} (a running stack's).");
        return value!;
    }

    /// <summary>
    /// The model's key in <paramref name="variable"/>, for a test that calls a real model; the test is skipped when it
    /// isn't set, in CI too, since CI holds no key and a live call spends money.
    /// </summary>
    public static string Key(string variable, string what)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(value)) Assert.Ignore($"{variable} isn't set: no key for {what}.");
        return value!;
    }

    /// <summary>
    /// The path in <paramref name="variable"/> to a file kept on this machine and never in the repository, which is
    /// public, such as a real bill (F171.1); the test is skipped when it isn't set, in CI too, since CI has no such
    /// file.
    /// </summary>
    public static string LocalFile(string variable, string what)
    {
        string? value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(value)) Assert.Ignore($"{variable} isn't set: no {what} on this machine.");
        return value!;
    }
}
