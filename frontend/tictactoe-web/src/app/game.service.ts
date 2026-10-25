import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../environments/environment';
import { GameMode, GameStateDto, GameStatus } from './game.models';

/**
 * The only place that talks to the API. It holds the latest state the backend returned and
 * derives nothing itself: the backend owns the rules.
 */
@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  readonly state = signal<GameStateDto | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly board = computed(() => this.state()?.board ?? Array<string>(9).fill(''));
  readonly scoreboard = computed(() => this.state()?.scoreboard ?? { xWins: 0, oWins: 0, draws: 0 });
  readonly moves = computed(() => this.state()?.moves ?? []);
  readonly winningCells = computed(() => this.state()?.winningCells ?? []);
  readonly mode = computed(() => this.state()?.mode ?? GameMode.TwoPlayer);
  readonly canUndo = computed(() => this.state()?.canUndo ?? false);
  readonly isComplete = computed(
    () => (this.state()?.status ?? GameStatus.InProgress) !== GameStatus.InProgress
  );

  /** The line shown above the board. */
  readonly statusMessage = computed(() => {
    const state = this.state();
    if (!state) {
      return 'Starting a new game...';
    }

    switch (state.status) {
      case GameStatus.Won:
        return `${state.winner} wins!`;
      case GameStatus.Draw:
        return 'It is a draw.';
      default:
        return `Turn: ${state.currentPlayer}`;
    }
  });

  newGame(mode: GameMode): Promise<void> {
    return this.apply(this.http.post<GameStateDto>(`${this.baseUrl}/api/games`, { mode }));
  }

  /** Plays a cell for whoever the backend says is to move. Ignored while a request is in flight. */
  play(cellIndex: number): Promise<void> {
    const state = this.state();
    if (!state || this.busy()) {
      return Promise.resolve();
    }

    return this.apply(this.http.post<GameStateDto>(
      `${this.baseUrl}/api/games/${state.id}/moves`,
      { player: state.currentPlayer, cellIndex }
    ));
  }

  undo(): Promise<void> {
    const id = this.state()?.id;
    return id
      ? this.apply(this.http.post<GameStateDto>(`${this.baseUrl}/api/games/${id}/undo`, {}))
      : Promise.resolve();
  }

  resetGame(): Promise<void> {
    const id = this.state()?.id;
    return id
      ? this.apply(this.http.post<GameStateDto>(`${this.baseUrl}/api/games/${id}/reset`, {}))
      : Promise.resolve();
  }

  /** Zeroes the scoreboard, then re-reads the game so the UI shows the new totals. */
  async resetScoreboard(): Promise<void> {
    const id = this.state()?.id;
    if (!id) {
      return;
    }

    this.busy.set(true);
    try {
      await firstValueFrom(this.http.post(`${this.baseUrl}/api/scoreboard/reset`, {}));
    } catch (error) {
      this.busy.set(false);
      this.error.set(this.describe(error));
      return;
    }

    this.busy.set(false);
    await this.apply(this.http.get<GameStateDto>(`${this.baseUrl}/api/games/${id}`));
  }

  /** Single place for the busy flag and for turning a failed request into a readable message. */
  private async apply(request: Observable<GameStateDto>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);

    try {
      this.state.set(await firstValueFrom(request));
    } catch (error) {
      this.error.set(this.describe(error));
    } finally {
      this.busy.set(false);
    }
  }

  private describe(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.error ?? `Request failed (${error.status}). Is the backend running?`;
    }

    return 'Request failed. Is the backend running?';
  }
}
