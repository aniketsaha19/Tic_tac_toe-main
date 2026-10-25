import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MoveDto } from '../game.models';

@Component({
  selector: 'app-move-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel">
      <h2>Move history</h2>

      @if (moves().length === 0) {
        <p class="empty">No moves yet.</p>
      } @else {
        <table>
          <thead>
            <tr><th>Move</th><th>Player</th><th>Position</th></tr>
          </thead>
          <tbody>
            @for (move of moves(); track move.moveNumber) {
              <tr>
                <td>{{ move.moveNumber }}</td>
                <td>{{ move.player }}{{ move.isComputerMove ? ' (computer)' : '' }}</td>
                <td>Row {{ move.row }}, Column {{ move.column }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: `
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.9rem;
    }

    th, td {
      text-align: left;
      padding: 0.35rem 0.5rem;
      border-bottom: 1px solid #e2e8f0;
    }

    th {
      color: #475569;
      font-weight: 600;
    }

    .empty {
      color: #64748b;
      margin: 0;
    }
  `
})
export class MoveHistoryComponent {
  readonly moves = input.required<MoveDto[]>();
}
