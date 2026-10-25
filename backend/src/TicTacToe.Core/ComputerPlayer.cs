namespace TicTacToe.Core;

/// <summary>
/// The basic opponent required by the spec. Priority: win, block, center, corner, any.
/// Cell order inside each rung is fixed so the behaviour is deterministic and testable.
/// </summary>
public static class ComputerPlayer
{
    private static readonly int[] Corners = [0, 2, 6, 8];
    private const int Center = 4;

    /// <summary>Returns the chosen cell index, or null when the board is full.</summary>
    public static int? ChooseMove(string board, char me = GameRules.PlayerO)
    {
        var opponent = GameRules.Opponent(me);

        if (FindCompletingCell(board, me) is int winning)
        {
            return winning;
        }

        if (FindCompletingCell(board, opponent) is int blocking)
        {
            return blocking;
        }

        if (board[Center] == GameRules.Empty)
        {
            return Center;
        }

        foreach (var corner in Corners)
        {
            if (board[corner] == GameRules.Empty)
            {
                return corner;
            }
        }

        for (var i = 0; i < board.Length; i++)
        {
            if (board[i] == GameRules.Empty)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>The empty cell that would give <paramref name="player"/> a line, if one exists.</summary>
    private static int? FindCompletingCell(string board, char player)
    {
        foreach (var line in GameRules.Lines)
        {
            var owned = 0;
            var emptyCell = -1;

            foreach (var cell in line)
            {
                if (board[cell] == player)
                {
                    owned++;
                }
                else if (board[cell] == GameRules.Empty)
                {
                    emptyCell = cell;
                }
            }

            if (owned == 2 && emptyCell >= 0)
            {
                return emptyCell;
            }
        }

        return null;
    }
}
