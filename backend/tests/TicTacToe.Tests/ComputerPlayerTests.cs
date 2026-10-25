using TicTacToe.Core;

namespace TicTacToe.Tests;

public class ComputerPlayerTests
{
    [Fact]
    public void Takes_the_winning_move_when_one_exists()
    {
        // O holds 0 and 1; X holds 3 and 4. Winning beats blocking.
        Assert.Equal(2, ComputerPlayer.ChooseMove("OO.XX...."));
    }

    [Fact]
    public void Blocks_the_opponent_when_it_cannot_win()
    {
        // X threatens the top row; O has nothing to complete.
        Assert.Equal(2, ComputerPlayer.ChooseMove("XX.O....."));
    }

    [Fact]
    public void Takes_the_center_when_free_and_no_line_is_urgent()
    {
        Assert.Equal(4, ComputerPlayer.ChooseMove("X........"));
    }

    [Fact]
    public void Takes_a_corner_when_the_center_is_taken()
    {
        Assert.Equal(0, ComputerPlayer.ChooseMove("....X...."));
    }

    [Fact]
    public void Falls_back_to_any_free_cell()
    {
        // Corners and center gone, no line to win or block: first free edge.
        Assert.Equal(1, ComputerPlayer.ChooseMove("X.X.O.O.X"));
    }

    [Fact]
    public void Returns_null_on_a_full_board()
    {
        Assert.Null(ComputerPlayer.ChooseMove("XXOOOXXOX"));
    }

    [Fact]
    public void Never_picks_an_occupied_cell_over_a_full_game()
    {
        var board = GameRules.EmptyBoard;
        var current = GameRules.PlayerX;

        while (board.Contains(GameRules.Empty) && GameRules.CheckWin(board).Winner is null)
        {
            var cell = ComputerPlayer.ChooseMove(board, current);
            Assert.NotNull(cell);
            Assert.Equal(GameRules.Empty, board[cell!.Value]);
            board = GameRules.ApplyMove(board, cell.Value, current);
            current = GameRules.Opponent(current);
        }
    }
}
