using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicTacToe.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoardState = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    CurrentPlayer = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Winner = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    WinningCells = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ScoreApplied = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scoreboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    XWins = table.Column<int>(type: "int", nullable: false),
                    OWins = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scoreboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Moves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MoveNumber = table.Column<int>(type: "int", nullable: false),
                    Player = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    CellIndex = table.Column<int>(type: "int", nullable: false),
                    IsComputerMove = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Moves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Moves_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Scoreboards",
                columns: new[] { "Id", "Draws", "OWins", "XWins" },
                values: new object[] { 1, 0, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_Moves_GameId_MoveNumber",
                table: "Moves",
                columns: new[] { "GameId", "MoveNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Moves");

            migrationBuilder.DropTable(
                name: "Scoreboards");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
