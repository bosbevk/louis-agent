using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Testing;

/// <summary>A screen for the catalogue's and the API's tests: a name, its rows, and what it was last asked.</summary>
public sealed class FakeScreen(string name, params ScreenRow[] rows) : IDataScreen
{
    private ScreenRecord? _record;
    private ScreenAnswer? _saved;

    /// <summary>What the page last asked for.</summary>
    public ScreenQuery? LastQuery { get; private set; }

    /// <summary>The last action it was asked to do, and on which row.</summary>
    public (string Action, string? RowId)? LastAction { get; private set; }

    /// <inheritdoc />
    public ScreenSpec Spec { get; private set; } = new(name, $"The {name}", $"The {name} screen",
        [new ScreenColumn("Name", ScreenCell.Text), new ScreenColumn("Count", ScreenCell.Number)], Searchable: true);

    /// <summary>What it was last asked to save, and for which row.</summary>
    public (string RowId, ScreenValues Values)? LastSave { get; private set; }

    /// <summary>This screen with its rows editable: row <c>a</c> reads as this form, and a save answers this.</summary>
    public FakeScreen Editable(ScreenRecord record, ScreenAnswer answer)
    {
        Spec = Spec with { Editable = true };
        _record = record;
        _saved = answer;
        return this;
    }

    /// <inheritdoc />
    public Task<ScreenRecord?> ReadRowAsync(string rowId, CancellationToken cancellationToken) =>
        Task.FromResult(rowId == "a" ? _record : null);

    /// <inheritdoc />
    public Task<ScreenAnswer> SaveRowAsync(string rowId, ScreenValues values, CancellationToken cancellationToken)
    {
        LastSave = (rowId, values);
        return Task.FromResult(_saved ?? ScreenAnswer.NotOffered);
    }

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
