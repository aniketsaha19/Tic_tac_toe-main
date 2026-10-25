import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ScoreboardDto } from '../game.models';

@Component({
  selector: 'app-scoreboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel">
      <h2>Scoreboard</h2>
      <dl>
        <div><dt>X wins</dt><dd>{{ scoreboard().xWins }}</dd></div>
        <div><dt>O wins</dt><dd>{{ scoreboard().oWins }}</dd></div>
        <div><dt>Draws</dt><dd>{{ scoreboard().draws }}</dd></div>
      </dl>
    </section>
  `,
  styles: `
    dl {
      display: flex;
      gap: 1.5rem;
      margin: 0;
    }

    dt {
      color: #475569;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    dd {
      margin: 0.15rem 0 0;
      font-size: 1.6rem;
      font-weight: 700;
      color: #1f2937;
    }
  `
})
export class ScoreboardComponent {
  readonly scoreboard = input.required<ScoreboardDto>();
}
