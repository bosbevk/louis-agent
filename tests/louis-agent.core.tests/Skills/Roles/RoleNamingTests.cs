using System.Reflection;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills.Roles;

// CL13 (Louis: "we must cleanup to fix the whole Skills folder namespace"): what a session is, a mode, a specialist's
// role or a minion's function, is a role in the code; "personality" is left only where callers can see it, until CL14
// changes the public word with the old names still accepted.
public class RoleNamingTests
{
    // The public contract's names, which CL14 renames: a tool's parameter is what the model reads.
    private static readonly HashSet<string> ContractNames =
    [
        "SpecialistTools.StartSubSession(personality)",   // CL14: the model's StartSubSession argument
    ];

    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                     BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void NothingInCore_IsNamedPersonality_ButThePublicContract()
    {
        var names = new List<string>();
        foreach (var type in typeof(ISkillProvider).Assembly.GetTypes().Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))))
        {
            if (Says(type.Name)) names.Add(type.FullName!);
            foreach (var member in type.GetMembers(All).Where(m => !m.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))))
            {
                if (Says(member.Name) && !member.Name.Contains('<')) names.Add($"{type.Name}.{member.Name}");
                if (member is MethodBase method)
                    names.AddRange(method.GetParameters().Where(p => Says(p.Name)).Select(p => $"{type.Name}.{method.Name}({p.Name})"));
            }
        }

        var left = names.Distinct().Except(ContractNames).Order().ToList();
        Assert.That(left, Is.Empty, "call it a role: " + string.Join(", ", left));
    }

    private static bool Says(string? name) => name?.Contains("personalit", StringComparison.OrdinalIgnoreCase) == true;
}
