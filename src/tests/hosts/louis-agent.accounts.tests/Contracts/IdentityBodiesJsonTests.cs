using System.Text.Json;
using LouisAgent.Accounts.Contracts;
using Microsoft.AspNetCore.Identity.Data;

namespace LouisAgent.Accounts.Tests.Contracts;

/// <summary>
/// Each body the browser sends to one of Identity's routes is written exactly as Identity's own request type is, so
/// the mirror can't drift from the thing it mirrors.
/// </summary>
public class IdentityBodiesJsonTests
{
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, AccountJson.Web);

    [Test]
    public void Register_IsIdentitysRegisterRequest() =>
        Assert.That(Json(new RegisterBody("a@x.test", "pw")),
            Is.EqualTo(Json(new RegisterRequest
            {
                Email = "a@x.test",
                Password = "pw"
            })));

    [Test]
    public void Login_IsIdentitysLoginRequest() =>
        Assert.That(Json(new LoginBody("a@x.test", "pw", "123456", null)),
            Is.EqualTo(Json(new LoginRequest
            {
                Email = "a@x.test",
                Password = "pw",
                TwoFactorCode = "123456"
            })));

    [Test]
    public void ResendConfirmation_IsIdentitys() =>
        Assert.That(Json(new ResendConfirmationBody("a@x.test")),
            Is.EqualTo(Json(new ResendConfirmationEmailRequest { Email = "a@x.test" })));

    [Test]
    public void ForgotPassword_IsIdentitys() =>
        Assert.That(Json(new ForgotPasswordBody("a@x.test")),
            Is.EqualTo(Json(new ForgotPasswordRequest { Email = "a@x.test" })));

    [Test]
    public void ResetPassword_IsIdentitys() =>
        Assert.That(Json(new ResetPasswordBody("a@x.test", "code", "new")),
            Is.EqualTo(Json(new ResetPasswordRequest
            {
                Email = "a@x.test",
                ResetCode = "code",
                NewPassword = "new"
            })));

    [Test]
    public void Info_IsIdentitysInfoRequest() =>
        Assert.That(Json(new InfoBody(NewPassword: "new", OldPassword: "old")),
            Is.EqualTo(Json(new InfoRequest
            {
                NewPassword = "new",
                OldPassword = "old"
            })));

    [Test]
    public void TwoFactor_IsIdentitysTwoFactorRequest() =>
        Assert.That(Json(new TwoFactorBody(Enable: true, TwoFactorCode: "123456")),
            Is.EqualTo(Json(new TwoFactorRequest
            {
                Enable = true,
                TwoFactorCode = "123456"
            })));

    [Test]
    public void TwoFactorReply_ReadsIdentitysTwoFactorResponse()
    {
        string identitys = Json(new TwoFactorResponse
        {
            SharedKey = "KEY",
            RecoveryCodesLeft = 10,
            RecoveryCodes = ["a-b"],
            IsTwoFactorEnabled = true,
            IsMachineRemembered = false
        });

        var reply = JsonSerializer.Deserialize<TwoFactorReply>(identitys, AccountJson.Web);

        Assert.That(reply, Is.EqualTo(new TwoFactorReply("KEY", 10, reply!.RecoveryCodes, true, false)));
        Assert.That(reply.RecoveryCodes, Is.EqualTo(new[] { "a-b" }));
    }

    /// <summary>Our own reply, pinned: the browser reads these names.</summary>
    [Test]
    public void AccountInfo_IsWrittenInCamelCase()
    {
        var info = new AccountInfo("1", "home", "a@x.test", null, true, ["Google"], false);

        Assert.That(Json(info), Is.EqualTo(
            """{"userId":"1","tenantId":"home","email":"a@x.test","displayName":null,"emailConfirmed":true,"logins":["Google"],"hasPassword":false}"""));
    }
}
