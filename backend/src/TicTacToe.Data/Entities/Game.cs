using System.ComponentModel.DataAnnotations;
using TicTacToe.Core;

namespace TicTacToe.Data.Entities;

/// <summary>One game session. The backend's source of truth for the board and its status.</summary>
public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>9 characters, row-major, '.' for an empty cell.</summary>
    [MaxLength(9)]
    public string BoardState { get; set; } = GameRules.EmptyBoard;

    public char CurrentPlayer { get; set; } = GameRules.PlayerX;

    public GameMode Mode { get; set; } = GameMode.TwoPlayer;

    public GameStatus Status { get; set; } = GameStatus.InProgress;

    public char? Winner { get; set; }

    /// <summary>Comma-separated winning cell indexes, null while no line is complete.</summary>
    [MaxLength(16)]
    public string? WinningCells { get; set; }

    /// <summary>Guards the "scoreboard updates only once per completed game" rule.</summary>
    public bool ScoreApplied { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Move> Moves { get; set; } = [];
}
