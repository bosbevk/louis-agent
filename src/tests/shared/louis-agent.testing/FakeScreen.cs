using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Testing;

/// <summary>A screen for the catalogue's and the API's tests: a name, its rows, and what it was last asked.</summary>
public sealed class FakeScreen(string name, params ScreenRow[] rows) : IDataScreen
{
    /// <summary>What the page last asked for.</summary>
    public ScreenQuery? LastQuery { get; private set; }

    /// <summary>The last action it was asked to do, and on which row.</summary>
    public (string Action, string? RowId)? LastAction { get; private set; }

    /// <inheritdoc />
    public ScreenSpec Spec { get; private set; } = new(name, $"The {name}", $"The {name} screen",
        [new ScreenColumn("Name", ScreenCell.Text), new ScreenColumn("Count", ScreenCell.Number)], Searchable: true);

    /// <summary>This screen, offering these actions.</summary>
    public FakeScreen With(params ScreenAction[] actions)
    {
        Spec = Spec with { Actions = actions };
        return this;
    }

    /// <inheritdoc />
    public Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken)
    {
        LastQuery = query;
        return Task.FromResult(new ScreenPage(rows, rows.Length));
    }

    /// <inheritdoc />
    public Task<ScreenAnswer> ActAsync(string action, string? rowId, CancellationToken cancellationToken)
    {
        LastAction = (action, rowId);
        return Task.FromResult(ScreenAnswer.Ok($"{action} done"));
    }
}
