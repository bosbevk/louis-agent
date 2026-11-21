using NUnit.Framework;

namespace LouisAgent.Testing;

/// <summary>
/// How an integration test asks for what is outside the test process (F18 step 7.6). A tool the CI image has (git, bash, an interpreter, dotnet, Mongo) is <see cref="Tool"/>: a missing one skips
/// the test on a developer's machine, and fails it where <c>INTEGRATION_REQUIRED</c> is set (the CI step), so CI cannot go green by quietly running nothing. What only the Puls stack or the internet
/// gives (the browser, SearXNG, the shops) is <see cref="Stack"/>: it always skips when the address isn't set, since CI has no such stack.
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

    public static Outcome Decide(bool available, bool required) =>
        available ? Outcome.Run : required ? Outcome.Fail : Outcome.Skip;

    public static bool IsRequired(string? value) => value is not null &&
                                                    (value == "1" || value.Equals("true",
                                                        StringComparison.OrdinalIgnoreCase));

    /// <summary>Runs on, skips or fails the test by <see cref="Decide"/>, with <paramref name="required"/> read from the environment.</summary>
    public static void Tool(bool available, string reason) => Tool(available, reason,
        IsRequired(Environment.GetEnvironmentVariable(RequiredVariable)));

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
        if (string.IsNullOrEmpty(value)) Assert.Ignore($"{variable} isn't set: no {what} (the Puls stack's).");
        return value!;
    }
}
