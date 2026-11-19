using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Requests;
using LouisAgent.Api.Messages;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LouisAgent.Api.Tests.Sessions;

// CL14 step 1: RoleNamingTests' rule for the API: what a session is, is its role. From CL20 step 5 nothing in the API is named
// personality: the alias a client one deploy behind sent went with the unversioned routes it called.
public class ApiRoleNamingTests
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void NothingInTheApi_IsNamedPersonality()
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

        var left = names.Distinct().Order().ToList();
        Assert.That(left, Is.Empty, "call it a role: " + string.Join(", ", left));
    }

    private static bool Says(string? name) => name?.Contains("personalit", StringComparison.OrdinalIgnoreCase) == true;
}
