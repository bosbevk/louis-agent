using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// Finds, in the compiled code of our own assemblies, every call of <c>ToString()</c> on a value of an enum we declare (F18 step 7.5.2: "out with .Wire()"). The compiler writes such a call as
/// <c>constrained. EnumType; callvirt ToString</c>, so the type is known exactly and a name like <c>Kind</c> or <c>Level</c> needs no guessing. Reads the metadata with the BCL's own
/// <c>System.Reflection.Metadata</c>; no package.
/// </summary>
public static class EnumToStringScan
{
    public sealed record Site(string Assembly, string Method, string Enum);

    private static readonly Dictionary<short, OpCode> OpCodeTable = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => o.Value);

    public static IReadOnlyList<Site> Find(IEnumerable<string> assemblies)
    {
        var readers = assemblies.Select(path => (Path: path, Pe: new PEReader(File.OpenRead(path)))).ToList();
        try
        {
            var metadata = readers.Select(r => (r.Path, Reader: r.Pe.GetMetadataReader(), r.Pe)).ToList();
            var ours = new HashSet<string>(metadata.SelectMany(m => EnumsOf(m.Reader)), StringComparer.Ordinal);
            var found = new List<Site>();
            foreach (var (path, reader, pe) in metadata)
                foreach (var handle in reader.MethodDefinitions)
                {
                    var method = reader.GetMethodDefinition(handle);
                    if (method.RelativeVirtualAddress == 0) continue;
                    byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
                    foreach (string enumName in ToStringCalls(reader, il).Where(ours.Contains))
                        found.Add(new Site(Path.GetFileNameWithoutExtension(path), MethodName(reader, method), enumName));
                }

            return found;
        }
        finally
        {
            foreach (var r in readers) r.Pe.Dispose();
        }
    }

    private static IEnumerable<string> EnumsOf(MetadataReader reader)
    {
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            if (type.BaseType.Kind == HandleKind.TypeReference &&
                reader.GetString(reader.GetTypeReference((TypeReferenceHandle)type.BaseType).Name) == "Enum")
                yield return FullName(reader, handle);
        }
    }

    private static string FullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        string name = reader.GetString(type.Name);
        var outer = type.GetDeclaringType();
        return outer.IsNil ? $"{reader.GetString(type.Namespace)}.{name}" : $"{FullName(reader, outer)}+{name}";
    }

    private static string FullName(MetadataReader reader, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => FullName(reader, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => TypeRefName(reader, (TypeReferenceHandle)handle),
        _ => "",
    };

    private static string TypeRefName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var type = reader.GetTypeReference(handle);
        string name = reader.GetString(type.Name);
        return type.ResolutionScope.Kind == HandleKind.TypeReference
            ? $"{TypeRefName(reader, (TypeReferenceHandle)type.ResolutionScope)}+{name}"
            : $"{reader.GetString(type.Namespace)}.{name}";
    }

    private static string MethodName(MetadataReader reader, MethodDefinition method) =>
        $"{FullName(reader, method.GetDeclaringType())}::{reader.GetString(method.Name)}";

    /// <summary>The enum types a method calls <c>ToString()</c> on, from its IL: <c>constrained. T</c> followed by a call of <c>ToString</c>.</summary>
    private static IEnumerable<string> ToStringCalls(MetadataReader reader, byte[] il)
    {
        int i = 0;
        string? constrained = null;
        while (i < il.Length)
        {
            short code = il[i++];
            if (code == 0xFE) code = (short)(0xFE00 | il[i++]);
            if (!OpCodeTable.TryGetValue(code, out var op)) yield break;
            int size = op.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                _ => 4,
            };
            if (op == OpCodes.Constrained && TypeFromToken(reader, BitConverter.ToInt32(il, i)) is { } type)
                constrained = type;
            else if ((op == OpCodes.Callvirt || op == OpCodes.Call) && constrained is not null)
            {
                if (MethodNamed(reader, BitConverter.ToInt32(il, i)) == "ToString") yield return constrained;
                constrained = null;
            }
            else if (op != OpCodes.Constrained) constrained = null;

            i += size;
        }
    }

    private static string? TypeFromToken(MetadataReader reader, int token)
    {
        var handle = MetadataTokens.EntityHandle(token);
        return handle.Kind is HandleKind.TypeDefinition or HandleKind.TypeReference ? FullName(reader, handle) : null;
    }

    private static string? MethodNamed(MetadataReader reader, int token)
    {
        var handle = MetadataTokens.EntityHandle(token);
        return handle.Kind switch
        {
            HandleKind.MemberReference => reader.GetString(
                reader.GetMemberReference((MemberReferenceHandle)handle).Name),
            HandleKind.MethodDefinition => reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)handle)
                .Name),
            _ => null,
        };
    }
}
