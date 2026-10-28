using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;

namespace LouisAgent.Api.Tests.Benchmarks.Calibrations;

/// <summary>Runs the judge-check store's shared tests against the in-memory store, on every machine and CI.</summary>
public class InMemoryCalibrationStoreTests : CalibrationStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override ICalibrationStore CreateStore() => new InMemoryCalibrationStore();
}
