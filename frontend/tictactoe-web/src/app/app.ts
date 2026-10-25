import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { BoardComponent } from './board/board';
import { GameMode } from './game.models';
import { GameService } from './game.service';
import { MoveHistoryComponent } from './move-history/move-history';
import { ScoreboardComponent } from './scoreboard/scoreboard';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BoardComponent, MoveHistoryComponent, ScoreboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly game = inject(GameService);
  protected readonly GameMode = GameMode;

  ngOnInit(): void {
    this.game.newGame(GameMode.TwoPlayer);
  }

  /** Switching mode starts a fresh game in that mode; the scoreboard carries over. */
  protected changeMode(value: string): void {
    this.game.newGame(Number(value) as GameMode);
  }
}
