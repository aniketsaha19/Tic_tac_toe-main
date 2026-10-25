import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { App } from './app';
import { GameMode, GameStateDto, GameStatus } from './game.models';

function emptyState(overrides: Partial<GameStateDto> = {}): GameStateDto {
  return {
    id: 'a3d3f4ba-0000-4000-8000-000000000001',
    board: Array<string>(9).fill(''),
    currentPlayer: 'X',
    mode: GameMode.TwoPlayer,
    status: GameStatus.InProgress,
    winner: null,
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides
  };
}

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Answers the game request the component fires on init, then lets the signal update settle. */
  async function start(state: GameStateDto = emptyState()): Promise<void> {
    fixture.detectChanges();
    http.expectOne(`${environment.apiBaseUrl}/api/games`).flush(state);
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('creates a game on init and renders nine cells', async () => {
    await start();

    const cells = fixture.nativeElement.querySelectorAll('.cell');
    expect(cells.length).toBe(9);
    expect(fixture.nativeElement.querySelector('.status').textContent).toContain('Turn: X');
  });

  it('posts the clicked cell and renders the state the backend returns', async () => {
    await start();

    fixture.nativeElement.querySelectorAll('.cell')[4].click();

    const request = http.expectOne(
      `${environment.apiBaseUrl}/api/games/a3d3f4ba-0000-4000-8000-000000000001/moves`
    );
    expect(request.request.body).toEqual({ player: 'X', cellIndex: 4 });

    const board = Array<string>(9).fill('');
    board[4] = 'X';
    request.flush(emptyState({ board, currentPlayer: 'O', canUndo: true }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.cell')[4].textContent.trim()).toBe('X');
    expect(fixture.nativeElement.querySelector('.status').textContent).toContain('Turn: O');
  });

  it('highlights the winning cells and disables Undo once the game is won', async () => {
    const board = ['X', 'X', 'X', 'O', 'O', '', '', '', ''];
    await start(emptyState({
      board,
      status: GameStatus.Won,
      winner: 'X',
      winningCells: [0, 1, 2],
      canUndo: false,
      scoreboard: { xWins: 1, oWins: 0, draws: 0 }
    }));

    expect(fixture.nativeElement.querySelectorAll('.cell.winning').length).toBe(3);
    expect(fixture.nativeElement.querySelector('.status').textContent).toContain('X wins!');

    const undo = Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('.actions button'))
      .find(button => button.textContent!.includes('Undo'))!;
    expect(undo.disabled).toBeTrue();
  });

  it('shows the move history returned by the backend', async () => {
    await start(emptyState({
      moves: [{ moveNumber: 1, player: 'X', row: 1, column: 1, cellIndex: 0, isComputerMove: false }]
    }));

    expect(fixture.nativeElement.querySelector('app-move-history').textContent)
      .toContain('Row 1, Column 1');
  });
});
