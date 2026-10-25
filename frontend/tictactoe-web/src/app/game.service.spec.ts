import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { GameMode, GameStateDto, GameStatus } from './game.models';
import { GameService } from './game.service';

const base = environment.apiBaseUrl;

function state(overrides: Partial<GameStateDto> = {}): GameStateDto {
  return {
    id: 'game-1',
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

describe('GameService', () => {
  let service: GameService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(GameService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates a game with the chosen mode', async () => {
    const done = service.newGame(GameMode.Computer);

    const request = http.expectOne(`${base}/api/games`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ mode: GameMode.Computer });
    request.flush(state({ mode: GameMode.Computer }));
    await done;

    expect(service.mode()).toBe(GameMode.Computer);
  });

  it('sends the move for the player the backend says is to move', async () => {
    service.state.set(state({ currentPlayer: 'O' }));

    const done = service.play(3);
    const request = http.expectOne(`${base}/api/games/game-1/moves`);
    expect(request.request.body).toEqual({ player: 'O', cellIndex: 3 });
    request.flush(state({ canUndo: true }));
    await done;

    expect(service.canUndo()).toBeTrue();
  });

  it('calls undo and reset on the current game', async () => {
    service.state.set(state());

    const undone = service.undo();
    http.expectOne(`${base}/api/games/game-1/undo`).flush(state());
    await undone;

    const reset = service.resetGame();
    http.expectOne(`${base}/api/games/game-1/reset`).flush(state());
    await reset;
  });

  it('resets the scoreboard and then re-reads the game', async () => {
    service.state.set(state({ scoreboard: { xWins: 2, oWins: 1, draws: 1 } }));

    const done = service.resetScoreboard();
    http.expectOne(`${base}/api/scoreboard/reset`).flush({ xWins: 0, oWins: 0, draws: 0 });
    await new Promise(resolve => setTimeout(resolve)); // let the reset continuation fire the re-read
    http.expectOne(`${base}/api/games/game-1`).flush(state());
    await done;

    expect(service.scoreboard()).toEqual({ xWins: 0, oWins: 0, draws: 0 });
  });

  it('surfaces the error message the backend returns for a rejected move', async () => {
    service.state.set(state());

    const done = service.play(0);
    http.expectOne(`${base}/api/games/game-1/moves`)
      .flush({ error: 'Cell is already occupied.' }, { status: 400, statusText: 'Bad Request' });
    await done;

    expect(service.error()).toBe('Cell is already occupied.');
    expect(service.busy()).toBeFalse();
  });
});
