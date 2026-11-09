import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  template: `
    <div class="flex flex-wrap items-center justify-between gap-3 text-sm text-gray-600">
      <label class="flex items-center gap-2">
        Show
        <select
          class="rounded-md border border-gray-300 bg-white px-2 py-1"
          [value]="pageSize()"
          (change)="pageSizeChange.emit(+$any($event.target).value)"
        >
          @for (size of pageSizes; track size) {
            <option [value]="size">{{ size }}</option>
          }
        </select>
      </label>

      <div class="flex items-center gap-3">
        <span>{{ from() }}–{{ to() }} of {{ totalCount() }}</span>
        <div class="flex gap-1">
          <button
            type="button"
            class="btn-secondary px-2.5"
            aria-label="Previous page"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
          >
            ‹
          </button>
          <button
            type="button"
            class="btn-secondary px-2.5"
            aria-label="Next page"
            [disabled]="page() >= totalPages()"
            (click)="pageChange.emit(page() + 1)"
          >
            ›
          </button>
        </div>
      </div>
    </div>
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly pageSizes = [10, 20, 50];

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  protected readonly from = computed(() =>
    this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly to = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
}
