using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Models;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
[Produces("application/json")]
public class ScoreboardController(GameService games) : ControllerBase
{
    /// <summary>Returns the session scoreboard.</summary>
    [HttpGet]
    [ProducesResponseType<ScoreboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScoreboardDto>> Get() => await games.GetScoreboardAsync();

    /// <summary>Sets wins and draws back to zero. Games in progress are not affected.</summary>
    [HttpPost("reset")]
    [ProducesResponseType<ScoreboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScoreboardDto>> Reset() => await games.ResetScoreboardAsync();
}
