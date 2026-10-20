using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Messages;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LouisAgent.Api.Tests.Sessions;

// CL14 step 1: RoleNamingTests' rule for the API: what a session is, is its role; "personality" is left only in the
// aliases a client one deploy behind still sends, and in what CL14's later steps rename.
public class ApiRoleNamingTests
{
    private static readonly HashSet<string> Allowed =
    [
        "CreateSessionRequest.Personality",                 // the alias POST /sessions still reads (removed by the TODO's line)
        "CreateSessionRequest.get_Personality", "CreateSessionRequest.set_Personality",
        "CreateSessionRequest..ctor(Personality)", "CreateSessionRequest.Deconstruct(Personality)",
        "ApiUserText.RoleAndPersonalityDiffer",             // the 400 for a request that sends both, differing
    ];

    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void NothingInTheApi_IsNamedPersonality_ButTheAliases()
    {
        var names = new List<string>();
        foreach (var type in typeof(AgentSessions).Assembly.GetTypes().Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute)) && !t.Name.Contains('<')))
        {
            if (Says(type.Name)) names.Add(type.FullName!);
            foreach (var member in type.GetMembers(All).Where(m => !m.IsDefined(typeof(CompilerGeneratedAttribute)) && !m.Name.Contains('<')))
            {
                if (Says(member.Name)) names.Add($"{type.Name}.{member.Name}");
                if (member is MethodBase method)
                    names.AddRange(method.GetParameters().Where(p => Says(p.Name)).Select(p => $"{type.Name}.{method.Name}({p.Name})"));
            }
        }

        var left = names.Distinct().Except(Allowed).Order().ToList();
        Assert.That(left, Is.Empty, "call it a role: " + string.Join(", ", left));
    }

    private static bool Says(string? name) => name?.Contains("personalit", StringComparison.OrdinalIgnoreCase) == true;
}
