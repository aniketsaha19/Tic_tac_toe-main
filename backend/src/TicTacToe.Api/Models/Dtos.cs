using TicTacToe.Core;

namespace TicTacToe.Api.Models;

/// <summary>Everything the frontend needs to render a game. Returned by every game endpoint.</summary>
public record GameStateDto(
    Guid Id,
    string[] Board,
    string CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    string? Winner,
    int[] WinningCells,
    MoveDto[] Moves,
    bool CanUndo,
    ScoreboardDto Scoreboard);

public record MoveDto(int MoveNumber, string Player, int Row, int Column, int CellIndex, bool IsComputerMove);

public record ScoreboardDto(int XWins, int OWins, int Draws);

public record CreateGameRequest(GameMode Mode = GameMode.TwoPlayer);

/// <summary>A move may be addressed either by row/column or by cell index, whichever the client sends.</summary>
public record MoveRequest(string Player, int? Row, int? Column, int? CellIndex);

public record ErrorResponse(string Error);
