using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;
using TicTacToe.Core;
using TicTacToe.Data;

namespace TicTacToe.Tests;

/// <summary>
/// Drives the real GameService against a SQLite in-memory database, so these tests exercise the
/// persistence path (including the scoreboard row) without needing the SQL Server instance.
/// </summary>
public class GameServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<GameDbContext> _options;

    public GameServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_connection).Options;

        using var db = new GameDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>A fresh service over a fresh context, the way a request-scoped service behaves.</summary>
    private GameService NewService() => new(new GameDbContext(_options));

    private async Task<GameStateDto> PlayAsync(Guid id, params (string Player, int Cell)[] moves)
    {
        GameStateDto? state = null;
        foreach (var (player, cell) in moves)
        {
            state = await NewService().MoveAsync(id, player, cell);
        }

        return state!;
    }

    [Fact]
    public async Task A_new_game_starts_empty_with_X_to_play()
    {
        var state = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal("X", state.CurrentPlayer);
        Assert.All(state.Board, cell => Assert.Equal(string.Empty, cell));
        Assert.Empty(state.Moves);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public async Task A_valid_move_is_recorded_and_the_turn_switches()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        var state = await NewService().MoveAsync(game.Id, "X", 4);

        Assert.Equal("X", state.Board[4]);
        Assert.Equal("O", state.CurrentPlayer);
        Assert.Single(state.Moves);
        Assert.Equal(new MoveDto(1, "X", 2, 2, 4, false), state.Moves[0]);
        Assert.True(state.CanUndo);
    }

    [Fact]
    public async Task An_invalid_move_is_rejected_and_the_turn_is_unchanged()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await NewService().MoveAsync(game.Id, "X", 0);

        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "O", 0));  // occupied
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "X", 5));  // wrong player
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "O", 42)); // off the board

        var state = await NewService().GetGameAsync(game.Id);
        Assert.Equal("O", state.CurrentPlayer);
        Assert.Single(state.Moves);
    }

    [Fact]
    public async Task A_row_win_completes_the_game_highlights_the_line_and_scores_once()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        var state = await PlayAsync(game.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal("X", state.Winner);
        Assert.Equal([0, 1, 2], state.WinningCells);
        Assert.Equal(1, state.Scoreboard.XWins);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public async Task A_column_win_is_detected()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        var state = await PlayAsync(game.Id, ("X", 0), ("O", 1), ("X", 3), ("O", 2), ("X", 6));

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal([0, 3, 6], state.WinningCells);
    }

    [Fact]
    public async Task A_diagonal_win_is_detected()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        var state = await PlayAsync(game.Id, ("X", 0), ("O", 1), ("X", 4), ("O", 2), ("X", 8));

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal([0, 4, 8], state.WinningCells);
    }

    [Fact]
    public async Task A_full_board_with_no_line_is_a_draw_and_counts_on_the_scoreboard()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);

        // X O X / X O O / O X X
        var state = await PlayAsync(game.Id,
            ("X", 0), ("O", 1), ("X", 2),
            ("O", 4), ("X", 3), ("O", 6),
            ("X", 7), ("O", 5), ("X", 8));

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Null(state.Winner);
        Assert.Equal(1, state.Scoreboard.Draws);
    }

    [Fact]
    public async Task A_move_after_completion_is_rejected()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(game.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));

        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "O", 5));
    }

    [Fact]
    public async Task The_scoreboard_counts_a_completed_game_only_once()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(game.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));

        // Any further traffic against the finished game must not move the scoreboard.
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "O", 5));
        var state = await NewService().GetGameAsync(game.Id);

        Assert.Equal(1, state.Scoreboard.XWins);
        Assert.Equal(0, state.Scoreboard.OWins);
    }

    [Fact]
    public async Task Reset_clears_the_game_but_keeps_the_scoreboard()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(game.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));

        var state = await NewService().ResetGameAsync(game.Id);

        Assert.Equal(game.Id, state.Id);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal("X", state.CurrentPlayer);
        Assert.Empty(state.Moves);
        Assert.All(state.Board, cell => Assert.Equal(string.Empty, cell));
        Assert.Equal(1, state.Scoreboard.XWins);
    }

    [Fact]
    public async Task Undo_in_two_player_mode_removes_only_the_last_move()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(game.Id, ("X", 0), ("O", 4));

        var state = await NewService().UndoAsync(game.Id);

        Assert.Single(state.Moves);
        Assert.Equal("X", state.Board[0]);
        Assert.Equal(string.Empty, state.Board[4]);
        Assert.Equal("O", state.CurrentPlayer);
    }

    [Fact]
    public async Task Undo_in_computer_mode_removes_the_computer_move_and_the_human_move()
    {
        var game = await NewService().CreateGameAsync(GameMode.Computer);
        var afterMove = await NewService().MoveAsync(game.Id, "X", 0);
        Assert.Equal(2, afterMove.Moves.Length); // human move plus the computer reply

        var state = await NewService().UndoAsync(game.Id);

        Assert.Empty(state.Moves);
        Assert.Equal("X", state.CurrentPlayer);
        Assert.All(state.Board, cell => Assert.Equal(string.Empty, cell));
        Assert.False(state.CanUndo);
    }

    [Fact]
    public async Task Undo_is_rejected_when_there_is_nothing_to_undo_or_the_game_is_over()
    {
        var fresh = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().UndoAsync(fresh.Id));

        var finished = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(finished.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().UndoAsync(finished.Id));
    }

    [Fact]
    public async Task In_computer_mode_the_computer_replies_automatically_and_only_X_is_human()
    {
        var game = await NewService().CreateGameAsync(GameMode.Computer);

        var state = await NewService().MoveAsync(game.Id, "X", 0);

        Assert.Equal("X", state.Board[0]);
        Assert.Equal("O", state.Board[4]); // center, per the priority list
        Assert.True(state.Moves[1].IsComputerMove);
        Assert.Equal("X", state.CurrentPlayer);
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "O", 1));
    }

    [Fact]
    public async Task The_computer_does_not_move_after_the_game_is_already_complete()
    {
        var game = await NewService().CreateGameAsync(GameMode.Computer);

        // X takes the top row; the computer blocks elsewhere and must stop once X wins.
        await NewService().MoveAsync(game.Id, "X", 0);
        var state = await NewService().GetGameAsync(game.Id);
        while (state.Status == GameStatus.InProgress)
        {
            var free = Enumerable.Range(0, 9).First(i => state.Board[i] == string.Empty);
            state = await NewService().MoveAsync(game.Id, "X", free);
        }

        var lastMove = state.Moves[^1];
        Assert.False(state.Status == GameStatus.Won && lastMove.Player == "X" && lastMove.IsComputerMove);
        await Assert.ThrowsAsync<InvalidMoveException>(() => NewService().MoveAsync(game.Id, "X", 0));
    }

    [Fact]
    public async Task Reset_scoreboard_zeroes_the_counters()
    {
        var game = await NewService().CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(game.Id, ("X", 0), ("O", 3), ("X", 1), ("O", 4), ("X", 2));

        var scoreboard = await NewService().ResetScoreboardAsync();

        Assert.Equal(new ScoreboardDto(0, 0, 0), scoreboard);
    }

    [Fact]
    public async Task An_unknown_game_is_reported_as_not_found()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => NewService().GetGameAsync(Guid.NewGuid()));
    }
}
