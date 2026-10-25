using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;
using TicTacToe.Core;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public class GamesController(GameService games) : ControllerBase
{
    /// <summary>Creates a new game session.</summary>
    [HttpPost]
    [ProducesResponseType<GameStateDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GameStateDto>> Create(CreateGameRequest? request)
    {
        var state = await games.CreateGameAsync(request?.Mode ?? GameMode.TwoPlayer);
        return CreatedAtAction(nameof(Get), new { id = state.Id }, state);
    }

    /// <summary>Returns the current state of a game.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GameStateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateDto>> Get(Guid id) => await games.GetGameAsync(id);

    /// <summary>Submits a move. In computer mode the opponent's reply is included in the response.</summary>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType<GameStateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameStateDto>> Move(Guid id, MoveRequest request)
    {
        var cellIndex = ResolveCellIndex(request);
        return await games.MoveAsync(id, request.Player, cellIndex);
    }

    /// <summary>Undoes the last move, or the computer reply plus the preceding human move.</summary>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType<GameStateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameStateDto>> Undo(Guid id) => await games.UndoAsync(id);

    /// <summary>Clears the board and history. The scoreboard is left unchanged.</summary>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType<GameStateDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<GameStateDto>> Reset(Guid id) => await games.ResetGameAsync(id);

    /// <summary>Accepts either a cell index or a row/column pair, and rejects a request carrying neither.</summary>
    private static int ResolveCellIndex(MoveRequest request)
    {
        if (request.CellIndex is int cellIndex)
        {
            return cellIndex;
        }

        if (request.Row is int row && request.Column is int column)
        {
            // Row/column arrive 0-based from the UI; anything outside the board is rejected downstream.
            return row is >= 0 and <= 2 && column is >= 0 and <= 2 ? GameRules.CellIndex(row, column) : -1;
        }

        throw new InvalidMoveException("A move needs either cellIndex or both row and column.");
    }
}
