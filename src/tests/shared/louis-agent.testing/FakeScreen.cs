using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Testing;

/// <summary>A screen for the catalogue's and the API's tests: a name, its rows, and the last query it got.</summary>
public sealed class FakeScreen(string name, params ScreenRow[] rows) : IDataScreen
{
    /// <summary>What the page last asked for.</summary>
    public ScreenQuery? LastQuery { get; private set; }

    /// <inheritdoc />
    public ScreenSpec Spec { get; } = new(name, $"The {name}", $"The {name} screen",
        [new ScreenColumn("Name", ScreenCell.Text), new ScreenColumn("Count", ScreenCell.Number)], Searchable: true);

    /// <inheritdoc />
    public Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken)
    {
        LastQuery = query;
        return Task.FromResult(new ScreenPage(rows, rows.Length));
    }
}
