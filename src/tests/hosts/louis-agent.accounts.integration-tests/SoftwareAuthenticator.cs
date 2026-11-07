using System.Buffers.Text;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// A passkey authenticator a test holds in memory: one ES256 key, one credential, a "none" attestation, as a
/// platform authenticator answers <c>navigator.credentials.create()</c> and <c>get()</c>. It answers the options
/// Identity made with the JSON a browser would post, so the routes run on real WebAuthn messages.
/// </summary>
internal sealed class SoftwareAuthenticator : IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly byte[] _credentialId = RandomNumberGenerator.GetBytes(16);
    /// <summary>The signature counter: always 0, as a platform authenticator that doesn't count reports it.</summary>
    private const uint Counter = 0;

    /// <summary>The credential's id as the browser writes it.</summary>
    public string CredentialId => Base64Url.EncodeToString(_credentialId);

    /// <summary>Answers the creation options with a new credential: what <c>navigator.credentials.create()</c> returns, as JSON.</summary>
    public string Create(string creationOptionsJson, string origin)
    {
        using var options = JsonDocument.Parse(creationOptionsJson);
        string challenge = options.RootElement.GetProperty("challenge").GetString()!;
        string rpId = options.RootElement.GetProperty("rp").GetProperty("id").GetString()!;

        byte[] clientData = ClientData("webauthn.create", challenge, origin);
        byte[] authData = AuthenticatorData(rpId, flags: 0x45, withCredential: true);
        return Credential(new
        {
            clientDataJSON = Base64Url.EncodeToString(clientData),
            attestationObject = Base64Url.EncodeToString(AttestationObject(authData)),
            transports = new[] { "internal" }
        }, "platform");
    }

    /// <summary>Answers the request options with a signature: what <c>navigator.credentials.get()</c> returns, as JSON.</summary>
    public string Get(string requestOptionsJson, string origin, string? userHandle)
    {
        using var options = JsonDocument.Parse(requestOptionsJson);
        string challenge = options.RootElement.GetProperty("challenge").GetString()!;
        string rpId = options.RootElement.GetProperty("rpId").GetString()!;

        byte[] clientData = ClientData("webauthn.get", challenge, origin);
        byte[] authData = AuthenticatorData(rpId, flags: 0x05, withCredential: false);
        byte[] signed = [.. authData, .. SHA256.HashData(clientData)];
        byte[] signature = _key.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return Credential(new
        {
            clientDataJSON = Base64Url.EncodeToString(clientData),
            authenticatorData = Base64Url.EncodeToString(authData),
            signature = Base64Url.EncodeToString(signature),
            userHandle
        }, "platform");
    }

    /// <summary>The client data the browser would sign over: the ceremony, the challenge as given, and where it ran.</summary>
    private static byte[] ClientData(string type, string challenge, string origin) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            type,
            challenge,
            origin,
            crossOrigin = false
        });

    /// <summary>
    /// The authenticator data: the relying party's hash, the flags (user present, user verified, and whether a
    /// credential follows), the counter, and for a new credential the zero AAGUID, the id and the COSE public key.
    /// </summary>
    private byte[] AuthenticatorData(string rpId, byte flags, bool withCredential)
    {
        var data = new List<byte>(SHA256.HashData(Encoding.UTF8.GetBytes(rpId))) { flags };
        data.AddRange(BitConverter.GetBytes(Counter).Reverse());
        if (!withCredential) return [.. data];

        data.AddRange(new byte[16]);
        data.AddRange([(byte)(_credentialId.Length >> 8), (byte)_credentialId.Length]);
        data.AddRange(_credentialId);
        data.AddRange(CoseKey());
        return [.. data];
    }

    /// <summary>The public key as COSE writes an EC2 P-256 key for ES256, in canonical order.</summary>
    private byte[] CoseKey()
    {
        var point = _key.ExportParameters(false).Q;
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical);
        cbor.WriteStartMap(5);
        cbor.WriteInt32(1);
        cbor.WriteInt32(2);
        cbor.WriteInt32(3);
        cbor.WriteInt32(-7);
        cbor.WriteInt32(-1);
        cbor.WriteInt32(1);
        cbor.WriteInt32(-2);
        cbor.WriteByteString(point.X!);
        cbor.WriteInt32(-3);
        cbor.WriteByteString(point.Y!);
        cbor.WriteEndMap();
        return cbor.Encode();
    }

    /// <summary>A "none" attestation: no statement, the authenticator data alone.</summary>
    private static byte[] AttestationObject(byte[] authData)
    {
        var cbor = new CborWriter(CborConformanceMode.Ctap2Canonical);
        cbor.WriteStartMap(3);
        cbor.WriteTextString("fmt");
        cbor.WriteTextString("none");
        cbor.WriteTextString("attStmt");
        cbor.WriteStartMap(0);
        cbor.WriteEndMap();
        cbor.WriteTextString("authData");
        cbor.WriteByteString(authData);
        cbor.WriteEndMap();
        return cbor.Encode();
    }

    /// <summary>The credential as <c>PublicKeyCredential.toJSON()</c> writes it.</summary>
    private string Credential(object response, string attachment) => JsonSerializer.Serialize(new
    {
        id = CredentialId,
        rawId = CredentialId,
        type = "public-key",
        authenticatorAttachment = attachment,
        clientExtensionResults = new { },
        response
    });

    public void Dispose() => _key.Dispose();
}
