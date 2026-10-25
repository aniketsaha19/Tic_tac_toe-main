namespace TicTacToe.Data.Entities;

/// <summary>A single persisted move. The move history and Undo both read from this table.</summary>
public class Move
{
    public int Id { get; set; }

    public Guid GameId { get; set; }

    public Game? Game { get; set; }

    public int MoveNumber { get; set; }

    public char Player { get; set; }

    /// <summary>0..8, row-major.</summary>
    public int CellIndex { get; set; }

    /// <summary>True when the computer opponent played it. Undo in computer mode needs this.</summary>
    public bool IsComputerMove { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
