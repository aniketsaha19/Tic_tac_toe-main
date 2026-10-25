namespace TicTacToe.Data.Entities;

/// <summary>Single global scoreboard row (Id = 1), seeded at startup.</summary>
public class Scoreboard
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int XWins { get; set; }

    public int OWins { get; set; }

    public int Draws { get; set; }
}
