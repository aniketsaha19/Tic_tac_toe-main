using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Models;
using TicTacToe.Core;
using TicTacToe.Data;
using TicTacToe.Data.Entities;

namespace TicTacToe.Api.Services;

/// <summary>
/// Owns game state. Rules come from TicTacToe.Core; this class only loads, applies and persists them.
/// Every mutating method saves once, so a human move plus the computer reply plus the scoreboard
/// update land in a single transaction.
/// </summary>
public class GameService(GameDbContext db)
{
    public async Task<GameStateDto> CreateGameAsync(GameMode mode)
    {
        var game = new Game { Mode = mode };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        return await ToDtoAsync(game);
    }

    public async Task<GameStateDto> GetGameAsync(Guid id) => await ToDtoAsync(await LoadAsync(id));

    public async Task<GameStateDto> MoveAsync(Guid id, string player, int cellIndex)
    {
        var game = await LoadAsync(id);
        var playerChar = ParsePlayer(player);

        var error = GameRules.ValidateMove(game.BoardState, game.Status, game.CurrentPlayer, playerChar, cellIndex);
        if (error is not null)
        {
            throw new InvalidMoveException(error);
        }

        if (game.Mode == GameMode.Computer && playerChar != GameRules.PlayerX)
        {
            throw new InvalidMoveException("In computer mode only player X is played by a human.");
        }

        RecordMove(game, cellIndex, playerChar, isComputerMove: false);

        // In computer mode O answers immediately, inside the same request and the same transaction.
        if (game.Mode == GameMode.Computer && game.Status == GameStatus.InProgress)
        {
            var computerCell = ComputerPlayer.ChooseMove(game.BoardState, GameRules.PlayerO);
            if (computerCell is int cell)
            {
                RecordMove(game, cell, GameRules.PlayerO, isComputerMove: true);
            }
        }

        await ApplyScoreIfCompletedAsync(game);
        await db.SaveChangesAsync();

        return await ToDtoAsync(game);
    }

    public async Task<GameStateDto> UndoAsync(Guid id)
    {
        var game = await LoadAsync(id);

        // Option A: once a game is won or drawn the result is final.
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidMoveException("Undo is disabled once the game is complete. Reset the game to play again.");
        }

        var moves = game.Moves.OrderBy(m => m.MoveNumber).ToList();
        if (moves.Count == 0)
        {
            throw new InvalidMoveException("There are no moves to undo.");
        }

        var last = moves[^1];
        db.Moves.Remove(last);
        moves.RemoveAt(moves.Count - 1);

        // Computer mode: drop the computer reply together with the human move that caused it.
        if (game.Mode == GameMode.Computer && last.IsComputerMove && moves.Count > 0)
        {
            var human = moves[^1];
            db.Moves.Remove(human);
            moves.RemoveAt(moves.Count - 1);
        }

        game.Moves = moves;
        Recalculate(game, moves);
        await db.SaveChangesAsync();

