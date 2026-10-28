using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

/// <summary>Runs the store's shared tests against the in-memory store, on every machine and on CI.</summary>
public class InMemoryBenchmarkStoreTests : BenchmarkStoreContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}

/// <summary>Runs the shared tests for a run's own fields against the in-memory store.</summary>
public class InMemoryRunFieldsTests : BenchmarkRunFieldsContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}

/// <summary>Runs the shared tests for a run's notes against the in-memory store.</summary>
public class InMemoryNotesTests : BenchmarkNotesContract
{
    /// <summary>A fresh, empty in-memory store for each test.</summary>
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}
