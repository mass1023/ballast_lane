import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import {
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  startWith,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { serverErrors } from '../core/api-errors';
import { ConfirmDialog } from '../shared/confirm-dialog';
import { ErrorBox } from '../shared/error-box';
import { Pagination } from '../shared/pagination';
import { Page, Release, ReleaseQuery, ReleasesApi } from './releases.api';

@Component({
  selector: 'app-release-list',
  imports: [ReactiveFormsModule, RouterLink, Pagination, ConfirmDialog, ErrorBox],
  templateUrl: './release-list.html',
})
export class ReleaseList {
  private api = inject(ReleasesApi);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private reloadRequests = new Subject<void>();

  protected readonly query = toSignal(this.route.queryParamMap.pipe(map(readQuery)), {
    requireSync: true,
  });
  protected readonly search = new FormControl(this.query().search, { nonNullable: true });

  protected readonly releases = signal<Page<Release> | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string[]>([]);

  protected readonly toDelete = signal<Release | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string[]>([]);

  constructor() {
    this.search.valueChanges
      .pipe(
        debounceTime(300),
        map((value) => value.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.navigate({ search: search || null, page: null }));

    combineLatest([this.route.queryParamMap.pipe(map(readQuery)), this.reloadRequests.pipe(startWith(null))])
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set([]);
        }),
        switchMap(([query]) =>
          this.api.search(query).pipe(
            catchError((error) => {
              this.error.set(serverErrors(error));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((page) => {
        this.loading.set(false);
        if (page) {
          this.releases.set(page);
        }
      });
  }

  protected changePage(page: number): void {
    this.navigate({ page: page > 1 ? page : null });
  }

  protected changePageSize(pageSize: number): void {
    this.navigate({ size: pageSize !== 10 ? pageSize : null, page: null });
  }

  protected clearSearch(): void {
    this.search.setValue('');
  }

  protected askDelete(release: Release): void {
    this.deleteError.set([]);
    this.toDelete.set(release);
  }

  protected cancelDelete(): void {
    if (!this.deleting()) {
      this.toDelete.set(null);
    }
  }

  protected confirmDelete(): void {
    const release = this.toDelete();
    if (!release) return;

    this.deleting.set(true);
    this.api.delete(release.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toDelete.set(null);

        const { page } = this.query();
        if (page > 1 && this.releases()?.items.length === 1) {
          this.changePage(page - 1);
        } else {
          this.reloadRequests.next();
        }
      },
      error: (error) => {
        this.deleting.set(false);
        this.toDelete.set(null);
        this.deleteError.set(serverErrors(error));
      },
    });
  }

  private navigate(queryParams: Params): void {
    this.router.navigate([], { queryParams, queryParamsHandling: 'merge', replaceUrl: true });
  }
}

function readQuery(params: { get(name: string): string | null }): ReleaseQuery {
  return {
    search: params.get('search') ?? '',
    page: Math.max(1, Number(params.get('page')) || 1),
    pageSize: Number(params.get('size')) || 10,
  };
}