        return await ToDtoAsync(game);
    }

    /// <summary>Clears the board and history of the existing session. The scoreboard is untouched.</summary>
    public async Task<GameStateDto> ResetGameAsync(Guid id)
    {
        var game = await LoadAsync(id);

        db.Moves.RemoveRange(game.Moves);
        game.Moves = [];
        game.BoardState = GameRules.EmptyBoard;
        game.CurrentPlayer = GameRules.PlayerX;
        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells = null;
        game.ScoreApplied = false;

        await db.SaveChangesAsync();

        return await ToDtoAsync(game);
    }

    public async Task<ScoreboardDto> GetScoreboardAsync()
    {
        var scoreboard = await LoadScoreboardAsync();
        return new ScoreboardDto(scoreboard.XWins, scoreboard.OWins, scoreboard.Draws);
    }

    public async Task<ScoreboardDto> ResetScoreboardAsync()
    {
        var scoreboard = await LoadScoreboardAsync();
        scoreboard.XWins = 0;
        scoreboard.OWins = 0;
        scoreboard.Draws = 0;
        await db.SaveChangesAsync();

        return new ScoreboardDto(0, 0, 0);
    }

    private static void RecordMove(Game game, int cellIndex, char player, bool isComputerMove)
    {
        game.BoardState = GameRules.ApplyMove(game.BoardState, cellIndex, player);
        game.Moves.Add(new Move
        {
            GameId = game.Id,
            MoveNumber = game.Moves.Count + 1,
            Player = player,
            CellIndex = cellIndex,
            IsComputerMove = isComputerMove
        });

        var (winner, winningCells) = GameRules.CheckWin(game.BoardState);
        if (winner is char champion)
        {
            game.Status = GameStatus.Won;
            game.Winner = champion;
            game.WinningCells = string.Join(',', winningCells);
            game.CurrentPlayer = champion;
        }
        else if (GameRules.IsDraw(game.BoardState))
        {
            game.Status = GameStatus.Draw;
        }
        else
        {
            game.CurrentPlayer = GameRules.Opponent(player);
        }
    }

    /// <summary>Rebuilds board, turn and status from the moves that survived an undo.</summary>
    private static void Recalculate(Game game, List<Move> moves)
    {
        game.BoardState = GameRules.BuildBoard(moves.Select(m => (m.CellIndex, m.Player)));

        for (var i = 0; i < moves.Count; i++)
        {
            moves[i].MoveNumber = i + 1;
        }

        var (winner, winningCells) = GameRules.CheckWin(game.BoardState);
        if (winner is char champion)
        {
            game.Status = GameStatus.Won;
            game.Winner = champion;
            game.WinningCells = string.Join(',', winningCells);
            game.CurrentPlayer = champion;
        }
        else if (GameRules.IsDraw(game.BoardState))
        {
            game.Status = GameStatus.Draw;
            game.Winner = null;
            game.WinningCells = null;
        }
        else
        {
            game.Status = GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = null;
            game.CurrentPlayer = moves.Count == 0
                ? GameRules.PlayerX
                : GameRules.Opponent(moves[^1].Player);
        }
    }

    /// <summary>Adds the result to the scoreboard once, the first time a game reaches a final status.</summary>
    private async Task ApplyScoreIfCompletedAsync(Game game)
    {
        if (game.ScoreApplied || game.Status == GameStatus.InProgress)
        {
            return;
        }

        var scoreboard = await LoadScoreboardAsync();
        if (game.Status == GameStatus.Draw)
        {
            scoreboard.Draws++;
        }
        else if (game.Winner == GameRules.PlayerX)
        {
            scoreboard.XWins++;
        }
        else
        {
            scoreboard.OWins++;
        }

        game.ScoreApplied = true;
    }

    private async Task<Game> LoadAsync(Guid id)
    {
        var game = await db.Games.Include(g => g.Moves).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Game {id} was not found.");

        game.Moves = [.. game.Moves.OrderBy(m => m.MoveNumber)];
        return game;
    }

    private async Task<Scoreboard> LoadScoreboardAsync()
    {
        var scoreboard = await db.Scoreboards.FirstOrDefaultAsync(s => s.Id == Scoreboard.SingletonId);
        if (scoreboard is null)
        {
            scoreboard = new Scoreboard();
            db.Scoreboards.Add(scoreboard);
        }

        return scoreboard;
    }

    private static char ParsePlayer(string player)
    {
        var trimmed = (player ?? string.Empty).Trim().ToUpperInvariant();
        if (trimmed.Length != 1 || (trimmed[0] != GameRules.PlayerX && trimmed[0] != GameRules.PlayerO))
        {
            throw new InvalidMoveException($"Unknown player '{player}'. Expected X or O.");
        }

        return trimmed[0];
    }

    private async Task<GameStateDto> ToDtoAsync(Game game)
    {
        var scoreboard = await GetScoreboardAsync();

        return new GameStateDto(
            game.Id,
            [.. game.BoardState.Select(c => c == GameRules.Empty ? string.Empty : c.ToString())],
            game.CurrentPlayer.ToString(),
            game.Mode,
            game.Status,
            game.Winner?.ToString(),
            string.IsNullOrEmpty(game.WinningCells) ? [] : [.. game.WinningCells.Split(',').Select(int.Parse)],
            [.. game.Moves.OrderBy(m => m.MoveNumber).Select(m =>
            {
                var (row, col) = GameRules.RowCol(m.CellIndex);
                return new MoveDto(m.MoveNumber, m.Player.ToString(), row + 1, col + 1, m.CellIndex, m.IsComputerMove);
            })],
            game.Status == GameStatus.InProgress && game.Moves.Count > 0,
            scoreboard);
    }
}
