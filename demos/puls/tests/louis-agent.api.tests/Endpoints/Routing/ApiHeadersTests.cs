using LouisAgent.Api.Contracts;
using LouisAgent.Core.Guardrails.Auth;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The contract's header names are the engine's: the engine references no contract, so the two copies are held
/// equal here (a tested duplicate, the engine split's rule).
/// </summary>
public class ApiHeadersTests
{
    [Test]
    public void TheRequestedWithHeader_IsTheEngines()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApiHeaders.RequestedWith, Is.EqualTo(HostAuth.RequestedWithHeader));
            Assert.That(ApiHeaders.RequestedWithWeb, Is.EqualTo(HostAuth.RequestedWithWeb));
        });
    }
}
