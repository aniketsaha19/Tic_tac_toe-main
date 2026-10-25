using TicTacToe.Core;

namespace TicTacToe.Tests;

public class GameRulesTests
{
    [Theory]
    [InlineData("XXX.O.O..", 'X', new[] { 0, 1, 2 })]     // top row
    [InlineData("O..XXX.O.", 'X', new[] { 3, 4, 5 })]     // middle row
    [InlineData("X.OX.O.XO", 'O', new[] { 2, 5, 8 })]     // right column
    [InlineData("X.OXO.O.X", 'O', new[] { 2, 4, 6 })]     // anti-diagonal
    [InlineData("XO.OX..OX", 'X', new[] { 0, 4, 8 })]     // main diagonal
    public void CheckWin_detects_lines(string board, char expectedWinner, int[] expectedCells)
    {
        var (winner, cells) = GameRules.CheckWin(board);

        Assert.Equal(expectedWinner, winner);
        Assert.Equal(expectedCells, cells);
    }

    [Theory]
    [InlineData(".........")]
    [InlineData("XOXXOOOXX")] // full board, no line
    [InlineData("XX.OO....")]
    public void CheckWin_returns_null_when_nobody_has_a_line(string board)
    {
        Assert.Null(GameRules.CheckWin(board).Winner);
    }

    [Fact]
    public void IsDraw_is_true_only_for_a_full_board_with_no_winner()
    {
        Assert.True(GameRules.IsDraw("XXOOOXXOX"));
        Assert.False(GameRules.IsDraw("XXOOOXXO.")); // not full
        Assert.False(GameRules.IsDraw("XXXOOXOXO")); // X won
    }

    [Fact]
    public void ValidateMove_accepts_a_legal_move()
    {
        Assert.Null(GameRules.ValidateMove(GameRules.EmptyBoard, GameStatus.InProgress, 'X', 'X', 4));
    }

    [Fact]
    public void ValidateMove_rejects_a_cell_outside_the_board()
    {
        Assert.NotNull(GameRules.ValidateMove(GameRules.EmptyBoard, GameStatus.InProgress, 'X', 'X', 9));
        Assert.NotNull(GameRules.ValidateMove(GameRules.EmptyBoard, GameStatus.InProgress, 'X', 'X', -1));
    }

    [Fact]
    public void ValidateMove_rejects_an_occupied_cell()
    {
        Assert.NotNull(GameRules.ValidateMove("X........", GameStatus.InProgress, 'O', 'O', 0));
    }

    [Fact]
    public void ValidateMove_rejects_a_move_by_the_wrong_player()
    {
        Assert.NotNull(GameRules.ValidateMove(GameRules.EmptyBoard, GameStatus.InProgress, 'X', 'O', 0));
    }

    [Theory]
    [InlineData(GameStatus.Won)]
    [InlineData(GameStatus.Draw)]
    public void ValidateMove_rejects_a_move_after_the_game_is_complete(GameStatus status)
    {
        Assert.NotNull(GameRules.ValidateMove("XXX.O.O..", status, 'O', 'O', 3));
    }

    [Fact]
    public void BuildBoard_replays_moves_in_order()
    {
        var board = GameRules.BuildBoard([(4, 'X'), (0, 'O'), (8, 'X')]);

        Assert.Equal("O...X...X", board);
    }

    [Fact]
    public void RowCol_and_CellIndex_round_trip()
    {
        for (var i = 0; i < 9; i++)
        {
            var (row, col) = GameRules.RowCol(i);
            Assert.Equal(i, GameRules.CellIndex(row, col));
        }
    }
}
