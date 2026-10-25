import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="board" role="grid" aria-label="Tic Tac Toe board">
      @for (cell of board(); track $index) {
        <button
          type="button"
          class="cell"
          [class.winning]="winningCells().includes($index)"
          [class.taken]="cell !== ''"
          [disabled]="disabled() || cell !== ''"
          [attr.aria-label]="label($index, cell)"
          (click)="cellClick.emit($index)">
          {{ cell }}
        </button>
      }
    </div>
  `,
  styles: `
    .board {
      display: grid;
      grid-template-columns: repeat(3, 5.5rem);
      grid-template-rows: repeat(3, 5.5rem);
      gap: 0.5rem;
    }

    .cell {
      font-size: 2.5rem;
      font-weight: 700;
      font-family: inherit;
      color: #1f2937;
      background: #ffffff;
      border: 2px solid #cbd5e1;
      border-radius: 0.5rem;
      cursor: pointer;
      transition: background 0.15s ease, border-color 0.15s ease;
    }

    .cell:hover:not(:disabled) {
      background: #eef2ff;
      border-color: #6366f1;
    }

    .cell:disabled {
      cursor: default;
    }

    .cell.taken {
      background: #f8fafc;
    }

    .cell.winning {
      background: #dcfce7;
      border-color: #16a34a;
      color: #15803d;
    }
  `
})
export class BoardComponent {
  /** 9 cells, row-major; '' for empty. */
  readonly board = input.required<string[]>();
  readonly winningCells = input<number[]>([]);
  readonly disabled = input(false);

  readonly cellClick = output<number>();

  protected label(index: number, cell: string): string {
    const position = `row ${Math.floor(index / 3) + 1}, column ${(index % 3) + 1}`;
    return cell === '' ? `Play ${position}` : `${cell} at ${position}`;
  }
}
