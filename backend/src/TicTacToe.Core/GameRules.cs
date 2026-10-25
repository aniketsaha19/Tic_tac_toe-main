namespace TicTacToe.Core;

/// <summary>
/// Pure Tic Tac Toe rules. No persistence, no framework types: every method is a
/// function of the board string, so the unit tests can drive them directly.
/// A board is 9 characters, index 0..8 in row-major order, '.' meaning empty.
/// </summary>
public static class GameRules
{
    public const char Empty = '.';
    public const char PlayerX = 'X';
    public const char PlayerO = 'O';
    public const string EmptyBoard = ".........";

    /// <summary>The 8 winning lines: 3 rows, 3 columns, 2 diagonals.</summary>
    public static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public static char Opponent(char player) => player == PlayerX ? PlayerO : PlayerX;

    public static int CellIndex(int row, int col) => row * 3 + col;

    public static (int Row, int Col) RowCol(int cellIndex) => (cellIndex / 3, cellIndex % 3);

    /// <summary>
    /// Returns the winner and the three winning cells, or (null, empty) when nobody has won.
    /// </summary>
    public static (char? Winner, int[] WinningCells) CheckWin(string board)
    {
        foreach (var line in Lines)
        {
            var first = board[line[0]];
            if (first != Empty && first == board[line[1]] && first == board[line[2]])
            {
                return (first, line);
            }
        }

        return (null, []);
    }

    public static bool IsDraw(string board) =>
        !board.Contains(Empty) && CheckWin(board).Winner is null;

    /// <summary>
    /// Validates a move against the board. Returns an error message, or null when the move is legal.
    /// </summary>
    public static string? ValidateMove(string board, GameStatus status, char currentPlayer, char player, int cellIndex)
    {
        if (status != GameStatus.InProgress)
        {
            return "The game is already complete. Reset the game to play again.";
        }

        if (player != PlayerX && player != PlayerO)
        {
            return $"Unknown player '{player}'. Expected X or O.";
        }

        if (player != currentPlayer)
        {
            return $"It is {currentPlayer}'s turn.";
        }

        if (cellIndex < 0 || cellIndex > 8)
        {
            return "Cell is outside the board.";
        }

        if (board[cellIndex] != Empty)
        {
            return "Cell is already occupied.";
        }

        return null;
    }

    public static string ApplyMove(string board, int cellIndex, char player)
    {
        var cells = board.ToCharArray();
        cells[cellIndex] = player;
        return new string(cells);
    }

    /// <summary>Rebuilds a board from an ordered move list. Used by Undo to replay the remaining moves.</summary>
    public static string BuildBoard(IEnumerable<(int CellIndex, char Player)> moves)
    {
        var cells = EmptyBoard.ToCharArray();
        foreach (var (cellIndex, player) in moves)
        {
            cells[cellIndex] = player;
        }

        return new string(cells);
    }
}
