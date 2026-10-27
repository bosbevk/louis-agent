using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

/// <summary>The contract against the in-memory store: always, on every machine and on CI.</summary>
public class InMemoryBenchmarkStoreTests : BenchmarkStoreContract
{
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}

/// <summary>The run-fields contract (F18 steps 5b.2, 5c.2) against the in-memory store.</summary>
public class InMemoryRunFieldsTests : BenchmarkRunFieldsContract
{
    protected override IBenchmarkStore CreateStore() => new InMemoryBenchmarkStore();
}
