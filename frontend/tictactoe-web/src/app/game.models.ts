/** Mirrors the backend DTOs in TicTacToe.Api/Models/Dtos.cs. */

export enum GameMode {
  TwoPlayer = 0,
  Computer = 1
}

export enum GameStatus {
  InProgress = 0,
  Won = 1,
  Draw = 2
}

export interface MoveDto {
  moveNumber: number;
  player: string;
  /** 1-based, for display. */
  row: number;
  /** 1-based, for display. */
  column: number;
  cellIndex: number;
  isComputerMove: boolean;
}

export interface ScoreboardDto {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameStateDto {
  id: string;
  /** 9 entries, row-major; '' for an empty cell. */
  board: string[];
  currentPlayer: string;
  mode: GameMode;
  status: GameStatus;
  winner: string | null;
  winningCells: number[];
  moves: MoveDto[];
  canUndo: boolean;
  scoreboard: ScoreboardDto;
}
