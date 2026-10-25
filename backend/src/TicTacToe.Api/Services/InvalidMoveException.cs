namespace TicTacToe.Api.Services;

/// <summary>Thrown for a rejected move or undo; the controller turns it into a 400.</summary>
public class InvalidMoveException(string message) : Exception(message);
