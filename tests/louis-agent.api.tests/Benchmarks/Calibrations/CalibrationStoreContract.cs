using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;

namespace LouisAgent.Api.Tests.Benchmarks.Calibrations;

/// <summary>The contract against the in-memory store: always, on every machine and on CI.</summary>
public class InMemoryCalibrationStoreTests : CalibrationStoreContract
{
    protected override ICalibrationStore CreateStore() => new InMemoryCalibrationStore();
}
